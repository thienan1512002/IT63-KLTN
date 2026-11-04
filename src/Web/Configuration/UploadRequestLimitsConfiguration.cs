using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Web.Configuration;

public static class UploadRequestLimitsConfiguration
{
    /// <summary>
    /// Configure global upload request limits for multipart/form-data.
    /// Note: reverse proxies (IIS/Nginx) still need their own limits configured.
    /// </summary>
    public static WebApplicationBuilder ConfigureUploadRequestLimits(this WebApplicationBuilder builder, long maxUploadBytes)
    {
        // If not configured (or configured as 0), keep framework/server defaults.
        if (maxUploadBytes <= 0)
        {
            return builder;
        }

        // Model binding / multipart form limits
        builder.Services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = maxUploadBytes;
        });

        // Kestrel request body limit
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Limits.MaxRequestBodySize = maxUploadBytes;
        });

        return builder;
    }
}

