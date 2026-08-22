using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Communications.Domain;
using MonoSlice.Modules.Communications.Features.GetAnnouncement;
using MonoSlice.Modules.Communications.Features.GetAnnouncements;
using MonoSlice.Modules.Communications.Features.GetDiscussionThread;
using MonoSlice.Modules.Communications.Features.GetDiscussionThreads;
using MonoSlice.Modules.Communications.Persistence;
using Xunit;

namespace MonoSlice.Modules.Communications.Tests;

public class CommunicationsQueryHandlerTests
{
    private readonly CommunicationsDbContext _dbContext;

    public CommunicationsQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<CommunicationsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.CreateVersion7().ToString())
            .Options;

        _dbContext = new CommunicationsDbContext(options);
    }

    [Fact]
    public async Task GetAnnouncements_ShouldReturnGlobalAndCourseAnnouncements()
    {
        var courseId = Guid.CreateVersion7();
        var globalAnn = Announcement.Create(null, Guid.CreateVersion7(), "Global Ann", "Content", false);
        var courseAnn = Announcement.Create(courseId, Guid.CreateVersion7(), "Course Ann", "Content", true);
        var otherCourseAnn = Announcement.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), "Other Ann", "Content", false);

        await _dbContext.Announcements.AddRangeAsync(globalAnn, courseAnn, otherCourseAnn);
        await _dbContext.SaveChangesAsync();

        var handler = new GetAnnouncementsQueryHandler(_dbContext);
        var result = await handler.Handle(new GetAnnouncementsQuery(courseId, true), CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal("Course Ann", result.Data[0].Title); // Pinned comes first
        Assert.Equal("Global Ann", result.Data[1].Title);
    }

    [Fact]
    public async Task GetAnnouncementById_ShouldReturnAnnouncement()
    {
        var ann = Announcement.Create(null, Guid.CreateVersion7(), "Detail Title", "Detail Content", false);
        await _dbContext.Announcements.AddAsync(ann);
        await _dbContext.SaveChangesAsync();

        var handler = new GetAnnouncementByIdQueryHandler(_dbContext);
        var result = await handler.Handle(new GetAnnouncementByIdQuery(ann.Id), CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(ann.Id, result.Data.Id);
        Assert.Equal("Detail Title", result.Data.Title);
    }

    [Fact]
    public async Task GetDiscussionThreads_ShouldReturnPaginatedList()
    {
        var courseId = Guid.CreateVersion7();
        for (int i = 1; i <= 5; i++)
        {
            var thread = DiscussionThread.Create(courseId, null, Guid.CreateVersion7(), $"Thread {i}", "Content");
            await _dbContext.DiscussionThreads.AddAsync(thread);
        }
        await _dbContext.SaveChangesAsync();

        var handler = new GetDiscussionThreadsQueryHandler(_dbContext);
        var result = await handler.Handle(new GetDiscussionThreadsQuery(courseId, null, 1, 3), CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(5, result.Data.TotalCount);
        Assert.Equal(3, result.Data.Items.Count);
        Assert.Equal(2, result.Data.TotalPages);
    }

    [Fact]
    public async Task GetDiscussionThreadById_ShouldBuildNestedCommentHierarchy()
    {
        var courseId = Guid.CreateVersion7();
        var thread = DiscussionThread.Create(courseId, null, Guid.CreateVersion7(), "Hierarchy Thread", "Content");
        await _dbContext.DiscussionThreads.AddAsync(thread);
        await _dbContext.SaveChangesAsync();

        var topComment1 = ThreadComment.Create(thread.Id, Guid.CreateVersion7(), "Top Comment 1");
        var topComment2 = ThreadComment.Create(thread.Id, Guid.CreateVersion7(), "Top Comment 2");
        await _dbContext.ThreadComments.AddRangeAsync(topComment1, topComment2);
        await _dbContext.SaveChangesAsync();

        var childComment = ThreadComment.Create(thread.Id, Guid.CreateVersion7(), "Child of 1", topComment1.Id);
        await _dbContext.ThreadComments.AddAsync(childComment);
        await _dbContext.SaveChangesAsync();

        var grandChildComment = ThreadComment.Create(thread.Id, Guid.CreateVersion7(), "Grandchild of 1", childComment.Id);
        await _dbContext.ThreadComments.AddAsync(grandChildComment);
        await _dbContext.SaveChangesAsync();

        var handler = new GetDiscussionThreadByIdQueryHandler(_dbContext);
        var result = await handler.Handle(new GetDiscussionThreadByIdQuery(thread.Id), CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Comments.Count); // 2 root comments

        var root1 = result.Data.Comments.First(c => c.Id == topComment1.Id);
        Assert.Single(root1.Replies);
        Assert.Equal(childComment.Id, root1.Replies[0].Id);
        Assert.Single(root1.Replies[0].Replies);
        Assert.Equal(grandChildComment.Id, root1.Replies[0].Replies[0].Id);
    }
}
