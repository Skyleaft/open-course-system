using MonoSlice.Modules.Communications.Domain;
using MonoSlice.Shared.Abstractions.Exceptions;
using Xunit;

namespace MonoSlice.Modules.Communications.Tests;

public class DiscussionThreadTests
{
    [Fact]
    public void Create_ValidThread_ShouldInitializeCorrectly()
    {
        var courseId = Guid.CreateVersion7();
        var lessonId = Guid.CreateVersion7();
        var authorId = Guid.CreateVersion7();

        var thread = DiscussionThread.Create(courseId, lessonId, authorId, "Question on EF Core", "How does tracking work?");

        Assert.NotNull(thread);
        Assert.Equal(courseId, thread.CourseId);
        Assert.Equal(lessonId, thread.LessonId);
        Assert.Equal(authorId, thread.AuthorId);
        Assert.Equal("Question on EF Core", thread.Title);
        Assert.Equal("How does tracking work?", thread.Content);
        Assert.False(thread.IsClosed);
        Assert.Empty(thread.Comments);
    }

    [Fact]
    public void AddComment_OpenThread_ShouldAddCommentSuccessfully()
    {
        var thread = DiscussionThread.Create(Guid.CreateVersion7(), null, Guid.CreateVersion7(), "Title", "Content");
        var commenterId = Guid.CreateVersion7();

        var comment = thread.AddComment(commenterId, "Here is an answer");

        Assert.NotNull(comment);
        Assert.Equal(thread.Id, comment.ThreadId);
        Assert.Equal(commenterId, comment.AuthorId);
        Assert.Equal("Here is an answer", comment.Content);
        Assert.Null(comment.ParentCommentId);
        Assert.Single(thread.Comments);
    }

    [Fact]
    public void AddComment_ClosedThread_ShouldThrowBusinessRuleException()
    {
        var thread = DiscussionThread.Create(Guid.CreateVersion7(), null, Guid.CreateVersion7(), "Title", "Content");
        thread.Close(Guid.CreateVersion7());

        Assert.True(thread.IsClosed);
        Assert.Throws<BusinessRuleException>(() =>
            thread.AddComment(Guid.CreateVersion7(), "Trying to reply to closed thread"));
    }

    [Fact]
    public void Close_And_Reopen_ShouldManageState()
    {
        var thread = DiscussionThread.Create(Guid.CreateVersion7(), null, Guid.CreateVersion7(), "Title", "Content");
        var adminId = Guid.CreateVersion7();

        thread.Close(adminId);
        Assert.True(thread.IsClosed);
        Assert.Equal(adminId, thread.ClosedByUserId);
        Assert.NotNull(thread.ClosedAtUtc);

        thread.Reopen();
        Assert.False(thread.IsClosed);
        Assert.Null(thread.ClosedByUserId);
        Assert.Null(thread.ClosedAtUtc);
    }
}
