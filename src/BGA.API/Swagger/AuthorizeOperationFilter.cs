using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BGA.API.Swagger;

public sealed class AuthorizeOperationFilter : IOperationFilter
{
    private static readonly OpenApiDocument SecurityDocument = new()
    {
        Components = new OpenApiComponents
        {
            SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
            {
                [JwtBearerDefaults.AuthenticationScheme] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = JwtBearerDefaults.AuthenticationScheme.ToLowerInvariant(),
                    BearerFormat = "JWT"
                }
            }
        }
    };

    private static readonly OpenApiSecuritySchemeReference BearerSecurityScheme =
        new(JwtBearerDefaults.AuthenticationScheme, SecurityDocument);

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (HasAllowAnonymous(context))
        {
            return;
        }

        if (!HasAuthorize(context))
        {
            return;
        }

        operation.Security ??= new List<OpenApiSecurityRequirement>();
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            {
                BearerSecurityScheme,
                new List<string>()
            }
        });

        operation.Responses ??= new OpenApiResponses();
        operation.Responses.TryAdd(StatusCodes.Status401Unauthorized.ToString(), new OpenApiResponse
        {
            Description = "Unauthorized"
        });

        operation.Responses.TryAdd(StatusCodes.Status403Forbidden.ToString(), new OpenApiResponse
        {
            Description = "Forbidden"
        });
    }

    private static bool HasAuthorize(OperationFilterContext context) =>
        context.MethodInfo.IsDefined(typeof(AuthorizeAttribute), inherit: true) ||
        context.MethodInfo.DeclaringType?.IsDefined(typeof(AuthorizeAttribute), inherit: true) == true;

    private static bool HasAllowAnonymous(OperationFilterContext context) =>
        context.MethodInfo.IsDefined(typeof(AllowAnonymousAttribute), inherit: true) ||
        context.MethodInfo.DeclaringType?.IsDefined(typeof(AllowAnonymousAttribute), inherit: true) == true;
}
