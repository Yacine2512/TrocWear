using System;
using AuthService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace AuthService.Infrastructure.Persistence.Migrations;
[DbContext(typeof(AuthDbContext))]
[Migration("202610260001_InitialAuth")]
public sealed class InitialAuth : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("users", columns: table => new
        {
            Id = table.Column<Guid>(nullable: false),
            Pseudonyme = table.Column<string>(maxLength: 60, nullable: false),
            Email = table.Column<string>(maxLength: 254, nullable: false),
            PasswordHash = table.Column<string>(nullable: false),
            AdresseParDefaut = table.Column<string>(maxLength: 1000, nullable: true),
            Status = table.Column<string>(maxLength: 32, nullable: false),
            Version = table.Column<long>(nullable: false),
            CreatedAt = table.Column<DateTimeOffset>(type:"timestamp with time zone",nullable:false),
            UpdatedAt = table.Column<DateTimeOffset>(type:"timestamp with time zone",nullable:false)
        }, constraints: table => table.PrimaryKey("PK_users", x => x.Id));
        migrationBuilder.CreateIndex("IX_users_Email", "users", "Email", unique: true);
        migrationBuilder.Sql("CREATE UNIQUE INDEX \"IX_users_Email_ci\" ON users (lower(\"Email\"));");
        migrationBuilder.CreateTable("outbox_messages", columns: table => new
        {
            Id = table.Column<Guid>(nullable: false),
            EventType = table.Column<string>(maxLength: 100, nullable: false),
            Payload = table.Column<string>(type:"jsonb", nullable:false),
            CreatedAt = table.Column<DateTimeOffset>(type:"timestamp with time zone",nullable:false),
            PublishedAt = table.Column<DateTimeOffset>(type:"timestamp with time zone",nullable:true),
            NextAttemptAt = table.Column<DateTimeOffset>(type:"timestamp with time zone",nullable:false),
            Attempts = table.Column<int>(nullable:false),
            LastError = table.Column<string>(maxLength:1000,nullable:true)
        }, constraints: table => table.PrimaryKey("PK_outbox_messages", x => x.Id));
        migrationBuilder.CreateIndex("IX_outbox_messages_PublishedAt_NextAttemptAt",
            "outbox_messages", new[] { "PublishedAt", "NextAttemptAt" });
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    { migrationBuilder.DropTable("outbox_messages"); migrationBuilder.DropTable("users"); }
}
