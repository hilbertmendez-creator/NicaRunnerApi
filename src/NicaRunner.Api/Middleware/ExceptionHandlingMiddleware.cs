using System.Net;
using System.Text.Json;
using NicaRunner.Application.Common.Exceptions;

namespace NicaRunner.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (NotFoundException ex)
        {
            await WriteProblemAsync(context, HttpStatusCode.NotFound, ex.Message);
        }
        catch (ConflictException ex)
        {
            await WriteProblemAsync(context, HttpStatusCode.Conflict, ex.Message);
        }
        catch (InvalidCredentialsException ex)
        {
            await WriteProblemAsync(context, HttpStatusCode.Unauthorized, ex.Message);
        }
    }

    private static Task WriteProblemAsync(HttpContext context, HttpStatusCode statusCode, string detail)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var problem = new { status = (int)statusCode, title = statusCode.ToString(), detail };
        return context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
