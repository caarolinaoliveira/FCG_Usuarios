using FCG.Application.Interfaces;
using FCG.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using FCG.Application.Services.Autenticacao;


namespace FCG.Application.Configuration
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IUsuarioService, UsuarioService>();
            services.AddScoped<ITokenService, TokenService>();

            return services;
        }
    }
}