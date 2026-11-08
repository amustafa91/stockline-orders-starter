public abstract record PlaceOrderResult;
public sealed record OrderRejected(string Error) : PlaceOrderResult;
public sealed record OrderPlaced(Order Order) : PlaceOrderResult;
