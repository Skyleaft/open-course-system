using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Modules.Catalog.Features.CreateCourse;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Interfaces;
using NSubstitute;
using Xunit;

namespace MonoSlice.Modules.Catalog.Tests;

public class CreateCourseCommandHandlerTests
{
    private readonly CoursesDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreateCourseCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<CoursesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.CreateVersion7().ToString())
            .Options;

        _dbContext = new CoursesDbContext(options);
        _currentUser = Substitute.For<ICurrentUser>();
    }

    [Fact]
    public async Task Handle_ShouldCreateCourseAndReturnDto()
    {
        var instructorId = Guid.CreateVersion7();
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(instructorId);

        var handler = new CreateCourseCommandHandler(_dbContext, _currentUser);

        var command = new CreateCourseCommand
        {
            Title = "ASP.NET Core Masterclass",
            Description = "Full stack masterclass",
            AccessType = CourseAccessType.OpenPaid,
            Price = 299000m
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(instructorId, result.Data.InstructorId);
        Assert.Equal("ASP.NET Core Masterclass", result.Data.Title);
        Assert.Equal("OpenPaid", result.Data.AccessType);
        Assert.Equal(299000m, result.Data.Price);

        var courseInDb = await _dbContext.Courses.FirstOrDefaultAsync(c => c.Id == result.Data.Id);
        Assert.NotNull(courseInDb);
        Assert.Equal(instructorId, courseInDb.InstructorId);
    }

    [Fact]
    public async Task Handle_ShouldHashEnrollmentKey_WhenPrivateWithKey()
    {
        var instructorId = Guid.CreateVersion7();
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(instructorId);

        var handler = new CreateCourseCommandHandler(_dbContext, _currentUser);

        var command = new CreateCourseCommand
        {
            Title = "Internal Company Training",
            AccessType = CourseAccessType.PrivateWithKey,
            EnrollmentKey = "SecretKey123"
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        var courseInDb = await _dbContext.Courses.FirstOrDefaultAsync(c => c.Id == result.Data!.Id);
        Assert.NotNull(courseInDb);
        Assert.NotNull(courseInDb.EnrollmentKeyHash);
        Assert.Equal(CreateCourseCommandHandler.HashKey("SecretKey123"), courseInDb.EnrollmentKeyHash);
    }
}
