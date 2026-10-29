namespace TrocWear.BuildingBlocks.CQRS;
public interface ICommand<out TResult> { }
public interface IQuery<out TResult> { }
public interface ICommandHandler<in TCommand,TResult> where TCommand:ICommand<TResult>
{ Task<TResult> Handle(TCommand command, CancellationToken cancellationToken = default); }
public interface IQueryHandler<in TQuery,TResult> where TQuery:IQuery<TResult>
{ Task<TResult> Handle(TQuery query, CancellationToken cancellationToken = default); }
public sealed record CommandResult(Guid CommandeId, string Statut);
