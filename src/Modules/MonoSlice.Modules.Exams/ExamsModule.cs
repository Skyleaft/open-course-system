using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MonoSlice.Modules.Exams.Contracts;
using MonoSlice.Modules.Exams.Features.AddQuestion;
using MonoSlice.Modules.Exams.Features.CreateExam;
using MonoSlice.Modules.Exams.Features.DeleteExam;
using MonoSlice.Modules.Exams.Features.DeleteQuestion;
using MonoSlice.Modules.Exams.Features.GetExam;
using MonoSlice.Modules.Exams.Features.GetExamQuestions;
using MonoSlice.Modules.Exams.Features.GetExamResult;
using MonoSlice.Modules.Exams.Features.GetQuestion;
using MonoSlice.Modules.Exams.Features.ListExams;
using MonoSlice.Modules.Exams.Features.PresignSnapshot;
using MonoSlice.Modules.Exams.Features.Proctor.ForceDisconnectCandidate;
using MonoSlice.Modules.Exams.Features.Proctor.GetLiveCandidates;
using MonoSlice.Modules.Exams.Features.Proctor.WarnCandidate;
using MonoSlice.Modules.Exams.Features.PublishExam;
using MonoSlice.Modules.Exams.Features.SaveAnswer;
using MonoSlice.Modules.Exams.Features.StartExam;
using MonoSlice.Modules.Exams.Features.SubmitExam;
using MonoSlice.Modules.Exams.Features.UpdateExam;
using MonoSlice.Modules.Exams.Features.CreateQuestionBank;
using MonoSlice.Modules.Exams.Features.DeleteQuestionBank;
using MonoSlice.Modules.Exams.Features.GetQuestionBank;
using MonoSlice.Modules.Exams.Features.ListQuestionBanks;
using MonoSlice.Modules.Exams.Features.ListQuestions;
using MonoSlice.Modules.Exams.Features.UpdateQuestionBank;
using MonoSlice.Modules.Exams.Features.UpdateQuestion;
using MonoSlice.Modules.Exams.Features.GetStudentExamOverview;
using MonoSlice.Modules.Exams.Features.ImportQuestionBank;
using MonoSlice.Modules.Exams.Features.ExportQuestionBankTemplate;
using MonoSlice.Modules.Exams.Domain.Services;
using MonoSlice.Modules.Exams.Services;
using MonoSlice.Modules.Exams.Persistence;
using MonoSlice.Shared.Abstractions.Contracts;
using MonoSlice.Shared.Abstractions.Messaging;
using MonoSlice.Shared.Infrastructure.Messaging;

namespace MonoSlice.Modules.Exams;

public static class ExamsModule
{
    public static IServiceCollection AddExamsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ExamsDb") ??
                               configuration.GetConnectionString("CoursesDb") ??
                               configuration.GetConnectionString("Database") ??
                               configuration.GetConnectionString("DefaultConnection") ??
                               "Host=localhost;Database=lms_db;Username=postgres;Password=postgres";

        if (connectionString.StartsWith("InMemory:", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<ExamsDbContext>(options =>
            {
                options.UseInMemoryDatabase(connectionString[9..]);
            });
        }
        else
        {
            services.AddDbContext<ExamsDbContext>(options =>
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", ExamsDbContext.DefaultSchema);
                });
            });
        }

        // Register module services and contract API
        services.AddScoped<IExamFinalizerService, MonoSlice.Modules.Exams.Domain.Services.ExamFinalizerService>();
        services.AddScoped<IWordQuestionBankService, WordQuestionBankService>();
        services.AddScoped<IExamsModuleApi, ExamsModuleApi>();

        // Register integration event handlers
        services.AddTransient<IIntegrationEventHandler<CourseDeletedIntegrationEvent>, EventHandlers.CourseDeletedIntegrationEventHandler>();
        services.AddTransient<IIntegrationEventHandler<StudentUnenrolledIntegrationEvent>, EventHandlers.StudentUnenrolledIntegrationEventHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapExamsEndpoints(this IEndpointRouteBuilder app)
    {
        var dispatcher = app.ServiceProvider.GetService<IIntegrationEventDispatcher>();
        dispatcher?.RegisterEvent<CourseDeletedIntegrationEvent>();
        dispatcher?.RegisterEvent<StudentUnenrolledIntegrationEvent>();

        var examsV1Group = app.MapGroup("/api/v1/exams")
            .WithTags("Exams");

        examsV1Group.MapListExamsEndpoint();
        examsV1Group.MapCreateExamEndpoint();
        examsV1Group.MapUpdateExamEndpoint();
        examsV1Group.MapDeleteExamEndpoint();
        examsV1Group.MapPublishExamEndpoint();
        examsV1Group.MapCreateQuestionBankEndpoint();
        examsV1Group.MapListQuestionBanksEndpoint();
        examsV1Group.MapGetQuestionBankEndpoint();
        examsV1Group.MapUpdateQuestionBankEndpoint();
        examsV1Group.MapDeleteQuestionBankEndpoint();
        examsV1Group.MapImportQuestionBankEndpoint();
        examsV1Group.MapExportQuestionBankTemplateEndpoint();
        examsV1Group.MapListQuestionsEndpoint();
        examsV1Group.MapAddQuestionEndpoint();
        examsV1Group.MapGetQuestionEndpoint();
        examsV1Group.MapUpdateQuestionEndpoint();
        examsV1Group.MapDeleteQuestionEndpoint();
        examsV1Group.MapGetExamEndpoint();
        examsV1Group.MapGetStudentExamOverviewEndpoint();
        examsV1Group.MapStartExamEndpoint();
        examsV1Group.MapGetExamQuestionsEndpoint();
        examsV1Group.MapSaveAnswerEndpoint();
        examsV1Group.MapPresignSnapshotEndpoint();
        examsV1Group.MapSubmitExamEndpoint();
        examsV1Group.MapGetExamResultEndpoint();

        var proctorV1Group = app.MapGroup("/api/v1/proctor")
            .WithTags("Proctor");

        proctorV1Group.MapGetLiveCandidatesEndpoint();
        proctorV1Group.MapWarnCandidateEndpoint();
        proctorV1Group.MapForceDisconnectCandidateEndpoint();

        return app;
    }
}
