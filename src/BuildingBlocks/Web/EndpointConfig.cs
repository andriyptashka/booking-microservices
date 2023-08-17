namespace BuildingBlocks.Web;

using Asp.Versioning.Builder;

public class EndpointConfig
{
    public const string BaseApiPath = "api/v{version:apiVersion}";
    public static ApiVersionSet VersionSet { get; private set; } = default!;
}
