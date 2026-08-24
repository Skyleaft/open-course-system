using System.Security.Cryptography;
using System.Text;
using Mapster;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Catalog.Features.CreateCourse;

public sealed class CreateCourseCommandHandler : ICommandHandler<CreateCourseCommand, ApiResponse<CourseDetailDto>>
{
    private readonly CoursesDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreateCourseCommandHandler(
        CoursesDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ApiResponse<CourseDetailDto>> Handle(
        CreateCourseCommand command,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authentication is required to create a course.");
        }

        string? keyHash = null;
        if (command.AccessType == CourseAccessType.PrivateWithKey && !string.IsNullOrWhiteSpace(command.EnrollmentKey))
        {
            keyHash = HashKey(command.EnrollmentKey);
        }

        var course = Course.Create(
            _currentUser.UserId.Value,
            command.Title,
            command.Description,
            command.AccessType,
            command.Price,
            keyHash,
            command.ThumbnailUrl);

        await _dbContext.Courses.AddAsync(course, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = course.Adapt<CourseDetailDto>() with
        {
            AccessType = course.AccessType.ToString()
        };

        return ApiResponse.Ok(dto, "Course created successfully.");
    }

    public static string HashKey(string key)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(key.Trim()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
