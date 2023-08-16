namespace BuildingBlocks.Core.CQRS;
using MediatR;

public interface ICommandHandler<TCommand> : ICommandHandler<TCommand, Unit> where TCommand : ICommand<Unit>
{
}

public interface ICommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
    where TResponse : notnull
{
}
