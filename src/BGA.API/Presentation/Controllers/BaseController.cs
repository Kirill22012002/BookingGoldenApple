using Microsoft.AspNetCore.Mvc;

namespace BGA.API.Presentation.Controllers;

public class BaseController : ControllerBase
{
    protected static string ControllerName<T>() where T : ControllerBase
        => typeof(T).Name.Replace("Controller", "");
}
