using System.Text.Json.Serialization;

namespace MonoSlice.Modules.Exams.Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum QuestionType
{
    SingleChoice,
    MultipleChoice,
    Essay,
    TrueFalse
}
