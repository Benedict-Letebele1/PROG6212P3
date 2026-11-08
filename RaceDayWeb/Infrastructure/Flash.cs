using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace RaceDayWeb.Infrastructure;

/// <summary>One-time messages shown at the top of the next page, such as "Event created".</summary>
public static class Flash
{
    public const string SuccessKey = "Flash.Success";
    public const string ErrorKey = "Flash.Error";
    public const string InfoKey = "Flash.Info";

    public static void FlashSuccess(this Controller controller, string message) => controller.TempData[SuccessKey] = message;

    public static void FlashError(this Controller controller, string message) => controller.TempData[ErrorKey] = message;

    public static void FlashInfo(this ITempDataDictionary tempData, string message) => tempData[InfoKey] = message;
}
