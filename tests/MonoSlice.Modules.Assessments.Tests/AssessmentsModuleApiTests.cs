using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Assessments.Contracts;
using MonoSlice.Modules.Assessments.Domain;
using MonoSlice.Modules.Assessments.Persistence;
using Xunit;

namespace MonoSlice.Modules.Assessments.Tests;

public class AssessmentsModuleApiTests
{
    private readonly AssessmentsDbContext _dbContext;
    private readonly AssessmentsModuleApi _api;

    public AssessmentsModuleApiTests()
    {
        var options = new DbContextOptionsBuilder<AssessmentsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.CreateVersion7().ToString())
            .Options;

        _dbContext = new AssessmentsDbContext(options);
        _api = new AssessmentsModuleApi(_dbContext);
    }

    [Fact]
    public async Task IssueCertificateAsync_ShouldPersistAndReturnDto()
    {
        var studentId = Guid.CreateVersion7();
        var courseId = Guid.CreateVersion7();

        var dto = await _api.IssueCertificateAsync(studentId, courseId, 95m);

        Assert.NotEqual(Guid.Empty, dto.Id);
        Assert.Equal(studentId, dto.StudentId);
        Assert.Equal(courseId, dto.CourseId);
        Assert.Equal(95m, dto.FinalScore);
        Assert.Equal("Issued", dto.Status);

        var queried = await _api.GetStudentCertificateAsync(studentId, courseId);
        Assert.NotNull(queried);
        Assert.Equal(dto.CertificateNumber, queried.CertificateNumber);
    }

    [Fact]
    public async Task GetStudentGradeRecordsAsync_ShouldReturnSavedGrades()
    {
        var studentId = Guid.CreateVersion7();
        var courseId = Guid.CreateVersion7();

        var g1 = GradeRecord.Create(studentId, courseId, GradeItemType.Quiz, Guid.CreateVersion7(), 80m, 100m);
        var g2 = GradeRecord.Create(studentId, courseId, GradeItemType.Assignment, Guid.CreateVersion7(), 90m, 100m);

        await _dbContext.GradeRecords.AddRangeAsync(g1, g2);
        await _dbContext.SaveChangesAsync();

        var list = await _api.GetStudentGradeRecordsAsync(studentId, courseId);

        Assert.Equal(2, list.Count);
    }
}
