using Microsoft.AspNetCore.Mvc;
using BGA.API.Presentation.Extensions;
using BGA.API.Application;
using Microsoft.EntityFrameworkCore;

namespace BGA.API.Presentation.Controllers;

public class BaseController : ControllerBase
{
    protected IActionResult ProblemResponse(ServiceResponse response)
    {
        switch (response.Exception, response.ErrorType)
        {
            case (DbUpdateConcurrencyException, ServiceErrorType.None):
            case (_, ServiceErrorType.NotFound):
                {
                    return Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Not found",
                        type: StatusCodes.Status404NotFound.GetProblemType(),
                        detail: string.Join(". ", response.Errors)
                    );
                }
            case (_, ServiceErrorType.Validation):
                {
                    return ValidationProblem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "One or more validation errors occured",
                        type: StatusCodes.Status400BadRequest.GetProblemType(),
                        modelStateDictionary: response.ValidationErrors.ToModelStateDictionary()
                    );
                }
            default:
                {
                    return Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "An error occured",
                        type: StatusCodes.Status500InternalServerError.GetProblemType(),
                        detail: string.Join(". ", response.Errors)
                    );
                }
        }
    }

    protected static string ControllerName<T>() where T : ControllerBase
        => typeof(T).Name.Replace("Controller", "");
}
