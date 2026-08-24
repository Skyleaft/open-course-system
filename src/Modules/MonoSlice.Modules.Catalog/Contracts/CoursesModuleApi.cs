using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Contracts;

namespace MonoSlice.Modules.Catalog.Contracts;

public sealed class CoursesModuleApi : ICoursesModuleApi
{
    private readonly CoursesDbContext _dbContext;

    public CoursesModuleApi(CoursesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CourseContractDto?> GetCourseByIdAsync(Guid courseId, CancellationToken ct = default)
    {
        var course = await _dbContext.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId, ct);

        if (course is null)
        {
            return null;
        }

        return new CourseContractDto(
            course.Id,
            course.Title,
            course.Description,
            course.AccessType.ToString(),
            course.Price,
            course.IsPublished);
    }

    public async Task<bool> IsStudentEnrolledAsync(Guid userId, Guid courseId, CancellationToken ct = default)
    {
        return await _dbContext.Enrollments
            .AsNoTracking()
            .AnyAsync(e => e.UserId == userId && e.CourseId == courseId, ct);
    }

    public async Task<bool> EnrollStudentAsync(Guid userId, Guid courseId, CancellationToken ct = default)
    {
        var courseExists = await _dbContext.Courses
            .AsNoTracking()
            .AnyAsync(c => c.Id == courseId, ct);

        if (!courseExists)
        {
            return false;
        }

        var existing = await _dbContext.Enrollments
            .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId, ct);

        if (existing is not null)
        {
            return true;
        }

        var enrollment = CourseEnrollment.Create(userId, courseId);
        await _dbContext.Enrollments.AddAsync(enrollment, ct);
        await _dbContext.SaveChangesAsync(ct);

        return true;
    }
}
