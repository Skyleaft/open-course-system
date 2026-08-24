using MonoSlice.Modules.Communications.Domain;
using Xunit;

namespace MonoSlice.Modules.Communications.Tests;

public class AnnouncementTests
{
    [Fact]
    public void Create_ValidGlobalAnnouncement_ShouldInitializeCorrectly()
    {
        var authorId = Guid.CreateVersion7();
        var announcement = Announcement.Create(null, authorId, "Platform Maintenance", "Platform will be down at midnight", true);

        Assert.NotNull(announcement);
        Assert.Null(announcement.CourseId);
        Assert.Equal(authorId, announcement.AuthorId);
        Assert.Equal("Platform Maintenance", announcement.Title);
        Assert.Equal("Platform will be down at midnight", announcement.Content);
        Assert.True(announcement.IsPinned);
    }

    [Fact]
    public void Create_CourseAnnouncement_ShouldSetCourseId()
    {
        var courseId = Guid.CreateVersion7();
        var authorId = Guid.CreateVersion7();
        var announcement = Announcement.Create(courseId, authorId, "Welcome to C# 14", "Course starts tomorrow", false);

        Assert.Equal(courseId, announcement.CourseId);
        Assert.False(announcement.IsPinned);
    }

    [Fact]
    public void Pin_And_Unpin_ShouldToggleStatus()
    {
        var announcement = Announcement.Create(null, Guid.CreateVersion7(), "Title", "Content", false);
        Assert.False(announcement.IsPinned);

        announcement.Pin();
        Assert.True(announcement.IsPinned);

        announcement.Unpin();
        Assert.False(announcement.IsPinned);
    }

    [Fact]
    public void Update_ShouldModifyProperties()
    {
        var announcement = Announcement.Create(null, Guid.CreateVersion7(), "Old Title", "Old Content", false);

        announcement.Update("New Title", "New Content", true);

        Assert.Equal("New Title", announcement.Title);
        Assert.Equal("New Content", announcement.Content);
        Assert.True(announcement.IsPinned);
        Assert.NotNull(announcement.UpdatedAtUtc);
    }
}
