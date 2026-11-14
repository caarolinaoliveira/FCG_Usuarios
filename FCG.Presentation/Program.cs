using System.Diagnostics;
using FCG.Application.Configuration;
using FCG.Infrastructure;
using FCG.Infrastructure.Context;
using FCG.Presentation;
using FCG.Presentation.Logging;
using FCG.Presentation.Middlewares;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


ActivitySource.AddActivityListener(new ActivityListener
{
    ShouldListenTo = _ => true,
    Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
});

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole(options => options.FormatterName = "compact")
    .AddConsoleFormatter<CompactConsoleFormatter, Microsoft.Extensions.Logging.Console.ConsoleFormatterOptions>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPresentation(builder.Configuration);
builder.Services.AddHealthChecks();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FcgDbContext>(); 
    db.Database.Migrate();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roles = ["Usuario", "Admin"];
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
}


// Swagger: sempre em Development; fora dele, só com Swagger:Habilitado=true. Swagger:RoutePrefix muda o caminho da UI e do JSON (padrão "swagger").
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Habilitado"))
{
    var swaggerPrefixo = app.Configuration["Swagger:RoutePrefix"]?.Trim('/');
    if (string.IsNullOrWhiteSpace(swaggerPrefixo))
        swaggerPrefixo = "swagger";

    app.UseSwagger(c => c.RouteTemplate = swaggerPrefixo + "/{documentName}/swagger.json");
    app.UseSwaggerUI(c =>
    {
        c.RoutePrefix = swaggerPrefixo;
        // Caminho relativo (sem "/" inicial): resolve a partir de onde a UI foi aberta, em qualquer prefixo.
        c.SwaggerEndpoint("v1/swagger.json", "FCG Usuários v1");
    });
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();