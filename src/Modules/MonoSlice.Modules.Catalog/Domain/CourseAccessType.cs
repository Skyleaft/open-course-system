using System.Text.Json.Serialization;

namespace MonoSlice.Modules.Catalog.Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CourseAccessType
{
    OpenFree,
    OpenPaid,
    PrivateWithKey
}
