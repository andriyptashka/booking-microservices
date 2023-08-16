namespace BuildingBlocks.Core.CQRS;
using MediatR;

public interface IQuery<out T> : IRequest<T>where T : notnull
{
}
