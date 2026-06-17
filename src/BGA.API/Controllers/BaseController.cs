using Microsoft.AspNetCore.Mvc;

namespace BGA.API.Controllers;

public class BaseController : ControllerBase
{
    protected static string ControllerName<T>() where T : ControllerBase
        => typeof(T).Name.Replace("Controller", "");
}
