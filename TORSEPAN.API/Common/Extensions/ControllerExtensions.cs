using Microsoft.AspNetCore.Mvc;
using TORSEPAN.Application.Common.Results;

namespace TORSEPAN.API.Common.Extensions;

public static class ControllerExtensions
{
    public static ActionResult ToActionResult<T>(
        this ControllerBase controller,
        Result<T> result)
    {
        if (result.IsSuccess)
            return controller.Ok(result.Value);

        return result.Error.Code switch
        {
            "NOT_FOUND" => controller.NotFound(result.Error),

            "BOWL_NOT_FOUND" => controller.NotFound(result.Error),

            "VALIDATION_ERROR" => controller.BadRequest(result.Error),

            _ => controller.BadRequest(result.Error)
        };
    }

    public static ActionResult ToActionResult(
        this ControllerBase controller,
        Result result)
    {
        if (result.IsSuccess)
            return controller.Ok();

        return result.Error.Code switch
        {
            "NOT_FOUND" => controller.NotFound(result.Error),

            "BOWL_NOT_FOUND" => controller.NotFound(result.Error),

            "VALIDATION_ERROR" => controller.BadRequest(result.Error),

            _ => controller.BadRequest(result.Error)
        };
    }
}