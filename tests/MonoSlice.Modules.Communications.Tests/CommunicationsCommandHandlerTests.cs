using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Communications.Domain;
using MonoSlice.Modules.Communications.Features.CloseDiscussionThread;
using MonoSlice.Modules.Communications.Features.CreateAnnouncement;
using MonoSlice.Modules.Communications.Features.CreateDiscussionThread;
using MonoSlice.Modules.Communications.Features.PostThreadComment;
using MonoSlice.Modules.Communications.Persistence;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;
using NSubstitute;
using Xunit;

namespace MonoSlice.Modules.Communications.Tests;

public class CommunicationsCommandHandlerTests
{
    private readonly CommunicationsDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CommunicationsCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<CommunicationsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.CreateVersion7().ToString())
            .Options;

        _dbContext = new CommunicationsDbContext(options);
        _currentUser = Substitute.For<ICurrentUser>();
    }

    [Fact]
    public async Task CreateAnnouncement_ShouldPersistAndReturnDto()
    {
        var userId = Guid.CreateVersion7();
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(userId);

        var handler = new CreateAnnouncementCommandHandler(_dbContext, _currentUser);
        var command = new CreateAnnouncementCommand
        {
            CourseId = Guid.CreateVersion7(),
            Title = "Exam Schedule",
            Content = "Exam is tomorrow at 9AM",
            IsPinned = true
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("Exam Schedule", result.Data.Title);
        Assert.True(result.Data.IsPinned);

        var persisted = await _dbContext.Announcements.FindAsync(result.Data.Id);
        Assert.NotNull(persisted);
        Assert.Equal("Exam Schedule", persisted.Title);
    }

    [Fact]
    public async Task CreateDiscussionThread_ShouldPersistAndReturnDto()
    {
        var userId = Guid.CreateVersion7();
        var courseId = Guid.CreateVersion7();
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(userId);

        var handler = new CreateDiscussionThreadCommandHandler(_dbContext, _currentUser);
        var command = new CreateDiscussionThreadCommand
        {
            CourseId = courseId,
            Title = "Module 1 Discussion",
            Content = "Any questions on topic 1?"
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("Module 1 Discussion", result.Data.Title);
        Assert.Equal(0, result.Data.CommentsCount);

        var persisted = await _dbContext.DiscussionThreads.FindAsync(result.Data.Id);
        Assert.NotNull(persisted);
        Assert.Equal(userId, persisted.AuthorId);
    }

    [Fact]
    public async Task PostThreadComment_TopLevelAndNested_ShouldPersistCorrectly()
    {
        var authorId = Guid.CreateVersion7();
        var commenterId = Guid.CreateVersion7();
        var thread = DiscussionThread.Create(Guid.CreateVersion7(), null, authorId, "Thread Title", "Thread Content");
        await _dbContext.DiscussionThreads.AddAsync(thread);
        await _dbContext.SaveChangesAsync();

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(commenterId);

        var handler = new PostThreadCommentCommandHandler(_dbContext, _currentUser);

        // 1. Post top-level comment
        var topCommentResult = await handler.Handle(new PostThreadCommentCommand
        {
            ThreadId = thread.Id,
            Content = "First comment"
        }, CancellationToken.None);

        Assert.True(topCommentResult.Success);
        Assert.NotNull(topCommentResult.Data);
        Assert.Null(topCommentResult.Data.ParentCommentId);

        // 2. Post nested reply
        var replyResult = await handler.Handle(new PostThreadCommentCommand
        {
            ThreadId = thread.Id,
            ParentCommentId = topCommentResult.Data.Id,
            Content = "Reply to first comment"
        }, CancellationToken.None);

        Assert.True(replyResult.Success);
        Assert.NotNull(replyResult.Data);
        Assert.Equal(topCommentResult.Data.Id, replyResult.Data.ParentCommentId);

        var comments = await _dbContext.ThreadComments.Where(c => c.ThreadId == thread.Id).ToListAsync();
        Assert.Equal(2, comments.Count);
    }

    [Fact]
    public async Task PostThreadComment_OnClosedThread_ShouldThrowBusinessRuleException()
    {
        var authorId = Guid.CreateVersion7();
        var thread = DiscussionThread.Create(Guid.CreateVersion7(), null, authorId, "Closed Thread", "Thread Content");
        thread.Close(authorId);
        await _dbContext.DiscussionThreads.AddAsync(thread);
        await _dbContext.SaveChangesAsync();

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(authorId);

        var handler = new PostThreadCommentCommandHandler(_dbContext, _currentUser);

        await Assert.ThrowsAsync<BusinessRuleException>(async () =>
        {
            await handler.Handle(new PostThreadCommentCommand
            {
                ThreadId = thread.Id,
                Content = "Attempt to reply"
            }, CancellationToken.None);
        });
    }

    [Fact]
    public async Task CloseDiscussionThread_ByAuthor_ShouldCloseSuccessfully()
    {
        var authorId = Guid.CreateVersion7();
        var thread = DiscussionThread.Create(Guid.CreateVersion7(), null, authorId, "Open Thread", "Thread Content");
        await _dbContext.DiscussionThreads.AddAsync(thread);
        await _dbContext.SaveChangesAsync();

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(authorId);
        _currentUser.Roles.Returns(["Student"]);

        var handler = new CloseDiscussionThreadCommandHandler(_dbContext, _currentUser);
        var result = await handler.Handle(new CloseDiscussionThreadCommand(thread.Id), CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.IsClosed);
        Assert.Equal(authorId, result.Data.ClosedByUserId);
    }
}
