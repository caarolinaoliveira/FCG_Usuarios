using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using FCG.Application.Interfaces;
using FCG.Domain.Interfaces;
using FCG.Infrastructure.Context;
using FCG.Infrastructure.Identity;
using FCG.Infrastructure.Logging;
using FCG.Infrastructure.Messaging;
using FCG.Infrastructure.Repository;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using RabbitMQ.Client;

namespace FCG.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<FcgDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly("FCG.Infrastructure")
                ));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<FcgDbContext>();

            services.AddScoped<IUsuarioRepository, UsuarioRepository>();

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration["Redis:ConnectionString"] ?? "localhost:6379";
            });

            // RabbitMQ
            services.AddSingleton<IConnection>(sp =>
            {
                var factory = new ConnectionFactory
                {
                    HostName = configuration["RabbitMQ:Host"] ?? "localhost",
                    Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
                    UserName = configuration["RabbitMQ:Usuario"] ?? "guest",
                    Password = configuration["RabbitMQ:Senha"] ?? "guest"
                };
                return factory.CreateConnectionAsync().GetAwaiter().GetResult();
            });

            services.AddScoped<IMessagePublisher, RabbitMQPublisher>();

            // SQS (trigger do NotificationsAPI serverless)
            services.AddSingleton<IAmazonSQS>(sp =>
            {
                var sqsConfig = new AmazonSQSConfig
                {
                    ServiceURL = configuration["AWS:SqsServiceUrl"] ?? "http://localhost:4566",
                    AuthenticationRegion = RegionEndpoint.USEast1.SystemName
                };
                return new AmazonSQSClient(new BasicAWSCredentials("test", "test"), sqsConfig);
            });

            services.AddScoped<ISqsPublisher, SqsPublisher>();

            services.AddSingleton<IMongoClient>(sp =>
                new MongoClient(configuration["MongoDB:ConnectionString"] ?? "mongodb://localhost:27017"));

            services.AddSingleton<IErroLogService, ErroLogService>();

            return services;
        }
    }
}