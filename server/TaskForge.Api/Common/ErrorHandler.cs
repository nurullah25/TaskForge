using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace TaskForge.Api.Common;

public class ErrorHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment,
    ILogger<ErrorHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            BadRequestException => (StatusCodes.Status400BadRequest, "Bad request"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden"),
            NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
            ConflictException or DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (StatusCodes.Status500InternalServerError, "Server error")
        };

        var detail = exception switch
        {
            DbUpdateConcurrencyException => "This item was changed by someone else. Reload it and try again.",
            _ when status == StatusCodes.Status500InternalServerError && !environment.IsDevelopment()
                => "Something went wrong on our side. Please try again later.",
            _ => exception.Message
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
        }

        context.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails =
            {
                Status = status,
                Title = title,
                Detail = detail
            }
        });
    }
}
