namespace MonoSlice.Modules.Exams.Domain;

public sealed record QuestionOption(
    Guid Id,
    string Text,
    bool IsCorrect,
    decimal Points = 0m,
    decimal PenaltyPoints = 0m);
