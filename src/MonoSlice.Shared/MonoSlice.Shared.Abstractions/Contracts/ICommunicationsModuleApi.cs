namespace MonoSlice.Shared.Abstractions.Contracts;

public interface ICommunicationsModuleApi
{
    Task<int> GetActiveAnnouncementsCountAsync(Guid? courseId = null, CancellationToken cancellationToken = default);
    Task<int> GetDiscussionThreadsCountAsync(Guid courseId, Guid? lessonId = null, CancellationToken cancellationToken = default);
    Task<bool> IsThreadOpenAsync(Guid threadId, CancellationToken cancellationToken = default);
}
