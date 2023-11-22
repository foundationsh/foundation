using Foundation.Common.Configurations;
using Foundation.Common.Entities;
using Foundation.Core.SDK;
using Foundation.Core.SDK.API.GraphQL;
using Foundation.Core.SDK.API.REST;
using Foundation.Core.SDK.Auth.JWT;
using Foundation.Core.SDK.Database.Mongo;
using Foundation.Services.UPx.Services;
using Foundation.Services.UPx.Types;

new ServiceBuilder(args)
    .WithName("UPx")
    .BindConfiguration<IBaseConfiguration, BaseConfiguration>()
    .UseMongo()
    .UseREST(enableWebSocket: true, enableSwagger: true)
    .UseGraphQL("/gql", (server, services, builder) =>
    {
        server
            .AddQueryType<Query>()
            .AddMutationType<Mutation>();

        server
            .AddType<Use>()
            .AddType<Resume>();

        server
            .ModifyRequestOptions(o =>
            {
                o.IncludeExceptionDetails = true;
                o.Complexity.Enable = true;
                o.Complexity.MaximumAllowed = 250;
            });
    })
    .Configure((WebApplicationBuilder builder) => {
        builder.Services.AddSingleton<IEcobucksLocationService, EcobucksLocationService>();
    })
    .UseJWT()
    .Build()
    .Run();