using System.ComponentModel.DataAnnotations;
using Klasly.API.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Klasly.API.Middleware;

/// <summary>
/// Turns the exceptions services throw into problem-details, so controllers stay one line per action.
/// Anything not listed here falls through to the default 500 (no stack trace or SQL in the body).
/// </summary>
public class ApiExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, detail) = exception switch
        {
            KeyNotFoundException e => (StatusCodes.Status404NotFound, e.Message),
            ValidationException e => (StatusCodes.Status400BadRequest, e.Message),
            ConflictException e => (StatusCodes.Status409Conflict, e.Message),
            // Backstop for a race between two saves: 2601/2627 = unique index / unique constraint violated.
            DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } } =>
                (StatusCodes.Status409Conflict, "That value is already used."),
            _ => (0, null)
        };
        if (status == 0) return false;

        context.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Detail = detail }
        });
    }
}
