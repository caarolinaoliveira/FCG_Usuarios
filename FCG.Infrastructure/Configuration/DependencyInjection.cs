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
            // Lidos aqui (e não dentro das lambdas abaixo) para a API falhar já no startup, e não no primeiro uso.
            var connectionString = ObterObrigatorio(configuration, "ConnectionStrings:DefaultConnection");
            var rabbitUsuario = ObterObrigatorio(configuration, "RabbitMQ:Usuario");
            var rabbitSenha = ObterObrigatorio(configuration, "RabbitMQ:Senha");

            services.AddDbContext<FcgDbContext>(options =>
                options.UseSqlServer(
                    connectionString,
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
                    UserName = rabbitUsuario,
                    Password = rabbitSenha
                };
                return factory.CreateConnectionAsync().GetAwaiter().GetResult();
            });

            services.AddScoped<IMessagePublisher, RabbitMQPublisher>();

            services.AddSingleton<IMongoClient>(sp =>
                new MongoClient(configuration["MongoDB:ConnectionString"] ?? "mongodb://localhost:27017"));

            services.AddSingleton<IErroLogService, ErroLogService>();

            return services;
        }

        private static string ObterObrigatorio(IConfiguration configuration, string chave)
        {
            var valor = configuration[chave];

            if (string.IsNullOrWhiteSpace(valor))
                throw new InvalidOperationException(
                    $"Configuração obrigatória '{chave}' não encontrada. " +
                    $"Defina via 'dotnet user-secrets' (desenvolvimento) ou pela variável de ambiente " +
                    $"'{chave.Replace(":", "__")}' (produção, injetada pelo Azure Key Vault).");

            return valor;
        }
    }
}
