using MonoSlice.Modules.Assessments.Domain;
using MonoSlice.Shared.Abstractions.Exceptions;
using Xunit;

namespace MonoSlice.Modules.Assessments.Tests;

public class CertificateDomainTests
{
    [Fact]
    public void Issue_ShouldGenerateValidCertificateWithSha256Hash()
    {
        var studentId = Guid.CreateVersion7();
        var courseId = Guid.CreateVersion7();
        var finalScore = 88.50m;

        var cert = Certificate.Issue(studentId, courseId, finalScore);

        Assert.NotEqual(Guid.Empty, cert.Id);
        Assert.Equal(studentId, cert.StudentId);
        Assert.Equal(courseId, cert.CourseId);
        Assert.Equal(finalScore, cert.FinalScore);
        Assert.Equal(CertificateStatus.Issued, cert.Status);
        Assert.StartsWith("CERT-", cert.CertificateNumber);
        Assert.NotEmpty(cert.CertificateHash);
        Assert.True(cert.VerifyAuthenticity());
    }

    [Fact]
    public void Revoke_ShouldUpdateStatusAndReason()
    {
        var studentId = Guid.CreateVersion7();
        var courseId = Guid.CreateVersion7();
        var cert = Certificate.Issue(studentId, courseId, 95m);

        cert.Revoke("Academic dishonesty during proctored exam");

        Assert.Equal(CertificateStatus.Revoked, cert.Status);
        Assert.Equal("Academic dishonesty during proctored exam", cert.RevocationReason);
    }

    [Fact]
    public void GradeRecord_Create_ShouldInitializeCorrectly()
    {
        var studentId = Guid.CreateVersion7();
        var courseId = Guid.CreateVersion7();
        var quizId = Guid.CreateVersion7();

        var grade = GradeRecord.Create(studentId, courseId, GradeItemType.Quiz, quizId, 85m, 100m);

        Assert.NotEqual(Guid.Empty, grade.Id);
        Assert.Equal(studentId, grade.StudentId);
        Assert.Equal(courseId, grade.CourseId);
        Assert.Equal(GradeItemType.Quiz, grade.ItemType);
        Assert.Equal(quizId, grade.ReferenceId);
        Assert.Equal(85m, grade.Score);
        Assert.Equal(100m, grade.MaxScore);
    }

    [Fact]
    public void GradingDeadLetter_CreateAndResolve_ShouldWork()
    {
        var submissionId = Guid.CreateVersion7();
        var dlq = GradingDeadLetter.Create("12345-0", submissionId, "Connection timeout", "stack...", retryCount: 3);

        Assert.NotEqual(Guid.Empty, dlq.Id);
        Assert.Equal("12345-0", dlq.StreamMessageId);
        Assert.Equal(submissionId, dlq.SubmissionId);
        Assert.False(dlq.IsResolved);

        dlq.MarkResolved();
        Assert.True(dlq.IsResolved);
    }
}
