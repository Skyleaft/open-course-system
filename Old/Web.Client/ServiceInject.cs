using Web.Client.Feature.BankSoal;
using Web.Client.Feature.Dashboards;
using Web.Client.Feature.References;
using Web.Client.Feature.Reports;
using Web.Client.Feature.Roms;
using Web.Client.Feature.UserExams;
using Web.Client.Feature.UserManagements;
using Web.Client.Interfaces;

namespace Web.Client;

public static class ServiceInject
{
    public static IServiceCollection InjectService(this IServiceCollection services)
    {
        services.AddScoped<IUser, UserService>();
        services.AddScoped<IExam, ExamService>();
        services.AddScoped<IReferences, ReferenceService>();
        services.AddScoped<IRoom, RoomService>();
        services.AddScoped<IUserExam, UserExamService>();
        services.AddScoped<IReport, ReportService>();
        services.AddScoped<IDashboard, DashboardService>();
        services.AddScoped<IDocx, DocxUploadService>();
        return services;
    }
}