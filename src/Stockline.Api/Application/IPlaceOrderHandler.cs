public interface IPlaceOrderHandler
{
    Task<PlaceOrderResult> Execute(PlaceOrderCommand command, CancellationToken cancellationToken);
}
