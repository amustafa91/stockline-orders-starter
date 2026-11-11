using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;

public sealed record ProviderRequest(string OperationKey, decimal Amount);
public sealed record ProviderReply(string Code);

public enum ProviderScenario { Approve, Decline, AuthorizeThenDisconnect }
public sealed record ProviderEvent(string Key, string Action, decimal Amount);

public sealed class SimulatedProvider(IConfiguration configuration) : HttpMessageHandler
{
    public ProviderScenario Scenario { get; set; } =
        Enum.Parse<ProviderScenario>(configuration["PaymentProvider:Scenario"] ?? "Approve");
    public ConcurrentQueue<ProviderEvent> Events { get; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payment = await request.Content!.ReadFromJsonAsync<ProviderRequest>(cancellationToken)
            ?? throw new InvalidOperationException("Invalid teaching fixture request.");
        Events.Enqueue(new(payment.OperationKey, "RequestReceived", payment.Amount));
        if (Scenario == ProviderScenario.Decline)
        {
            Events.Enqueue(new(payment.OperationKey, "Declined", payment.Amount));
            return Reply("DECLINED");
        }

        Events.Enqueue(new(payment.OperationKey, "Authorized", payment.Amount));
        if (Scenario == ProviderScenario.AuthorizeThenDisconnect)
        {
            Events.Enqueue(new(payment.OperationKey, "ReplyLost", payment.Amount));
            throw new HttpRequestException("Simulated disconnect after authorization.");
        }

        return Reply(configuration["PaymentProvider:ApprovedReply"] ?? "APPROVED");
    }

    private static HttpResponseMessage Reply(string code) => new(HttpStatusCode.OK)
    {
        Content = JsonContent.Create(new ProviderReply(code))
    };
}
