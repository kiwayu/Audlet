using System.Net.Http;

namespace Audlet.Data;

// One HTTP client for everything the app fetches, with a short timeout and a cap on buffered response size
// so a slow or misbehaving service cannot hold the UI or fill memory.
public static class Net
{
    public const long MaxResponseBytes = 20 * 1024 * 1024;

    public static HttpClient Http { get; } = Create();

    private static HttpClient Create()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15), MaxResponseContentBufferSize = MaxResponseBytes };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Audlet/" + UpdateChecker.Current + " (audiobook player)");
        return client;
    }
}
