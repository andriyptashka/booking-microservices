namespace BuildingBlocks.Core.CQRS;

using MediatR;

public interface ICommand : ICommand<Unit>
{
}

public interface ICommand<out T> : IRequest<T> where T : notnull
{
}
