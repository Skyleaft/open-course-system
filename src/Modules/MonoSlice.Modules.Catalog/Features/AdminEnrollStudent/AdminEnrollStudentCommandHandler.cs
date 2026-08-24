using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.Contracts;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Catalog.Features.AdminEnrollStudent;

public sealed class AdminEnrollStudentCommandHandler
    : ICommandHandler<AdminEnrollStudentCommand, ApiResponse<AdminEnrollStudentResultDto>>
{
    private readonly CoursesDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IIdentityModuleApi _identityModuleApi;

    public AdminEnrollStudentCommandHandler(
        CoursesDbContext dbContext,
        ICurrentUser currentUser,
        IIdentityModuleApi identityModuleApi)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _identityModuleApi = identityModuleApi;
    }

    public async ValueTask<ApiResponse<AdminEnrollStudentResultDto>> Handle(
        AdminEnrollStudentCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var course = await _dbContext.Courses
            .FirstOrDefaultAsync(c => c.Id == request.CourseId, cancellationToken);

        if (course is null)
        {
            return ApiResponse.Fail<AdminEnrollStudentResultDto>("Course not found.", 404);
        }

        var isAdmin = _currentUser.Roles.Contains("Admin");
        var isInstructor = _currentUser.Roles.Contains("Instructor");

        if (!isAdmin && (!isInstructor || course.InstructorId != _currentUser.UserId.Value))
        {
            return ApiResponse.Fail<AdminEnrollStudentResultDto>(
                "You are not authorized to enroll students in this course.", 403);
        }

        UserContractDto? targetUser = null;

        if (request.UserId.HasValue && request.UserId.Value != Guid.Empty)
        {
            targetUser = await _identityModuleApi.GetUserByIdAsync(request.UserId.Value, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(request.Email))
        {
            targetUser = await _identityModuleApi.GetUserByEmailAsync(request.Email.Trim(), cancellationToken);
        }

        if (targetUser is null)
        {
            return ApiResponse.Fail<AdminEnrollStudentResultDto>(
                "Student user was not found with the provided identifier or email address.", 404);
        }

        var isAlreadyEnrolled = await _dbContext.Enrollments
            .AnyAsync(e => e.CourseId == request.CourseId && e.UserId == targetUser.Id, cancellationToken);

        if (isAlreadyEnrolled)
        {
            return ApiResponse.Fail<AdminEnrollStudentResultDto>(
                $"Student '{targetUser.FullName}' ({targetUser.Email}) is already enrolled in this course.", 409);
        }

        var enrollment = CourseEnrollment.Create(targetUser.Id, request.CourseId);
        _dbContext.Enrollments.Add(enrollment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var result = new AdminEnrollStudentResultDto(
            enrollment.Id,
            enrollment.CourseId,
            enrollment.UserId,
            targetUser.FullName,
            targetUser.Email,
            enrollment.EnrolledAtUtc);

        return ApiResponse.Ok(result, "Student enrolled successfully.");
    }
}
