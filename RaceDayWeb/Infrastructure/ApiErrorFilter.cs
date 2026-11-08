using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using RaceDayWeb.Services;

namespace RaceDayWeb.Infrastructure;

/// <summary>
/// Turns API failures that a controller did not handle into friendly pages, so a user never sees a raw error.
/// Forms catch their own 400 and 409 errors and show them next to the fields. This filter handles the rest.
/// </summary>
public class ApiErrorFilter(ITempDataDictionaryFactory tempDataFactory, ILogger<ApiErrorFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var http = context.HttpContext;
        var tempData = tempDataFactory.GetTempData(http);

        switch (context.Exception)
        {
            // The API session ended (timed out, or the API restarted). Log the user out here too and ask them to log in.
            case ApiException { IsUnauthorized: true }:
                http.Session.SignOut();
                tempData.FlashInfo("Your session has ended. Please log in again.");
                var returnUrl = http.Request.Method == HttpMethods.Get ? $"{http.Request.Path}{http.Request.QueryString}" : null;
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
                break;

            case ApiException { IsForbidden: true }:
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                break;

            case ApiException { IsNotFound: true } notFound:
                context.Result = ErrorView("NotFound", StatusCodes.Status404NotFound, notFound.Message, context);
                break;

            case ApiException other:
                context.Result = ErrorView("Error", StatusCodes.Status400BadRequest, other.Message, context);
                break;

            // The API is not running or cannot be reached.
            case HttpRequestException or TaskCanceledException:
                logger.LogWarning(context.Exception, "The RaceDay API could not be reached");
                context.Result = ErrorView("ServiceUnavailable", StatusCodes.Status503ServiceUnavailable, null, context);
                break;

            default:
                return;
        }

        context.ExceptionHandled = true;
    }

    private static ViewResult ErrorView(string viewName, int statusCode, string? message, ExceptionContext context)
    {
        var view = new ViewResult
        {
            ViewName = viewName,
            StatusCode = statusCode,
            ViewData = new ViewDataDictionary(new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(), context.ModelState)
        };
        view.ViewData["Message"] = message;
        return view;
    }
}
