using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Communications.Contracts;
using MonoSlice.Modules.Communications.Domain;
using MonoSlice.Modules.Communications.Persistence;
using Xunit;

namespace MonoSlice.Modules.Communications.Tests;

public class CommunicationsModuleApiTests
{
    private readonly CommunicationsDbContext _dbContext;
    private readonly CommunicationsModuleApi _api;

    public CommunicationsModuleApiTests()
    {
        var options = new DbContextOptionsBuilder<CommunicationsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.CreateVersion7().ToString())
            .Options;

        _dbContext = new CommunicationsDbContext(options);
        _api = new CommunicationsModuleApi(_dbContext);
    }

    [Fact]
    public async Task GetActiveAnnouncementsCountAsync_ShouldReturnCorrectCount()
    {
        var courseId = Guid.CreateVersion7();
        await _dbContext.Announcements.AddRangeAsync(
            Announcement.Create(null, Guid.CreateVersion7(), "Global", "Content"),
            Announcement.Create(courseId, Guid.CreateVersion7(), "Course 1", "Content"),
            Announcement.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), "Other Course", "Content")
        );
        await _dbContext.SaveChangesAsync();

        var globalAndCourseCount = await _api.GetActiveAnnouncementsCountAsync(courseId);
        Assert.Equal(2, globalAndCourseCount);

        var totalCount = await _api.GetActiveAnnouncementsCountAsync();
        Assert.Equal(3, totalCount);
    }

    [Fact]
    public async Task GetDiscussionThreadsCountAsync_ShouldReturnCountForCourseAndLesson()
    {
        var courseId = Guid.CreateVersion7();
        var lessonId = Guid.CreateVersion7();

        await _dbContext.DiscussionThreads.AddRangeAsync(
            DiscussionThread.Create(courseId, lessonId, Guid.CreateVersion7(), "Lesson Thread", "Content"),
            DiscussionThread.Create(courseId, null, Guid.CreateVersion7(), "Course General", "Content"),
            DiscussionThread.Create(Guid.CreateVersion7(), null, Guid.CreateVersion7(), "Other Course", "Content")
        );
        await _dbContext.SaveChangesAsync();

        var courseTotal = await _api.GetDiscussionThreadsCountAsync(courseId);
        Assert.Equal(2, courseTotal);

        var lessonTotal = await _api.GetDiscussionThreadsCountAsync(courseId, lessonId);
        Assert.Equal(1, lessonTotal);
    }

    [Fact]
    public async Task IsThreadOpenAsync_ShouldReturnTrueForOpenThreadAndFalseForClosed()
    {
        var openThread = DiscussionThread.Create(Guid.CreateVersion7(), null, Guid.CreateVersion7(), "Open", "Content");
        var closedThread = DiscussionThread.Create(Guid.CreateVersion7(), null, Guid.CreateVersion7(), "Closed", "Content");
        closedThread.Close(Guid.CreateVersion7());

        await _dbContext.DiscussionThreads.AddRangeAsync(openThread, closedThread);
        await _dbContext.SaveChangesAsync();

        Assert.True(await _api.IsThreadOpenAsync(openThread.Id));
        Assert.False(await _api.IsThreadOpenAsync(closedThread.Id));
        Assert.False(await _api.IsThreadOpenAsync(Guid.CreateVersion7()));
    }
}
