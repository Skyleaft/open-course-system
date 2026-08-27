using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Modules.Catalog.Features.AddLesson;
using MonoSlice.Modules.Catalog.Features.AddSection;
using MonoSlice.Modules.Catalog.Features.DeleteCourse;
using MonoSlice.Modules.Catalog.Features.DeleteLesson;
using MonoSlice.Modules.Catalog.Features.DeleteSection;
using MonoSlice.Modules.Catalog.Features.GetLesson;
using MonoSlice.Modules.Catalog.Features.UpdateLesson;
using MonoSlice.Modules.Catalog.Features.UpdateSection;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;
using MonoSlice.Shared.Abstractions.Messaging;
using MonoSlice.Shared.Abstractions.Storage;
using NSubstitute;
using Xunit;

namespace MonoSlice.Modules.Catalog.Tests;

public class CourseManagementTests
{
    private readonly CoursesDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ICacheService _cacheService;
    private readonly IEventBus _eventBus;
    private readonly IObjectStorageService _storageService;
    private readonly ILogger<DeleteCourseCommandHandler> _logger;

    public CourseManagementTests()
    {
        var options = new DbContextOptionsBuilder<CoursesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.CreateVersion7().ToString())
            .Options;

        _dbContext = new CoursesDbContext(options);
        _currentUser = Substitute.For<ICurrentUser>();
        _cacheService = Substitute.For<ICacheService>();
        _eventBus = Substitute.For<IEventBus>();
        _storageService = Substitute.For<IObjectStorageService>();
        _logger = Substitute.For<ILogger<DeleteCourseCommandHandler>>();
    }

    [Fact]
    public async Task DeleteCourse_ShouldDeleteCourseAndPublishIntegrationEvent()
    {
        var instructorId = Guid.CreateVersion7();
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(instructorId);
        _currentUser.IsInRole("Admin").Returns(false);

        var course = Course.Create(
            instructorId,
            "Course to delete",
            "Desc",
            CourseAccessType.OpenFree,
            thumbnailUrl: "http://localhost:9000/branding-assets/courses/thumbnails/thumb123.jpg");
        var section = course.AddSection("Intro");
        section.AddLesson("Lesson 1", LessonType.Video, "https://storage/vid1", 10);
        _dbContext.Courses.Add(course);
        await _dbContext.SaveChangesAsync();

        var handler = new DeleteCourseCommandHandler(_dbContext, _cacheService, _currentUser, _eventBus, _storageService, _logger);
        var result = await handler.Handle(new DeleteCourseCommand(course.Id), CancellationToken.None);

        Assert.True(result.Success);
        var deleted = await _dbContext.Courses.FindAsync(course.Id);
        Assert.Null(deleted);

        await _storageService.Received(1).DeleteObjectAsync(
            "branding-assets",
            "courses/thumbnails/thumb123.jpg",
            Arg.Any<CancellationToken>());

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<CourseDeletedIntegrationEvent>(e => e.CourseId == course.Id && e.InstructorId == instructorId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteCourse_UnauthorizedInstructor_ShouldThrowForbiddenException()
    {
        var instructorId = Guid.CreateVersion7();
        var otherUserId = Guid.CreateVersion7();
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(otherUserId);
        _currentUser.IsInRole("Admin").Returns(false);

        var course = Course.Create(instructorId, "Owner Course", "Desc", CourseAccessType.OpenFree);
        _dbContext.Courses.Add(course);
        await _dbContext.SaveChangesAsync();

        var handler = new DeleteCourseCommandHandler(_dbContext, _cacheService, _currentUser, _eventBus, _storageService, _logger);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new DeleteCourseCommand(course.Id), CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task UpdateSection_ShouldUpdateTitleAndOrderIndex()
    {
        var instructorId = Guid.CreateVersion7();
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(instructorId);
        _currentUser.IsInRole("Admin").Returns(false);

        var course = Course.Create(instructorId, "Course", "Desc", CourseAccessType.OpenFree);
        var section = course.AddSection("Original Section Title");
        _dbContext.Courses.Add(course);
        await _dbContext.SaveChangesAsync();

        var handler = new UpdateSectionCommandHandler(_dbContext, _cacheService, _currentUser);
        var result = await handler.Handle(new UpdateSectionCommand
        {
            SectionId = section.Id,
            Title = "Updated Section Title",
            OrderIndex = 2
        }, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("Updated Section Title", result.Data!.Title);
        Assert.Equal(2, result.Data.OrderIndex);

        var updatedSection = await _dbContext.Sections.FindAsync(section.Id);
        Assert.NotNull(updatedSection);
        Assert.Equal("Updated Section Title", updatedSection.Title);
        Assert.Equal(2, updatedSection.OrderIndex);
    }

    [Fact]
    public async Task DeleteSection_ShouldRemoveSectionAndItsLessons()
    {
        var instructorId = Guid.CreateVersion7();
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(instructorId);
        _currentUser.IsInRole("Admin").Returns(false);

        var course = Course.Create(instructorId, "Course", "Desc", CourseAccessType.OpenFree);
        var section = course.AddSection("Section to delete");
        section.AddLesson("Lesson in section", LessonType.PdfDocument, "https://storage/doc1", 5);
        _dbContext.Courses.Add(course);
        await _dbContext.SaveChangesAsync();

        var handler = new DeleteSectionCommandHandler(_dbContext, _cacheService, _currentUser);
        var result = await handler.Handle(new DeleteSectionCommand(section.Id), CancellationToken.None);

        Assert.True(result.Success);
        var deleted = await _dbContext.Sections.FindAsync(section.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task GetAndUpdateAndDeleteLesson_ShouldWorkCorrectly()
    {
        var instructorId = Guid.CreateVersion7();
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(instructorId);
        _currentUser.IsInRole("Admin").Returns(false);

        var course = Course.Create(instructorId, "Course", "Desc", CourseAccessType.OpenFree);
        var section = course.AddSection("Section");
        var lesson = section.AddLesson("Original Lesson", LessonType.Video, "https://storage/vid1", 15);
        _dbContext.Courses.Add(course);
        await _dbContext.SaveChangesAsync();

        // 1. GetLesson
        var getHandler = new GetLessonQueryHandler(_dbContext);
        var getResult = await getHandler.Handle(new GetLessonQuery(lesson.Id), CancellationToken.None);
        Assert.True(getResult.Success);
        Assert.Equal("Original Lesson", getResult.Data!.Title);

        // 2. UpdateLesson with Text lesson (TextContent and null ContentUrl)
        var updateHandler = new UpdateLessonCommandHandler(_dbContext, _cacheService, _currentUser);
        var updateResult = await updateHandler.Handle(new UpdateLessonCommand
        {
            LessonId = lesson.Id,
            Title = "Updated Lesson Title",
            Type = LessonType.Text,
            TextContent = "{\"type\":\"doc\",\"content\":[{\"type\":\"paragraph\",\"content\":[{\"type\":\"text\",\"text\":\"Welcome to Rich Text Lesson\"}]}]}",
            ContentUrl = null,
            DurationMinutes = 25,
            OrderIndex = 1
        }, CancellationToken.None);

        Assert.True(updateResult.Success);
        Assert.Equal("Updated Lesson Title", updateResult.Data!.Title);
        Assert.Equal("Text", updateResult.Data.Type);
        Assert.Null(updateResult.Data.ContentUrl);
        Assert.NotNull(updateResult.Data.TextContent);
        Assert.Equal(25, updateResult.Data.DurationMinutes);

        // 3. DeleteLesson
        var deleteHandler = new DeleteLessonCommandHandler(_dbContext, _cacheService, _currentUser);
        var deleteResult = await deleteHandler.Handle(new DeleteLessonCommand(lesson.Id), CancellationToken.None);
        Assert.True(deleteResult.Success);

        var deleted = await _dbContext.Lessons.FindAsync(lesson.Id);
        Assert.Null(deleted);
    }
}
