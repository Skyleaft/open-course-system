using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MonoSlice.Modules.Assessments.Domain;
using MonoSlice.Modules.Assessments.Persistence;
using MonoSlice.Shared.Abstractions.Contracts;
using MonoSlice.Shared.Abstractions.Messaging;
using StackExchange.Redis;

namespace MonoSlice.Modules.Assessments.Workers;

public sealed class GradingBackgroundWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GradingBackgroundWorker> _logger;
    private static readonly ActivitySource ActivitySource = new("MonoSlice.Assessments.Worker");

    public const string StreamKey = "stream:exam-events";
    public const string DlqStreamKey = "stream:grading-dlq";
    public const string ConsumerGroup = "cg:assessments:grading";
    public const string ConsumerName = "worker-1";

    public GradingBackgroundWorker(
        IServiceProvider serviceProvider,
        ILogger<GradingBackgroundWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("GradingBackgroundWorker started.");

        using var scope = _serviceProvider.CreateScope();
        var redis = scope.ServiceProvider.GetService<IConnectionMultiplexer>();

        if (redis is null || !redis.IsConnected)
        {
            _logger.LogWarning("Redis connection is not available for GradingBackgroundWorker stream consumer. Background worker running in idle mode.");
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(10000, stoppingToken);
            }
            return;
        }

        var db = redis.GetDatabase();

        // Ensure consumer group exists
        try
        {
            await db.StreamCreateConsumerGroupAsync(StreamKey, ConsumerGroup, "0-0", createStream: true);
            _logger.LogInformation("Created consumer group '{ConsumerGroup}' on stream '{StreamKey}'", ConsumerGroup, StreamKey);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP", StringComparison.OrdinalIgnoreCase))
        {
            // Consumer group already exists
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not initialize Redis consumer group for {StreamKey}", StreamKey);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var entries = await db.StreamReadGroupAsync(
                    StreamKey,
                    ConsumerGroup,
                    ConsumerName,
                    count: 10,
                    noAck: false);

                if (entries == null || entries.Length == 0)
                {
                    await Task.Delay(1000, stoppingToken);
                    continue;
                }

                foreach (var entry in entries)
                {
                    await ProcessStreamEntryAsync(db, entry, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading from stream '{StreamKey}'", StreamKey);
                await Task.Delay(3000, stoppingToken);
            }
        }

        _logger.LogInformation("GradingBackgroundWorker stopped.");
    }

    private async Task ProcessStreamEntryAsync(
        IDatabase db,
        StreamEntry entry,
        CancellationToken cancellationToken)
    {
        var messageId = entry.Id.ToString();
        var entriesDict = entry.Values.ToDictionary(v => v.Name.ToString(), v => v.Value.ToString());

        entriesDict.TryGetValue("traceparent", out var traceparent);
        entriesDict.TryGetValue("payload", out var payloadJson);

        using var activity = ActivitySource.StartActivity(
            "ProcessGradingEvent",
            ActivityKind.Consumer,
            traceparent ?? string.Empty);

        var retryCount = 0;
        if (entriesDict.TryGetValue("retry_count", out var retryStr) && int.TryParse(retryStr, out var parsedRetry))
        {
            retryCount = parsedRetry;
        }

        try
        {
            if (string.IsNullOrEmpty(payloadJson))
            {
                await db.StreamAcknowledgeAsync(StreamKey, ConsumerGroup, entry.Id);
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AssessmentsDbContext>();
            var examsApi = scope.ServiceProvider.GetService<IExamsModuleApi>();

            var examEvent = JsonSerializer.Deserialize<ExamSubmittedIntegrationEventPayload>(payloadJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (examEvent is null)
            {
                await db.StreamAcknowledgeAsync(StreamKey, ConsumerGroup, entry.Id);
                return;
            }

            // Retrieve Exam Course link if available
            Guid courseId = Guid.Empty;
            if (examsApi is not null)
            {
                var examDetails = await examsApi.GetExamByIdAsync(examEvent.ExamId, cancellationToken);
                if (examDetails is not null && examDetails.CourseId.HasValue && examDetails.CourseId.Value != Guid.Empty)
                {
                    courseId = examDetails.CourseId.Value;
                }
            }

            if (courseId == Guid.Empty)
            {
                // Standalone exam or self-contained course
                courseId = examEvent.ExamId;
            }

            // 1. Record Grade
            var existingGrade = await dbContext.GradeRecords
                .FirstOrDefaultAsync(g => g.StudentId == examEvent.StudentId &&
                                          g.CourseId == courseId &&
                                          g.ReferenceId == examEvent.ExamId &&
                                          g.ItemType == GradeItemType.Quiz, cancellationToken);

            if (existingGrade is null)
            {
                var gradeRecord = GradeRecord.Create(
                    examEvent.StudentId,
                    courseId,
                    GradeItemType.Quiz,
                    examEvent.ExamId,
                    examEvent.Score,
                    100m);

                await dbContext.GradeRecords.AddAsync(gradeRecord, cancellationToken);
            }
            else
            {
                existingGrade.UpdateScore(examEvent.Score);
            }

            // 2. Issue Certificate if passed
            if (examEvent.IsPassed)
            {
                var existingCert = await dbContext.Certificates
                    .FirstOrDefaultAsync(c => c.StudentId == examEvent.StudentId && c.CourseId == courseId, cancellationToken);

                if (existingCert is null)
                {
                    var certificate = Certificate.Issue(
                        examEvent.StudentId,
                        courseId,
                        examEvent.Score);

                    await dbContext.Certificates.AddAsync(certificate, cancellationToken);
                    _logger.LogInformation("Issued certificate '{CertNumber}' for student '{StudentId}' on course '{CourseId}'",
                        certificate.CertificateNumber, examEvent.StudentId, courseId);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            // Acknowledge stream message
            await db.StreamAcknowledgeAsync(StreamKey, ConsumerGroup, entry.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process stream message '{MessageId}' (Attempt {RetryCount})", messageId, retryCount + 1);

            retryCount++;
            if (retryCount >= 3)
            {
                // Poison message -> write to DLQ
                await RouteToDlqAsync(db, entry, messageId, payloadJson, ex, retryCount, cancellationToken);
                await db.StreamAcknowledgeAsync(StreamKey, ConsumerGroup, entry.Id);
            }
            else
            {
                // Exponential backoff delay
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), cancellationToken);
            }
        }
    }

    private async Task RouteToDlqAsync(
        IDatabase db,
        StreamEntry entry,
        string messageId,
        string? payloadJson,
        Exception ex,
        int retryCount,
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AssessmentsDbContext>();
            var eventPublisher = scope.ServiceProvider.GetService<IEventStreamPublisher>();

            var submissionId = Guid.Empty;
            if (!string.IsNullOrEmpty(payloadJson))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<ExamSubmittedIntegrationEventPayload>(payloadJson);
                    if (parsed is not null)
                    {
                        submissionId = parsed.SubmissionId;
                    }
                }
                catch
                {
                    // Ignore JSON parse errors during DLQ routing
                }
            }

            if (submissionId == Guid.Empty)
            {
                submissionId = Guid.CreateVersion7();
            }

            var deadLetter = GradingDeadLetter.Create(
                messageId,
                submissionId,
                ex.Message,
                ex.StackTrace,
                payloadJson,
                retryCount);

            await dbContext.GradingDeadLetters.AddAsync(deadLetter, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            if (eventPublisher is not null)
            {
                await eventPublisher.PublishAsync(DlqStreamKey, new
                {
                    DeadLetterId = deadLetter.Id,
                    StreamMessageId = messageId,
                    SubmissionId = submissionId,
                    Error = ex.Message,
                    FailedAtUtc = DateTime.UtcNow
                }, ct: cancellationToken);
            }

            _logger.LogWarning("Routed poison message '{MessageId}' to DLQ table and stream '{DlqStreamKey}'",
                messageId, DlqStreamKey);
        }
        catch (Exception dlqEx)
        {
            _logger.LogCritical(dlqEx, "Failed to persist dead letter for message '{MessageId}'", messageId);
        }
    }

    private sealed record ExamSubmittedIntegrationEventPayload(
        Guid SubmissionId,
        Guid ExamId,
        Guid StudentId,
        decimal Score,
        bool IsPassed,
        DateTime SubmittedAtUtc);
}
