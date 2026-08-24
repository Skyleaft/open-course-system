using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Assessments.Domain;
using MonoSlice.Modules.Assessments.Features.Admin.GetDeadLetters;
using MonoSlice.Modules.Assessments.Features.Admin.RedriveDeadLetter;
using MonoSlice.Modules.Assessments.Features.GetCertificate;
using MonoSlice.Modules.Assessments.Features.GetMyCertificates;
using MonoSlice.Modules.Assessments.Features.IssueCertificate;
using MonoSlice.Modules.Assessments.Features.VerifyCertificate;
using MonoSlice.Modules.Assessments.Persistence;
using MonoSlice.Shared.Abstractions.Interfaces;
using MonoSlice.Shared.Abstractions.Messaging;
using NSubstitute;
using Xunit;

namespace MonoSlice.Modules.Assessments.Tests;

public class AssessmentsCommandHandlerTests
{
    private readonly AssessmentsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IEventStreamPublisher _eventPublisher;

    public AssessmentsCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AssessmentsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.CreateVersion7().ToString())
            .Options;

        _dbContext = new AssessmentsDbContext(options);
        _currentUser = Substitute.For<ICurrentUser>();
        _eventPublisher = Substitute.For<IEventStreamPublisher>();
    }

    [Fact]
    public async Task IssueCertificate_ShouldPersistAndReturnDto()
    {
        var instructorId = Guid.CreateVersion7();
        var studentId = Guid.CreateVersion7();
        var courseId = Guid.CreateVersion7();

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(instructorId);
        _currentUser.Roles.Returns(["Instructor"]);

        var handler = new IssueCertificateCommandHandler(_dbContext, _currentUser);
        var result = await handler.Handle(new IssueCertificateCommand
        {
            StudentId = studentId,
            CourseId = courseId,
            FinalScore = 92m
        }, CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(studentId, result.Data.StudentId);
        Assert.Equal(courseId, result.Data.CourseId);
        Assert.Equal(92m, result.Data.FinalScore);
        Assert.Equal("Issued", result.Data.Status);
    }

    [Fact]
    public async Task VerifyCertificate_ValidHash_ShouldReturnVerified()
    {
        var cert = Certificate.Issue(Guid.CreateVersion7(), Guid.CreateVersion7(), 90m);
        await _dbContext.Certificates.AddAsync(cert);
        await _dbContext.SaveChangesAsync();

        var handler = new VerifyCertificateQueryHandler(_dbContext);
        var result = await handler.Handle(new VerifyCertificateQuery(cert.CertificateHash), CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.IsValid);
        Assert.Equal(cert.CertificateNumber, result.Data.CertificateNumber);
    }

    [Fact]
    public async Task GetMyCertificates_ShouldReturnListForCurrentUser()
    {
        var studentId = Guid.CreateVersion7();
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(studentId);

        var cert1 = Certificate.Issue(studentId, Guid.CreateVersion7(), 85m);
        var cert2 = Certificate.Issue(studentId, Guid.CreateVersion7(), 95m);
        var otherCert = Certificate.Issue(Guid.CreateVersion7(), Guid.CreateVersion7(), 70m);

        await _dbContext.Certificates.AddRangeAsync(cert1, cert2, otherCert);
        await _dbContext.SaveChangesAsync();

        var handler = new GetMyCertificatesQueryHandler(_dbContext, _currentUser);
        var result = await handler.Handle(new GetMyCertificatesQuery(), CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Count);
    }

    [Fact]
    public async Task RedriveDeadLetter_ShouldPublishAndMarkResolved()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.Roles.Returns(["Admin"]);

        var deadLetter = GradingDeadLetter.Create(
            "123-0",
            Guid.CreateVersion7(),
            "DB connection failed",
            "stack",
            payloadJson: "{\"ExamId\":\"" + Guid.CreateVersion7() + "\"}");

        await _dbContext.GradingDeadLetters.AddAsync(deadLetter);
        await _dbContext.SaveChangesAsync();

        var handler = new RedriveDeadLetterCommandHandler(_dbContext, _eventPublisher, _currentUser);
        var result = await handler.Handle(new RedriveDeadLetterCommand { Id = deadLetter.Id }, CancellationToken.None);

        Assert.True(result.Success);
        var updated = await _dbContext.GradingDeadLetters.FindAsync(deadLetter.Id);
        Assert.NotNull(updated);
        Assert.True(updated.IsResolved);

        await _eventPublisher.Received(1).PublishRawAsync(
            "stream:exam-events",
            Arg.Any<IDictionary<string, string>>(),
            Arg.Any<int?>(),
            Arg.Any<CancellationToken>());
    }
}
