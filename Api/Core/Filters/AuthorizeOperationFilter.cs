using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Api.Core.Filters
{
    public class AuthorizeOperationFilter : IOperationFilter
    {
        /// <inheritdoc />
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var isAuthorized = context.MethodInfo.DeclaringType != null && (context.MethodInfo.DeclaringType.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() ||
                                                                            context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any());

            if (!isAuthorized) return;

            // Try setting the Unauthorized and Forbidden API responses. Will only do something if the response hasn't already been set in the controller endpoint.
            operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
            operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden" });

            var oauth2SecurityScheme = new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" }
            };
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [oauth2SecurityScheme] = new[] { "wiser-api" }
            });
        }
    }
}