using System.Text.Json;
using System.Text.Json.Nodes;
using GeorgiaERP.Api.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GeorgiaERP.Api.Filters;

public class LocalizedResponseFilter : IAsyncResultFilter
{
    private static readonly Dictionary<string, string> KaFieldMap = new()
    {
        ["name"] = "nameKa",
        ["firstName"] = "firstNameKa",
        ["lastName"] = "lastNameKa",
        ["description"] = "descriptionKa"
    };

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        var lang = context.HttpContext.GetLanguage();

        if (lang == "ka" && context.Result is ObjectResult objectResult && objectResult.Value is not null)
        {
            var json = JsonSerializer.Serialize(objectResult.Value, JsonOptions);
            var node = JsonNode.Parse(json);
            if (node is not null)
            {
                ApplyLocalization(node);
                objectResult.Value = node;
            }
        }

        await next();
    }

    private static void ApplyLocalization(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            foreach (var (enField, kaField) in KaFieldMap)
            {
                if (obj.ContainsKey(kaField) && obj[kaField] is JsonValue kaValue)
                {
                    var kaStr = kaValue.GetValue<string?>();
                    if (!string.IsNullOrEmpty(kaStr) && obj.ContainsKey(enField))
                    {
                        obj[enField] = kaStr;
                    }
                }
            }

            foreach (var prop in obj.ToArray())
            {
                if (prop.Value is JsonObject or JsonArray)
                    ApplyLocalization(prop.Value);
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                if (item is not null) ApplyLocalization(item);
            }
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
