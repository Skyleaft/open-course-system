using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MonoSlice.Modules.Catalog.Domain;
using MonoSlice.Modules.Catalog.Features.CreateCourse;
using MonoSlice.Modules.Catalog.Persistence;
using MonoSlice.Shared.Abstractions.Common;
using MonoSlice.Shared.Abstractions.Contracts;
using MonoSlice.Shared.Abstractions.CQRS;
using MonoSlice.Shared.Abstractions.Exceptions;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Catalog.Features.EnrollCourse;

public sealed class EnrollCourseCommandHandler : ICommandHandler<EnrollCourseCommand, ApiResponse<EnrollmentResultDto>>
{
    private readonly CoursesDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IServiceProvider _serviceProvider;

    public EnrollCourseCommandHandler(
        CoursesDbContext dbContext,
        ICurrentUser currentUser,
        IServiceProvider serviceProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _serviceProvider = serviceProvider;
    }

    public async ValueTask<ApiResponse<EnrollmentResultDto>> Handle(
        EnrollCourseCommand command,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authentication required to enroll in a course.");
        }

        var userId = _currentUser.UserId.Value;

        var course = await _dbContext.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == command.CourseId, cancellationToken);

        if (course is null)
        {
            throw new NotFoundException(nameof(Course), command.CourseId);
        }

        // Check if already enrolled
        var existing = await _dbContext.Enrollments
            .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == command.CourseId, cancellationToken);

        if (existing is not null)
        {
            return ApiResponse.Ok(
                new EnrollmentResultDto(existing.Id, existing.UserId, existing.CourseId, existing.EnrolledAtUtc),
                "User is already enrolled in this course.");
        }

        // AccessType verification
        switch (course.AccessType)
        {
            case CourseAccessType.OpenFree:
                // No validation needed
                break;

            case CourseAccessType.PrivateWithKey:
                if (string.IsNullOrWhiteSpace(command.EnrollmentKey))
                {
                    throw new BusinessRuleException("Enrollment key is required for this private course.");
                }

                var keyHash = CreateCourseCommandHandler.HashKey(command.EnrollmentKey);
                if (!string.Equals(keyHash, course.EnrollmentKeyHash, StringComparison.OrdinalIgnoreCase))
                {
                    throw new BusinessRuleException("Invalid enrollment key provided.");
                }
                break;

            case CourseAccessType.OpenPaid:
                var paymentsApi = _serviceProvider.GetService<IPaymentsModuleApi>();
                if (paymentsApi is not null)
                {
                    var isPaid = await paymentsApi.HasUserPurchasedCourseAsync(userId, command.CourseId, cancellationToken);
                    if (!isPaid)
                    {
                        throw new BusinessRuleException("Payment required before enrolling in this paid course.");
                    }
                }
                break;
        }

        var enrollment = CourseEnrollment.Create(userId, command.CourseId);
        await _dbContext.Enrollments.AddAsync(enrollment, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var result = new EnrollmentResultDto(
            enrollment.Id,
            enrollment.UserId,
            enrollment.CourseId,
            enrollment.EnrolledAtUtc);

        return ApiResponse.Ok(result, "Successfully enrolled in course.");
    }
}
