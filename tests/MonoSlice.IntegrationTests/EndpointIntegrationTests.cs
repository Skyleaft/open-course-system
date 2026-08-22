using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MonoSlice.Shared.Abstractions.Common;
using Xunit;

namespace MonoSlice.IntegrationTests;

public class MonoSliceApplicationFactory : WebApplicationFactory<Program>
{
    static MonoSliceApplicationFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__UsersDb", "InMemory:IntegrationTestUsersDb");
        Environment.SetEnvironmentVariable("ConnectionStrings__CatalogDb", "InMemory:IntegrationTestCatalogDb");
        Environment.SetEnvironmentVariable("ConnectionStrings__CoursesDb", "InMemory:IntegrationTestCoursesDb");
        Environment.SetEnvironmentVariable("ConnectionStrings__OrdersDb", "InMemory:IntegrationTestOrdersDb");
        Environment.SetEnvironmentVariable("ConnectionStrings__PaymentsDb", "InMemory:IntegrationTestPaymentsDb");
        Environment.SetEnvironmentVariable("ConnectionStrings__ExamsDb", "InMemory:IntegrationTestExamsDb");
        Environment.SetEnvironmentVariable("Cache__Provider", "Memory");
        Environment.SetEnvironmentVariable("Messaging__Provider", "RabbitMQ");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }
}

public class EndpointIntegrationTests : IClassFixture<MonoSliceApplicationFactory>
{
    private readonly HttpClient _client;

    public EndpointIntegrationTests(MonoSliceApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AuthRegisterAndLogin_ShouldSucceed()
    {
        var registerPayload = new
        {
            Email = "student1@example.com",
            UserName = "student1",
            Password = "Password123!",
            FullName = "Student One"
        };

        var regResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerPayload);
        var regRaw = await regResponse.Content.ReadAsStringAsync();
        Assert.True(regResponse.IsSuccessStatusCode, $"Register failed: {regRaw}");

        var loginPayload = new
        {
            UserNameOrEmail = "student1@example.com",
            Password = "Password123!"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginPayload);
        var loginRaw = await loginResponse.Content.ReadAsStringAsync();
        Assert.True(loginResponse.IsSuccessStatusCode, $"Login failed: {loginRaw}");
    }

    [Fact]
    public async Task PaymentsCheckoutAndWebhook_ShouldSucceed()
    {
        // 1. Register and Login to get access token
        var registerPayload = new
        {
            Email = "student_pay@example.com",
            UserName = "student_pay",
            Password = "Password123!",
            FullName = "Paying Student"
        };
        await _client.PostAsJsonAsync("/api/v1/auth/register", registerPayload);

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            UserNameOrEmail = "student_pay@example.com",
            Password = "Password123!"
        });
        var loginData = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<MonoSlice.Modules.Users.Features.Login.LoginResponseDto>>();
        Assert.NotNull(loginData?.Data?.AccessToken);

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginData.Data.AccessToken);

        // 2. Checkout
        var courseId = Guid.CreateVersion7();
        var checkoutPayload = new
        {
            CourseId = courseId,
            Amount = 150000m,
            Currency = "IDR"
        };

        var checkoutResponse = await _client.PostAsJsonAsync("/api/v1/payments/checkout", checkoutPayload);
        var checkoutRaw = await checkoutResponse.Content.ReadAsStringAsync();
        Assert.True(checkoutResponse.IsSuccessStatusCode, $"Checkout failed: {checkoutRaw}");

        var checkoutData = await checkoutResponse.Content.ReadFromJsonAsync<ApiResponse<MonoSlice.Modules.Orders.Features.CreateCheckout.CheckoutResponseDto>>();
        Assert.NotNull(checkoutData?.Data);
        var orderId = checkoutData.Data.OrderId;

        // 3. Query Order
        var getOrderResponse = await _client.GetAsync($"/api/v1/payments/orders/{orderId}");
        Assert.True(getOrderResponse.IsSuccessStatusCode);

        // 4. Webhook
        var webhookPayload = new
        {
            OrderId = orderId,
            ExternalPaymentReference = "GATEWAY-REF-1001",
            PaymentStatus = "PAID"
        };
        var webhookResponse = await _client.PostAsJsonAsync("/api/v1/payments/webhook", webhookPayload);
        var webhookRaw = await webhookResponse.Content.ReadAsStringAsync();
        Assert.True(webhookResponse.IsSuccessStatusCode, $"Webhook failed: {webhookRaw}");

        // 5. Query Order again to verify Paid status
        var verifyOrderResponse = await _client.GetAsync($"/api/v1/payments/orders/{orderId}");
        var orderData = await verifyOrderResponse.Content.ReadFromJsonAsync<ApiResponse<MonoSlice.Modules.Orders.Features.GetOrder.OrderResponseDto>>();
        Assert.NotNull(orderData?.Data);
        Assert.Equal("Paid", orderData.Data.Status);
    }

    [Fact]
    public async Task CoursesFullLifecycle_ShouldSucceed()
    {
        // 1. Register and Login as Instructor
        var instructorPayload = new
        {
            Email = "instructor_course@example.com",
            UserName = "instructor_course",
            Password = "Password123!",
            FullName = "Course Instructor"
        };
        await _client.PostAsJsonAsync("/api/v1/auth/register", instructorPayload);

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            UserNameOrEmail = "instructor_course@example.com",
            Password = "Password123!"
        });
        var loginData = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<MonoSlice.Modules.Users.Features.Login.LoginResponseDto>>();
        Assert.NotNull(loginData?.Data?.AccessToken);

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginData.Data.AccessToken);

        // 2. Create Course
        var createCoursePayload = new
        {
            Title = "Complete .NET 10 & SvelteKit Masterclass",
            Description = "Comprehensive full stack bootcamp",
            AccessType = "OpenFree",
            Price = 0m
        };

        var createCourseResponse = await _client.PostAsJsonAsync("/api/v1/courses", createCoursePayload);
        var createCourseRaw = await createCourseResponse.Content.ReadAsStringAsync();
        Assert.True(createCourseResponse.IsSuccessStatusCode, $"Create course failed: {createCourseRaw}");

        var courseData = await createCourseResponse.Content.ReadFromJsonAsync<ApiResponse<MonoSlice.Modules.Catalog.Features.CreateCourse.CourseDetailDto>>();
        Assert.NotNull(courseData?.Data);
        var courseId = courseData.Data.Id;

        // 3. Publish Course
        var publishResponse = await _client.PostAsync($"/api/v1/courses/{courseId}/publish", null);
        Assert.True(publishResponse.IsSuccessStatusCode);

        // 4. Add Section
        var sectionResponse = await _client.PostAsJsonAsync($"/api/v1/courses/{courseId}/sections", new { Title = "Module 1: Introduction" });
        var sectionData = await sectionResponse.Content.ReadFromJsonAsync<ApiResponse<MonoSlice.Modules.Catalog.Features.AddSection.SectionResultDto>>();
        Assert.NotNull(sectionData?.Data);
        var sectionId = sectionData.Data.Id;

        // 5. Add Lesson
        var lessonResponse = await _client.PostAsJsonAsync($"/api/v1/courses/sections/{sectionId}/lessons", new
        {
            Title = "Welcome to the Course",
            Type = "Video",
            ContentUrl = "s3://courses/videos/lesson1.mp4",
            DurationMinutes = 12
        });
        Assert.True(lessonResponse.IsSuccessStatusCode);

        // 6. Create Assignment
        var assignmentResponse = await _client.PostAsJsonAsync($"/api/v1/courses/{courseId}/assignments", new
        {
            Title = "Assignment 1: Setup Repository",
            Instruction = "Push clean architecture template to Github",
            DeadlineUtc = DateTime.UtcNow.AddDays(7),
            MaxScore = 100m
        });
        var assignmentData = await assignmentResponse.Content.ReadFromJsonAsync<ApiResponse<MonoSlice.Modules.Catalog.Features.CreateAssignment.AssignmentResultDto>>();
        Assert.NotNull(assignmentData?.Data);
        var assignmentId = assignmentData.Data.Id;

        // 7. Query Course Syllabus
        var getCourseResponse = await _client.GetAsync($"/api/v1/courses/{courseId}");
        var courseCurriculum = await getCourseResponse.Content.ReadFromJsonAsync<ApiResponse<MonoSlice.Modules.Catalog.Features.GetCourse.CourseCurriculumDto>>();
        Assert.NotNull(courseCurriculum?.Data);
        Assert.Single(courseCurriculum.Data.Sections);
        Assert.Single(courseCurriculum.Data.Sections[0].Lessons);
        Assert.Single(courseCurriculum.Data.Assignments);

        // 8. Enroll in course
        var enrollResponse = await _client.PostAsync($"/api/v1/courses/{courseId}/enroll", null);
        Assert.True(enrollResponse.IsSuccessStatusCode);

        // 9. Submit Assignment
        var submitResponse = await _client.PostAsJsonAsync($"/api/v1/courses/assignments/{assignmentId}/submit", new
        {
            FileUrl = "s3://submissions/student_repo.zip"
        });
        Assert.True(submitResponse.IsSuccessStatusCode);
    }

    [Fact]
    public async Task ExamsFullLifecycle_ShouldSucceed()
    {
        // 1. Register & Login Instructor
        var instructorPayload = new
        {
            Email = "instructor_exam@example.com",
            UserName = "instructor_exam",
            Password = "Password123!",
            FullName = "Exam Instructor"
        };
        await _client.PostAsJsonAsync("/api/v1/auth/register", instructorPayload);

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            UserNameOrEmail = "instructor_exam@example.com",
            Password = "Password123!"
        });
        var loginData = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<MonoSlice.Modules.Users.Features.Login.LoginResponseDto>>();
        Assert.NotNull(loginData?.Data?.AccessToken);

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginData.Data.AccessToken);

        // 2. Create Exam
        var createExamPayload = new
        {
            Title = "Backend Architecture Certification",
            Description = "Comprehensive .NET backend certification exam",
            Mode = "RealExam",
            DurationMinutes = 60,
            PassingScore = 50m,
            MaxAllowedViolations = 3,
            ShuffleQuestions = true,
            ShuffleOptions = true
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v1/exams", createExamPayload);
        var createRaw = await createResponse.Content.ReadAsStringAsync();
        Assert.True(createResponse.IsSuccessStatusCode, $"Create exam failed: {createRaw}");

        var examData = await createResponse.Content.ReadFromJsonAsync<ApiResponse<MonoSlice.Modules.Exams.Features.CreateExam.ExamDetailDto>>();
        Assert.NotNull(examData?.Data);
        var examId = examData.Data.Id;

        // 3. Add Questions
        var opt1Correct = Guid.CreateVersion7();
        var opt1Wrong = Guid.CreateVersion7();
        var addQ1Response = await _client.PostAsJsonAsync($"/api/v1/exams/{examId}/questions", new
        {
            QuestionText = "Which principle states that high-level modules should not depend on low-level modules?",
            Type = "SingleChoice",
            Points = 10m,
            Explanation = "Dependency Inversion Principle (DIP)",
            Options = new[]
            {
                new { Id = (Guid?)opt1Correct, Text = "Dependency Inversion Principle", IsCorrect = true },
                new { Id = (Guid?)opt1Wrong, Text = "Single Responsibility Principle", IsCorrect = false }
            }
        });
        Assert.True(addQ1Response.IsSuccessStatusCode);

        var q1Data = await addQ1Response.Content.ReadFromJsonAsync<ApiResponse<MonoSlice.Modules.Exams.Features.AddQuestion.QuestionResultDto>>();
        Assert.NotNull(q1Data?.Data);
        var q1Id = q1Data.Data.Id;

        // 4. Publish Exam
        var publishResponse = await _client.PostAsync($"/api/v1/exams/{examId}/publish", null);
        Assert.True(publishResponse.IsSuccessStatusCode);

        // 5. Start Exam Attempt
        var startResponse = await _client.PostAsync($"/api/v1/exams/{examId}/start", null);
        var startRaw = await startResponse.Content.ReadAsStringAsync();
        Assert.True(startResponse.IsSuccessStatusCode, $"Start exam failed: {startRaw}");

        var attemptData = await startResponse.Content.ReadFromJsonAsync<ApiResponse<MonoSlice.Modules.Exams.Features.StartExam.ExamAttemptDto>>();
        Assert.NotNull(attemptData?.Data);
        var submissionId = attemptData.Data.SubmissionId;

        // 6. Get Exam Questions (Randomized)
        var getQuestionsResponse = await _client.GetAsync($"/api/v1/exams/submissions/{submissionId}/questions");
        Assert.True(getQuestionsResponse.IsSuccessStatusCode);

        // 7. Save Answer
        var saveAnswerResponse = await _client.PostAsJsonAsync($"/api/v1/exams/submissions/{submissionId}/answers", new
        {
            QuestionId = q1Id,
            SelectedOptionIds = new[] { opt1Correct }
        });
        Assert.True(saveAnswerResponse.IsSuccessStatusCode);

        // 8. Presign Proctor Snapshot
        var presignResponse = await _client.PostAsync($"/api/v1/exams/submissions/{submissionId}/snapshots/presign", null);
        Assert.True(presignResponse.IsSuccessStatusCode);

        // 9. Finish and Submit Exam
        var finishResponse = await _client.PostAsync($"/api/v1/exams/submissions/{submissionId}/finish", null);
        var finishRaw = await finishResponse.Content.ReadAsStringAsync();
        Assert.True(finishResponse.IsSuccessStatusCode, $"Finish exam failed: {finishRaw}");

        var finishData = await finishResponse.Content.ReadFromJsonAsync<ApiResponse<MonoSlice.Modules.Exams.Features.SubmitExam.ExamFinalResultDto>>();
        Assert.NotNull(finishData?.Data);
        Assert.Equal("Completed", finishData.Data.Status);
        Assert.Equal(100m, finishData.Data.Score);
        Assert.True(finishData.Data.IsPassed);

        // 10. View Result
        var resultResponse = await _client.GetAsync($"/api/v1/exams/submissions/{submissionId}/result");
        Assert.True(resultResponse.IsSuccessStatusCode);
    }
}
