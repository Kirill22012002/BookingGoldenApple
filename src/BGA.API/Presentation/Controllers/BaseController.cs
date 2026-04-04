using BGA.API.Application;
using BGA.API.Presentation.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BGA.API.Presentation.Controllers;

public class BaseController : ControllerBase
{
    protected IActionResult ProblemResponse(ServiceResponse response)
    {
        return response.ValidationErrors.Count > 0
                ? ValidationProblem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "One or more validation errors occured",
                    type: StatusCodes.Status400BadRequest.GetProblemType(),
                    modelStateDictionary: response.ValidationErrors.ToModelStateDictionary())
                : Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not found",
                    type: StatusCodes.Status404NotFound.GetProblemType(),
                    detail: string.Join(". ", response.Errors));
    }

    protected static string ControllerName<T>() where T : ControllerBase
        => typeof(T).Name.Replace("Controller", "");
}
