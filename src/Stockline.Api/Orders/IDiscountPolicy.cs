public sealed record DiscountOffer(string Code, decimal Rate);

public interface IDiscountPolicy
{
    DiscountOffer? GetOffer(Product product, int quantity);
}
