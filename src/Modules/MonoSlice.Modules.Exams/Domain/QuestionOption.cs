namespace MonoSlice.Modules.Exams.Domain;

public sealed record QuestionOption(
    Guid Id,
    string Text,
    bool IsCorrect);
