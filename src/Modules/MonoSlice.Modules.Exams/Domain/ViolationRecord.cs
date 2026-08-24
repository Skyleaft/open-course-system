namespace MonoSlice.Modules.Exams.Domain;

public sealed record ViolationRecord(
    string Type,
    string Reason,
    DateTime TimestampUtc);
