using Web.Services.DashboardService;
using Web.Services.Email;
using Web.Services.ExamServices;
using Web.Services.ReferenceServices;
using Web.Services.ReportServices;
using Web.Services.RoomServices;
using Web.Services.UserExamServices;
using Web.Services.UserServices;

namespace Web;

public static class ServiceInject
{
    public static IServiceCollection InjectService(this IServiceCollection services)
    {
        services.AddScoped<IUser, UserService>();
        services.AddScoped<IExam, ExamService>();
        services.AddScoped<IReferences, ReferenceService>();
        services.AddScoped<IRoom, RoomService>();
        services.AddScoped<IUserExam, UserExamService>();
        services.AddTransient<IMailService, EmailService>();
        services.AddScoped<IReport, ReportService>();
        services.AddScoped<IDashboard, DashboardService>();
        services.AddScoped<IDocx, WordDocumentService>();
        return services;
    }
}