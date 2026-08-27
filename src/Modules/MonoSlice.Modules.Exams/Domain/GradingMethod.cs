using System.Text.Json.Serialization;

namespace MonoSlice.Modules.Exams.Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GradingMethod
{
    AllOrNothing,
    PartialWithPenalty,
    PartialWithoutPenalty,
    OptionWeighted
}
