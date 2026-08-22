using System.Text.Json;
using System.Text.Json.Serialization;
using MonoSlice.Shared.Abstractions.Common;

namespace MonoSlice.Shared.Infrastructure.Serialization;

[JsonSerializable(typeof(ApiResponse))]
[JsonSerializable(typeof(ApiResponse<string>))]
[JsonSerializable(typeof(Result))]
[JsonSerializable(typeof(Error))]
[JsonSerializable(typeof(IReadOnlyList<string>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(int))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class SharedJsonSerializerContext : JsonSerializerContext
{
    private static JsonSerializerOptions? _defaultOptions;

    public static JsonSerializerOptions DefaultOptions =>
        _defaultOptions ??= new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            TypeInfoResolver = Default
        };
}
