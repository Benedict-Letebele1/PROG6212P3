using Microsoft.AspNetCore.Mvc;

namespace RaceDayWeb.Controllers;

/// <summary>The home page and the friendly error pages.</summary>
public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>Shown by the exception handler outside Development, so no stack trace reaches the user.</summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View();
    }

    /// <summary>Shown for status codes such as 404 when a URL does not match any page.</summary>
    [Route("Home/Status/{code:int}")]
    public IActionResult Status(int code)
    {
        Response.StatusCode = code;
        return View(code == StatusCodes.Status404NotFound ? "NotFound" : "Error");
    }
}
