using System.Net.Http.Json;

public sealed class PaymentAdapter(IHttpClientFactory clients) : IPaymentGateway
{
    public async Task<PaymentDecision> AuthorizeAsync(
        string operationKey, decimal amount, CancellationToken cancellationToken)
    {
        using var client = clients.CreateClient("provider");
        PaymentDecision decision;
        try
        {
            using var response = await client.PostAsJsonAsync("authorize",
                new ProviderRequest(operationKey, amount), cancellationToken);
            response.EnsureSuccessStatusCode();
            var reply = await response.Content.ReadFromJsonAsync<ProviderReply>(cancellationToken);
            decision = reply?.Code switch
            {
                "APPROVED" or "SUCCESS" => PaymentDecision.Approved,
                "DECLINED" => PaymentDecision.Declined,
                _ => PaymentDecision.Unknown
            };
        }
        catch (HttpRequestException)
        {
            decision = PaymentDecision.Unknown;
        }
        return decision;
    }
}
