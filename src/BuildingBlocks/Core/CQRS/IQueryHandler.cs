namespace BuildingBlocks.Core.CQRS;
using MediatR;

public interface IQueryHandler<in T, TR> : IRequestHandler<T, TR>
    where T : IQuery<TR>
    where TR : notnull
{
}
