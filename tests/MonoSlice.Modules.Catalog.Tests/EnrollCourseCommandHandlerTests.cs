using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Modules.Catalog.Features.CreateCourse;
using MonoSlice.Modules.Catalog.Features.EnrollCourse;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Contracts;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;
using NSubstitute;
using Xunit;

namespace MonoSlice.Modules.Catalog.Tests;

public class EnrollCourseCommandHandlerTests
{
    private readonly CoursesDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IServiceProvider _serviceProvider;

    public EnrollCourseCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<CoursesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.CreateVersion7().ToString())
            .Options;

        _dbContext = new CoursesDbContext(options);
        _currentUser = Substitute.For<ICurrentUser>();
        _serviceProvider = Substitute.For<IServiceProvider>();
    }

    [Fact]
    public async Task Handle_ShouldEnrollStudent_WhenCourseIsOpenFree()
    {
        var studentId = Guid.CreateVersion7();
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(studentId);

        var course = Course.Create(Guid.CreateVersion7(), "Free Course", "Desc", CourseAccessType.OpenFree);
        await _dbContext.Courses.AddAsync(course);
        await _dbContext.SaveChangesAsync();

        var handler = new EnrollCourseCommandHandler(_dbContext, _currentUser, _serviceProvider);

        var command = new EnrollCourseCommand { CourseId = course.Id };
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(studentId, result.Data.UserId);
        Assert.Equal(course.Id, result.Data.CourseId);

        var enrollment = await _dbContext.Enrollments.FirstOrDefaultAsync(e => e.CourseId == course.Id && e.UserId == studentId);
        Assert.NotNull(enrollment);
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenPrivateWithKeyAndKeyIsInvalid()
    {
        var studentId = Guid.CreateVersion7();
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(studentId);

        var keyHash = CreateCourseCommandHandler.HashKey("CorrectKey123");
        var course = Course.Create(Guid.CreateVersion7(), "Private Course", "Desc", CourseAccessType.PrivateWithKey, 0m, keyHash);
        await _dbContext.Courses.AddAsync(course);
        await _dbContext.SaveChangesAsync();

        var handler = new EnrollCourseCommandHandler(_dbContext, _currentUser, _serviceProvider);

        var command = new EnrollCourseCommand { CourseId = course.Id, EnrollmentKey = "WrongKey" };

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_ShouldVerifyPayment_WhenCourseIsOpenPaid()
    {
        var studentId = Guid.CreateVersion7();
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(studentId);

        var course = Course.Create(Guid.CreateVersion7(), "Paid Course", "Desc", CourseAccessType.OpenPaid, 150000m);
        await _dbContext.Courses.AddAsync(course);
        await _dbContext.SaveChangesAsync();

        var paymentsApi = Substitute.For<IPaymentsModuleApi>();
        paymentsApi.HasUserPurchasedCourseAsync(studentId, course.Id, Arg.Any<CancellationToken>())
            .Returns(true);

        _serviceProvider.GetService(typeof(IPaymentsModuleApi)).Returns(paymentsApi);

        var handler = new EnrollCourseCommandHandler(_dbContext, _currentUser, _serviceProvider);

        var command = new EnrollCourseCommand { CourseId = course.Id };
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        var enrollment = await _dbContext.Enrollments.FirstOrDefaultAsync(e => e.CourseId == course.Id && e.UserId == studentId);
        Assert.NotNull(enrollment);
    }
}
