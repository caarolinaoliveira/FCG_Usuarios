// FCG.Presentation/DependencyInjection.cs
using System.Text;
using System.Text.Json.Serialization;
using FCG.Application.Configuration;
using FCG.Presentation.Swagger;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace FCG.Presentation
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPresentation(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(
                        new JsonStringEnumConverter(
                            namingPolicy: null,
                            allowIntegerValues: true));
                });

            services.AddEndpointsApiExplorer();

            services.AddSwaggerGen(c =>
            {
                c.SchemaFilter<EnumSchemaFilter>();
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Insira o token JWT: ",
                    Name = "Authorization",
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            var jwtSettingsSection = configuration.GetSection("JwtSettings");
            services.Configure<JwtSettings>(jwtSettingsSection);

            var jwtSettings = jwtSettingsSection.Get<JwtSettings>() ?? new JwtSettings();
            var segredo = ObterSegredoJwt(jwtSettings);
            var key = Encoding.UTF8.GetBytes(segredo);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audiencia,
                    ValidIssuer = jwtSettings.Emissor,
                    ValidateIssuerSigningKey = true
                };
            });

            return services;
        }

        private const int TamanhoMinimoSegredoJwt = 32;

        private static string ObterSegredoJwt(JwtSettings jwtSettings)
        {
            if (string.IsNullOrWhiteSpace(jwtSettings.Segredo))
                throw new InvalidOperationException(
                    "Configuração obrigatória 'JwtSettings:Segredo' não encontrada. " +
                    "Defina via 'dotnet user-secrets' (desenvolvimento) ou pela variável de ambiente " +
                    "'JwtSettings__Segredo' (produção, injetada pelo Azure Key Vault).");

            if (jwtSettings.Segredo.Length < TamanhoMinimoSegredoJwt)
                throw new InvalidOperationException(
                    $"'JwtSettings:Segredo' deve ter no mínimo {TamanhoMinimoSegredoJwt} caracteres " +
                    $"(valor atual tem {jwtSettings.Segredo.Length}).");

            return jwtSettings.Segredo;
        }
    }
}