using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Catalog.Contracts;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Modules.Catalog.Persistence;
using Xunit;

namespace MonoSlice.Modules.Catalog.Tests;

public class CoursesModuleApiTests
{
    [Fact]
    public async Task GetCourseByIdAsync_ShouldReturnCourseContractDto()
    {
        var options = new DbContextOptionsBuilder<CoursesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.CreateVersion7().ToString())
            .Options;

        var dbContext = new CoursesDbContext(options);
        var instructorId = Guid.CreateVersion7();
        var course = Course.Create(instructorId, "Microservices .NET", "Comprehensive guide", CourseAccessType.OpenPaid, 199000m);
        course.Publish();
        await dbContext.Courses.AddAsync(course);
        await dbContext.SaveChangesAsync();

        var api = new CoursesModuleApi(dbContext);
        var contract = await api.GetCourseByIdAsync(course.Id);

        Assert.NotNull(contract);
        Assert.Equal(course.Id, contract.Id);
        Assert.Equal("Microservices .NET", contract.Title);
        Assert.Equal("OpenPaid", contract.AccessType);
        Assert.Equal(199000m, contract.Price);
        Assert.True(contract.IsPublished);
    }

    [Fact]
    public async Task EnrollStudentAsync_ShouldEnrollAndReturnTrue()
    {
        var options = new DbContextOptionsBuilder<CoursesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.CreateVersion7().ToString())
            .Options;

        var dbContext = new CoursesDbContext(options);
        var course = Course.Create(Guid.CreateVersion7(), "Docker & K8s", "Desc", CourseAccessType.OpenFree);
        await dbContext.Courses.AddAsync(course);
        await dbContext.SaveChangesAsync();

        var studentId = Guid.CreateVersion7();
        var api = new CoursesModuleApi(dbContext);

        var enrolled = await api.EnrollStudentAsync(studentId, course.Id);
        var isEnrolled = await api.IsStudentEnrolledAsync(studentId, course.Id);

        Assert.True(enrolled);
        Assert.True(isEnrolled);
    }
}
