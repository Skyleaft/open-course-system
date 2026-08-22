using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Exams.Contracts;
using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Interfaces;
using NSubstitute;
using Xunit;

namespace MonoSlice.Modules.Exams.Tests;

public class ExamsModuleApiTests
{
    [Fact]
    public async Task GetExamByIdAsync_ShouldReturnContractDto()
    {
        var options = new DbContextOptionsBuilder<ExamsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.CreateVersion7().ToString())
            .Options;

        var dbContext = new ExamsDbContext(options);
        var cacheService = Substitute.For<ICacheService>();

        var exam = QuizExam.Create(Guid.CreateVersion7(), "System Architecture Exam", "Desc", QuizMode.RealExam, 60, 70m);
        await dbContext.Exams.AddAsync(exam);
        await dbContext.SaveChangesAsync();

        var api = new ExamsModuleApi(dbContext, cacheService);
        var contract = await api.GetExamByIdAsync(exam.Id);

        Assert.NotNull(contract);
        Assert.Equal(exam.Id, contract.Id);
        Assert.Equal("System Architecture Exam", contract.Title);
        Assert.Equal("RealExam", contract.Mode);
    }
}
