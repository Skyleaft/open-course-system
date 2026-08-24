using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Modules.Catalog.Features.ListCourses;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Interfaces;
using NSubstitute;
using Xunit;

namespace MonoSlice.Modules.Catalog.Tests;

public class ListCoursesQueryHandlerTests
{
    private readonly CoursesDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListCoursesQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<CoursesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.CreateVersion7().ToString())
            .Options;

        _dbContext = new CoursesDbContext(options);
        _currentUser = Substitute.For<ICurrentUser>();
    }

    [Fact]
    public async Task Handle_AnonymousUser_ShouldOnlyReturnPublishedCourses()
    {
        // Arrange
        _currentUser.IsAuthenticated.Returns(false);

        var instructorId = Guid.CreateVersion7();
        var publishedCourse = Course.Create(instructorId, "Published C#", "Learn C#", CourseAccessType.OpenFree);
        publishedCourse.Publish();

        var draftCourse = Course.Create(instructorId, "Draft C#", "Draft content", CourseAccessType.OpenFree);
        // draftCourse is not published

        _dbContext.Courses.AddRange(publishedCourse, draftCourse);
        await _dbContext.SaveChangesAsync();

        var handler = new ListCoursesQueryHandler(_dbContext, _currentUser);

        // Act - request with IsPublished = false (should be ignored for anonymous users)
        var query = new ListCoursesQuery { IsPublished = false };
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.TotalCount);
        Assert.Single(result.Data.Items);
        Assert.Equal("Published C#", result.Data.Items[0].Title);
        Assert.True(result.Data.Items[0].IsPublished);
    }

    [Fact]
    public async Task Handle_AdminUser_CanFilterUnpublishedCourses()
    {
        // Arrange
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.IsInRole("Admin").Returns(true);
        _currentUser.Roles.Returns(new List<string> { "Admin" });

        var instructorId = Guid.CreateVersion7();
        var publishedCourse = Course.Create(instructorId, "Published Course", "Desc", CourseAccessType.OpenFree);
        publishedCourse.Publish();

        var draftCourse = Course.Create(instructorId, "Draft Course", "Desc", CourseAccessType.OpenFree);

        _dbContext.Courses.AddRange(publishedCourse, draftCourse);
        await _dbContext.SaveChangesAsync();

        var handler = new ListCoursesQueryHandler(_dbContext, _currentUser);

        // Act - Admin queries unpublished only
        var query = new ListCoursesQuery { IsPublished = false };
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.TotalCount);
        Assert.Single(result.Data.Items);
        Assert.Equal("Draft Course", result.Data.Items[0].Title);
        Assert.False(result.Data.Items[0].IsPublished);
    }

    [Fact]
    public async Task Handle_InstructorUser_CanSeeAllCourses_WhenIsPublishedIsNull()
    {
        // Arrange
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.IsInRole("Instructor").Returns(true);
        _currentUser.Roles.Returns(new List<string> { "Instructor" });

        var instructorId = Guid.CreateVersion7();
        var publishedCourse = Course.Create(instructorId, "Published Course", "Desc", CourseAccessType.OpenFree);
        publishedCourse.Publish();

        var draftCourse = Course.Create(instructorId, "Draft Course", "Desc", CourseAccessType.OpenFree);

        _dbContext.Courses.AddRange(publishedCourse, draftCourse);
        await _dbContext.SaveChangesAsync();

        var handler = new ListCoursesQueryHandler(_dbContext, _currentUser);

        // Act - Instructor queries without IsPublished filter
        var query = new ListCoursesQuery { IsPublished = null };
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.TotalCount);
        Assert.Equal(2, result.Data.Items.Count);
    }

    [Fact]
    public async Task Handle_SearchTermAndAccessTypeFilter_ShouldFilterCorrectly()
    {
        // Arrange
        _currentUser.IsAuthenticated.Returns(false);

        var instructorId = Guid.CreateVersion7();
        var c1 = Course.Create(instructorId, "Mastering TypeScript", "Deep dive into types", CourseAccessType.OpenFree);
        c1.Publish();

        var c2 = Course.Create(instructorId, "Mastering Rust", "Memory safety and speed", CourseAccessType.OpenPaid, 150000m);
        c2.Publish();

        var c3 = Course.Create(instructorId, "Go Basics", "Learn Go concurrency", CourseAccessType.OpenFree);
        c3.Publish();

        _dbContext.Courses.AddRange(c1, c2, c3);
        await _dbContext.SaveChangesAsync();

        var handler = new ListCoursesQueryHandler(_dbContext, _currentUser);

        // Act - search for "Mastering" with OpenPaid
        var query = new ListCoursesQuery
        {
            SearchTerm = "Mastering",
            AccessType = "OpenPaid"
        };
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.TotalCount);
        Assert.Equal("Mastering Rust", result.Data.Items[0].Title);
    }

    [Fact]
    public async Task Handle_PriceRangeAndSorting_ShouldFilterAndSortCorrectly()
    {
        // Arrange
        _currentUser.IsAuthenticated.Returns(false);

        var instructorId = Guid.CreateVersion7();
        var c1 = Course.Create(instructorId, "Course A", "Desc", CourseAccessType.OpenPaid, 50000m);
        c1.Publish();

        var c2 = Course.Create(instructorId, "Course B", "Desc", CourseAccessType.OpenPaid, 150000m);
        c2.Publish();

        var c3 = Course.Create(instructorId, "Course C", "Desc", CourseAccessType.OpenPaid, 250000m);
        c3.Publish();

        _dbContext.Courses.AddRange(c1, c2, c3);
        await _dbContext.SaveChangesAsync();

        var handler = new ListCoursesQueryHandler(_dbContext, _currentUser);

        // Act - Price between 60k and 300k, sort by Price descending
        var query = new ListCoursesQuery
        {
            MinPrice = 60000m,
            MaxPrice = 300000m,
            SortBy = "price",
            SortOrder = "desc"
        };
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.TotalCount);
        Assert.Equal("Course C", result.Data.Items[0].Title);
        Assert.Equal(250000m, result.Data.Items[0].Price);
        Assert.Equal("Course B", result.Data.Items[1].Title);
        Assert.Equal(150000m, result.Data.Items[1].Price);
    }
}
