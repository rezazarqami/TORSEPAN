using System.Net;
using System.Text.Json;
using FluentValidation;
using TORSEPAN.API.Models;

namespace TORSEPAN.API.Middlewares;

public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger)
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
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error");

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json; charset=utf-8";

            var response = new ErrorResponse
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Message = "خطای اعتبارسنجی",
                Errors = ex.Errors.Select(e => e.ErrorMessage)
            };

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found");

            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            context.Response.ContentType = "application/json; charset=utf-8";

            var response = new ErrorResponse
            {
                StatusCode = StatusCodes.Status404NotFound,
                Message = "اطلاعات مورد نظر پیدا نشد."
            };

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json; charset=utf-8";

            var response = new ErrorResponse
            {
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = "خطایی در سرور رخ داده است."
            };

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response));
        }
    }
}