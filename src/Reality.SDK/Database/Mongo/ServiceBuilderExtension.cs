using Microsoft.Extensions.DependencyInjection;

namespace Reality.SDK.Database.Mongo
{
    public static class ServiceBuilderExtension
    {
        public static ServiceBuilder UseMongo(this ServiceBuilder builder)
        {
            builder.Configure((b) =>
            {
                b.Services.AddSingleton<IDatabaseContext, DatabaseContext>();
                b.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            });

            return builder;
        }
    }
}