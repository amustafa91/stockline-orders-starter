public sealed class OrderPricing(IEnumerable<IDiscountPolicy> policies)
{
    private readonly IDiscountPolicy[] _policies = policies.ToArray();

    public DiscountOffer? SelectOffer(Product product, int quantity, IDiscountPolicy? customerPolicy = null)
    {
        DiscountOffer? best = null;
        var candidates = customerPolicy is null ? _policies : _policies.Append(customerPolicy);
        foreach (var policy in candidates)
        {
            var offer = policy.GetOffer(product, quantity);
            // The earlier policy wins a tie. Composition lists wholesale first.
            if (offer is not null && (best is null || offer.Rate > best.Rate))
                best = offer;
        }

        return best;
    }

    public decimal Calculate(Product product, int quantity, IDiscountPolicy? customerPolicy = null)
    {
        var subtotal = product.UnitPrice * quantity;
        var discountRate = SelectOffer(product, quantity, customerPolicy)?.Rate ?? 0m;
        return decimal.Round(subtotal * (1m - discountRate), 2, MidpointRounding.AwayFromZero);
    }
}
