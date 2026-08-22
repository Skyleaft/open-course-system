using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Shared.Abstractions.Exceptions;
using Xunit;

namespace MonoSlice.Modules.Catalog.Tests;

public class CourseDomainTests
{
    [Fact]
    public void Create_ShouldInitializeWithGuidV7AndCorrectValues()
    {
        var instructorId = Guid.CreateVersion7();
        var course = Course.Create(
            instructorId,
            "Clean Architecture in .NET",
            "Deep dive into modular monolith and domain-driven design",
            CourseAccessType.OpenFree);

        Assert.NotEqual(Guid.Empty, course.Id);
        Assert.Equal(instructorId, course.InstructorId);
        Assert.Equal("Clean Architecture in .NET", course.Title);
        Assert.Equal(CourseAccessType.OpenFree, course.AccessType);
        Assert.Equal(0m, course.Price);
        Assert.False(course.IsPublished);
        Assert.Empty(course.Sections);
        Assert.Empty(course.Assignments);
    }

    [Fact]
    public void Create_ShouldThrowException_WhenOpenPaidAndPriceIsZeroOrNegative()
    {
        var instructorId = Guid.CreateVersion7();

        Assert.Throws<BusinessRuleException>(() =>
            Course.Create(instructorId, "Paid Course", "Desc", CourseAccessType.OpenPaid, 0m));

        Assert.Throws<BusinessRuleException>(() =>
            Course.Create(instructorId, "Paid Course", "Desc", CourseAccessType.OpenPaid, -50000m));
    }

    [Fact]
    public void Create_ShouldThrowException_WhenPrivateWithKeyAndNoKeyHash()
    {
        var instructorId = Guid.CreateVersion7();

        Assert.Throws<BusinessRuleException>(() =>
            Course.Create(instructorId, "Private Course", "Desc", CourseAccessType.PrivateWithKey, 0m, null));
    }

    [Fact]
    public void PublishAndUnpublish_ShouldTogglePublishedFlag()
    {
        var course = Course.Create(Guid.CreateVersion7(), "Course 1", "Desc", CourseAccessType.OpenFree);

        course.Publish();
        Assert.True(course.IsPublished);

        course.Unpublish();
        Assert.False(course.IsPublished);
    }

    [Fact]
    public void AddSectionAndLesson_ShouldMaintainCurriculumHierarchy()
    {
        var course = Course.Create(Guid.CreateVersion7(), "Course 1", "Desc", CourseAccessType.OpenFree);

        var section1 = course.AddSection("Module 1: Introduction");
        var lesson1 = section1.AddLesson("Welcome Video", LessonType.Video, "s3://courses/videos/intro.mp4", 15);
        var lesson2 = section1.AddLesson("Course Syllabus PDF", LessonType.PdfDocument, "s3://courses/docs/syllabus.pdf", 5);

        Assert.Single(course.Sections);
        Assert.Equal(1, section1.OrderIndex);
        Assert.Equal(2, section1.Lessons.Count);
        Assert.Equal(1, lesson1.OrderIndex);
        Assert.Equal(2, lesson2.OrderIndex);
        Assert.Equal(LessonType.Video, lesson1.Type);
        Assert.Equal(LessonType.PdfDocument, lesson2.Type);
    }

    [Fact]
    public void AddAssignment_ShouldAddAssignmentCorrectly()
    {
        var course = Course.Create(Guid.CreateVersion7(), "Course 1", "Desc", CourseAccessType.OpenFree);
        var deadline = DateTime.UtcNow.AddDays(7);

        var assignment = course.AddAssignment("Final Project", "Build an API", deadline, 100m);

        Assert.Single(course.Assignments);
        Assert.Equal("Final Project", assignment.Title);
        Assert.Equal(deadline, assignment.DeadlineUtc);
        Assert.Equal(100m, assignment.MaxScore);
    }

    [Fact]
    public void AssignmentSubmission_ShouldThrowException_WhenSubmittedAfterDeadline()
    {
        var assignmentId = Guid.CreateVersion7();
        var studentId = Guid.CreateVersion7();
        var pastDeadline = DateTime.UtcNow.AddHours(-1);

        Assert.Throws<BusinessRuleException>(() =>
            AssignmentSubmission.Create(assignmentId, studentId, "s3://submissions/file.zip", pastDeadline));
    }

    [Fact]
    public void CourseEnrollment_ShouldCreateWithGuidV7()
    {
        var userId = Guid.CreateVersion7();
        var courseId = Guid.CreateVersion7();

        var enrollment = CourseEnrollment.Create(userId, courseId);

        Assert.NotEqual(Guid.Empty, enrollment.Id);
        Assert.Equal(userId, enrollment.UserId);
        Assert.Equal(courseId, enrollment.CourseId);
    }
}
