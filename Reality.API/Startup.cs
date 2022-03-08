﻿using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using Reality.Common.Configurations;
using Reality.Common.Data;
using System.Reflection;

namespace Reality.API
{
    public class Startup
    {
        public IConfiguration Configuration { get; set; }

        public Startup(IWebHostEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddUserSecrets(Assembly.GetExecutingAssembly());

            Configuration = builder.Build();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var mongoConfig = new MongoDbConfiguration();
            Configuration.GetSection(nameof(MongoDbConfiguration)).Bind(mongoConfig);

            services.AddSingleton(mongoConfig);

            // Database Repositories
            var databaseContext = new DatabaseContext(mongoConfig);
            services.AddSingleton(_ => databaseContext);

            // Hangfire
            services
                .AddHangfire(configuration =>
                {
                    var client = databaseContext.GetClient();

                    configuration
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseMongoStorage(client, "hangfire", new MongoStorageOptions
                    {
                        MigrationOptions = new MongoMigrationOptions
                        {
                            MigrationStrategy = new MigrateMongoMigrationStrategy(),
                            BackupStrategy = new CollectionMongoBackupStrategy()
                        },
                        Prefix = "hangfire.mongo",
                        CheckConnection = true
                    });
                })
                .AddHangfireServer(serverOptions =>
                {
                    serverOptions.ServerName = "reality.hangfire.mongo1";
                });

            // GraphQL
            services
                .AddHttpClient("auth", (sp, client) =>
                {
                    client.BaseAddress = new Uri("http://127.0.0.1:5010/");
                });
            services
                .AddHttpClient("database", (sp, client) =>
                {
                    client.BaseAddress = new Uri("http://127.0.0.1:5020/");
                });

            services
                .AddGraphQLServer()
                .AddRemoteSchema("auth")
                .AddRemoteSchema("database");
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGraphQL("/api");
            });

            // app.UseHangfireDashboard();
        }
    }
}
