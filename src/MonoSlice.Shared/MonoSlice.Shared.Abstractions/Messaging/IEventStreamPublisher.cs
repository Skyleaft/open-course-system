namespace MonoSlice.Shared.Abstractions.Messaging;

public interface IEventStreamPublisher
{
    Task<string> PublishAsync<T>(
        string streamKey, 
        T payload, 
        int? maxLen = 100000, 
        CancellationToken ct = default);

    Task<string> PublishRawAsync(
        string streamKey, 
        IDictionary<string, string> entries, 
        int? maxLen = 100000, 
        CancellationToken ct = default);
}
