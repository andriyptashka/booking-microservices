using EasyCaching.Core;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Caching
{
    public class InvalidateCachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull, IRequest<TResponse>
        where TResponse : notnull
    {
        private readonly ILogger<InvalidateCachingBehavior<TRequest, TResponse>> _logger;
        private readonly IEasyCachingProvider _cachingProvider;

        public InvalidateCachingBehavior(IEasyCachingProviderFactory cachingFactory,
            ILogger<InvalidateCachingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
            _cachingProvider = cachingFactory.GetCachingProvider("mem");
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (request is not IInvalidateCacheRequest invalidateCacheRequest)
            {
                return await next();
            }

            var cacheKey = invalidateCacheRequest.CacheKey;
            var response = await next();

            await _cachingProvider.RemoveAsync(cacheKey);

            return response;
        }
    }
}
