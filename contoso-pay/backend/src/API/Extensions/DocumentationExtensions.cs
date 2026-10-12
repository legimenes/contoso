using Asp.Versioning.ApiExplorer;
using Scalar.AspNetCore;

namespace API.Extensions;

internal static class DocumentationExtensions
{
    internal static IApplicationBuilder UseDocumentation(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi().WithDocumentPerVersion();
            app.MapScalarApiReference("docs/scalar", options =>
            {
                IReadOnlyList<ApiVersionDescription> descriptions = app.DescribeApiVersions();

                for (int i = 0; i < descriptions.Count; i++)
                {
                    ApiVersionDescription description = descriptions[i];
                    options.AddDocument(description.GroupName, description.GroupName, isDefault: i == descriptions.Count - 1);
                }
            });
        }
        return app;
    }
}