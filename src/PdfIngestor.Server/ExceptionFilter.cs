using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PdfIngestor.App.Dto;

namespace PdfIngestor.Server;

public class ExceptionFilter : IAsyncExceptionFilter
{
    private readonly ILogger<ExceptionFilter> _logger;

    public ExceptionFilter(ILogger<ExceptionFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnExceptionAsync(ExceptionContext context)
    {
        _logger.LogError("An exception occurred: {ContextException}", context.Exception);
        
        const HttpStatusCode statusCode = HttpStatusCode.InternalServerError;
        const string message = "An internal server error occurred.";
        
        WriteResponse(context, statusCode, message);
        
        await Task.CompletedTask;
    }

    private static void WriteResponse(ExceptionContext context,
        HttpStatusCode  statusCode,
        string message)
    {
        if (context.Exception is ValidationException validationException)
        {
            statusCode = HttpStatusCode.BadRequest;
            message = validationException.Message;
        }
        else if (context.Exception is KeyNotFoundException keyNotFoundException)
        {
            statusCode = HttpStatusCode.NotFound;
            message = keyNotFoundException.Message;
        }
        else if (context.Exception is ArgumentException argumentException)
        {
            statusCode = HttpStatusCode.BadRequest;
            message = argumentException.Message;
        }

        var response = Result<bool>.Failure(message); 

        context.Result = new ObjectResult(response)
        {
            StatusCode = (int)statusCode
        };

        context.ExceptionHandled = true;
    }
}

