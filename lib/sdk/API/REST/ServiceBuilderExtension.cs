using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Foundation.Core.SDK.API.REST;

public static class ServiceBuilderExtension
{
    public static ServiceBuilder UseREST(this ServiceBuilder builder, string? path = null, bool enableWebSocket = false, bool enableSwagger = false, Action<IServiceCollection, ServiceBuilder>? configure = null)
    {
        builder.Configure((WebApplicationBuilder b) =>
        {
            b.Services.AddControllers();

            b.Services.AddOpenApiDocument();

            configure?.Invoke(b.Services, builder);
        });

        builder.Configure((WebApplication app) =>
        {
            app.MapControllers();

            if (enableSwagger)
            {
                app.UseOpenApi();
                app.UseSwaggerUi3();
            }

            if (enableWebSocket)
                app.UseWebSockets();
        });

        return builder;
    }
}
