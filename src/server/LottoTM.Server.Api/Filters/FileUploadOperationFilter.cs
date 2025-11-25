using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LottoTM.Server.Api.Filters;

/// <summary>
/// Swagger operation filter to handle file upload endpoints
/// Configures multipart/form-data content type for endpoints with IFormFile parameters
/// </summary>
public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Check if the operation has IFormFile parameters
        var formFileParams = context.ApiDescription.ParameterDescriptions
            .Where(p => p.Type == typeof(IFormFile) ||
                       p.Type == typeof(IEnumerable<IFormFile>))
            .ToList();

        if (!formFileParams.Any())
        {
            return;
        }

        // Clear existing parameters
        operation.Parameters?.Clear();

        // Set request body to multipart/form-data with file upload schema
        operation.RequestBody = new OpenApiRequestBody
        {
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, OpenApiSchema>
                        {
                            ["file"] = new OpenApiSchema
                            {
                                Type = "string",
                                Format = "binary",
                                Description = "CSV file to upload (max 1MB)"
                            }
                        },
                        Required = new HashSet<string> { "file" }
                    }
                }
            }
        };
    }
}
