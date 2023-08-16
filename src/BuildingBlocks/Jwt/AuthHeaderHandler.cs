namespace BuildingBlocks.Jwt;

using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

public class AuthHeaderHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContext;

    public AuthHeaderHandler(IHttpContextAccessor httpContext)
    {
        _httpContext = httpContext;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var ct = (_httpContext?.HttpContext?.Request.Headers["Authorization"])?.ToString();

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ct?.Replace("Bearer ", ""));

        return base.SendAsync(request, cancellationToken);
    }
}
