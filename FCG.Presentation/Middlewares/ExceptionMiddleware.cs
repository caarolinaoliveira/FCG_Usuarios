using FCG.Domain.Exceptions;
using System.Net;
using System.Text.Json;
namespace FCG.Presentation.Middlewares;
public class ExceptionMiddleware
{
   private readonly RequestDelegate _next;
   private readonly ILogger<ExceptionMiddleware> _logger;
   public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
   {
       _next = next;
       _logger = logger;
   }
   public async Task InvokeAsync(HttpContext context)
   {
       try
       {
           await _next(context);
       }
       catch (NotFoundException ex)
       {
           _logger.LogWarning(ex, "Recurso não encontrado");
           await WriteResponse(context, HttpStatusCode.NotFound, ex.Message);
       }
       catch (ConflictException ex)
       {
           _logger.LogWarning(ex, "Conflito de dados");
           await WriteResponse(context, HttpStatusCode.Conflict, ex.Message);
       }
       catch (UnauthorizedException ex)
       {
           _logger.LogWarning(ex, "Acesso não autorizado");
           await WriteResponse(context, HttpStatusCode.Unauthorized, ex.Message);
       }
       catch (Exception ex)
       {
           _logger.LogError(ex, "Erro inesperado");
           await WriteResponse(context, HttpStatusCode.InternalServerError, "Ocorreu um erro interno.");
       }
   }
   private static async Task WriteResponse(HttpContext context, HttpStatusCode status, string message)
   {
       context.Response.ContentType = "application/json";
       context.Response.StatusCode = (int)status;
       var body = JsonSerializer.Serialize(new { error = message });
       await context.Response.WriteAsync(body);
   }
}