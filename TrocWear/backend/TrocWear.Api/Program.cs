
using TrocWear.Domain.Models;
using FluentValidation;
using MediatR;
using AuthService.Application.Behaviors;
using AuthService.Api.Security;
using AuthService.Application.Authentification;
using AuthService.Application.Utilisateurs;
using AuthService.Domain.DTOs;
using AuthService.Application.Interface.IRepositories;
using AuthService.Application.Interface.IService;
using AuthService.Infrastructure.Messaging;
using AuthService.Infrastructure.Persistence;
using AuthService.Infrastructure.Persistence.Interface;
using AuthService.Infrastructure.Repositories;
using AuthService.Infrastructure.Service;
using Microsoft.AspNetCore.Authentication;
using TrocWear.Application.Metiers;
using TrocWear.Infrastructure.Metiers;
using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using StackExchange.Redis;
using TrocWear.BuildingBlocks.CQRS;
using TrocWear.BuildingBlocks.Messaging;
using TrocWear.BuildingBlocks.SharedExceptions;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton(TimeProvider.System);

// Connexions aux bases de données
var pg = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:Postgres manquante");

var redisAddress = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:Redis manquante");

// PostgreSQL
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(pg, x =>
        x.MigrationsAssembly("TrocWear.Infrastructure"))
    .ConfigureWarnings(w =>
        w.Ignore(
            Microsoft.EntityFrameworkCore.Diagnostics
                .RelationalEventId.PendingModelChangesWarning)));

// MongoDB (représentation Guid à fixer avant la création du client)
MongoConfiguration.Register();
builder.Services.AddSingleton<IMongoClient>(_ =>
    new MongoClient(
        builder.Configuration.GetConnectionString("Mongo")
        ?? "mongodb://mongo:27017"));

// Repositories MongoDB
builder.Services.AddScoped<IMetierRepository, MongoMetierRepository>();

// Handlers des services metiers
builder.Services.AddScoped<
    IRequestHandler<SaveMetierCommand, MetierDocument>,
    SaveMetierHandler>();

builder.Services.AddScoped<
    IRequestHandler<FindMetierQuery, MetierDocument?>,
    FindMetierHandler>();

builder.Services.AddScoped<
    IRequestHandler<ListMetierQuery, List<MetierDocument>>,
    ListMetierHandler>();

builder.Services.AddScoped<
    IRequestHandler<DeleteMetierCommand, bool>,
    DeleteMetierHandler>();

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(redisAddress));

// Repositories PostgreSQL
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IFabriqueConnexion, FabriqueConnexionPostgres>();
builder.Services.AddScoped<IUserReadRepository, DapperUserReadRepository>();

// Transactions et événements
builder.Services.AddScoped<IOutboxWriter, PostgresOutboxWriter>();
builder.Services.AddScoped<IUnitOfWork, AuthUnitOfWork>();

// Services d'authentification
builder.Services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddScoped<ISessionRepository, RedisSessionRepository>();
builder.Services.AddScoped<ISessionService, SessionService>();

// Validation
builder.Services.AddScoped<RegisterUserValidator>();
builder.Services.AddScoped<ProfileValidator>();

builder.Services.AddScoped<IValidator<RegisterUserCommand>>(
    sp => sp.GetRequiredService<RegisterUserValidator>());

builder.Services.AddScoped<IValidator<UpdateProfileCommand>>(
    sp => sp.GetRequiredService<ProfileValidator>());

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(
        typeof(RegisterUserCommand).Assembly));

builder.Services.AddTransient(
    typeof(IPipelineBehavior<,>),
    typeof(ValidationBehavior<,>));

// Commandes et requêtes
builder.Services.AddScoped<
    ICommandHandler<RegisterUserCommand, UserResponse>,
    RegisterUserCommandHandler>();

builder.Services.AddScoped<
    ICommandHandler<LoginUserCommand, string>,
    LoginUserCommandHandler>();

builder.Services.AddScoped<
    ICommandHandler<LogoutUserCommand, bool>,
    LogoutUserCommandHandler>();

builder.Services.AddScoped<
    ICommandHandler<UpdateProfileCommand, UserResponse>,
    UpdateProfileCommandHandler>();

builder.Services.AddScoped<
    IQueryHandler<GetCurrentUserQuery, UserResponse>,
    GetCurrentUserQueryHandler>();

builder.Services.AddScoped<
    IQueryHandler<GetUserByIdQuery, UserResponse>,
    GetUserByIdQueryHandler>();

// Messagerie
builder.Services.AddScoped<IEventPublisher, RedisEventPublisher>();
builder.Services.AddHostedService<OutboxDispatcher>();

// Authentification Redis
builder.Services.AddAuthentication("RedisSession")
    .AddScheme<
        AuthenticationSchemeOptions,
        RedisSessionAuthenticationHandler>(
        "RedisSession", _ => { });

builder.Services.AddAuthorization();

// CORS
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:4200"];

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
            .WithMethods("GET", "POST", "PATCH", "PUT", "DELETE", "OPTIONS")
            .WithHeaders("Content-Type", "X-TrocWear-Request")
            .AllowCredentials()));

var app = builder.Build();

// Gestion des exceptions
app.UseExceptionHandler(error => error.Run(async ctx =>
{
    var ex = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;

    var status = ex switch
    {
        DomainException domain => domain.StatusCode,
        DbUpdateConcurrencyException => 409,
        DbUpdateException
        {
            InnerException: PostgresException { SqlState: "23505" }
        } => 409,
        _ => 500
    };

    if (status == 500)
        app.Logger.LogError(ex, "Unhandled API error");

    ctx.Response.StatusCode = status;
    ctx.Response.ContentType = "application/problem+json";

    await ctx.Response.WriteAsJsonAsync(new
    {
        type = "about:blank",
        title = status switch
        {
            401 => "Non autorisé",
            403 => "Accès interdit",
            404 => "Introuvable",
            409 => "Conflit",
            400 => "Requête invalide",
            _ => "Erreur interne"
        },
        status,
        detail = status == 500
            ? "Erreur interne du serveur."
            : ex?.Message
    });
}));

app.UseCors();

// Protection CSRF
app.Use(async (ctx, next) =>
{
    var path = ctx.Request.Path;
    var method = ctx.Request.Method;

    var isApiRoute = path.StartsWithSegments("/api");

    var isPublicAuthRoute =
        (path.Equals("/api/auth/register", StringComparison.OrdinalIgnoreCase) ||
         path.Equals("/api/auth/login", StringComparison.OrdinalIgnoreCase))
        && HttpMethods.IsPost(method);

    var isModification =
        HttpMethods.IsPost(method) ||
        HttpMethods.IsPatch(method) ||
        HttpMethods.IsPut(method) ||
        HttpMethods.IsDelete(method);

    if (isApiRoute && isModification)
    {
        var origin = ctx.Request.Headers.Origin.ToString();

        var originAllowed =
            string.IsNullOrEmpty(origin) ||
            string.Equals(
                origin,
                $"{ctx.Request.Scheme}://{ctx.Request.Host}",
                StringComparison.OrdinalIgnoreCase) ||
            allowedOrigins.Contains(
                origin,
                StringComparer.OrdinalIgnoreCase);

        var headerValid =
            ctx.Request.Headers["X-TrocWear-Request"].ToString() == "1";

        if (!originAllowed || (!isPublicAuthRoute && !headerValid))
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;

            await ctx.Response.WriteAsJsonAsync(new
            {
                type = "about:blank",
                title = "Requête CSRF refusée",
                status = 403
            });

            return;
        }
    }

    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Healthchecks
app.MapGet("/health/live", () =>
    Results.Ok(new { status = "alive" }));

app.MapGet("/health/ready", async (
    AuthDbContext db,
    IConnectionMultiplexer redis,
    IMongoClient mongo) =>
{
    try
    {
        if (!await db.Database.CanConnectAsync())
            return Results.StatusCode(503);

        await redis.GetDatabase().PingAsync();

        await mongo.GetDatabase("admin")
            .RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));

        return Results.Ok(new { status = "ready" });
    }
    catch
    {
        return Results.StatusCode(503);
    }
});

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Migrations PostgreSQL
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider
        .GetRequiredService<AuthDbContext>()
        .Database.MigrateAsync();
}

await app.RunAsync();
