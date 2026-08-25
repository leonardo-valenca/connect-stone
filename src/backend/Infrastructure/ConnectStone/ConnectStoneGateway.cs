using Application.Abstractions.ConnectStone;
using ConnectStone.Sdk;
using ConnectStone.Sdk.Exceptions;
using ConnectStone.Sdk.Models;
using Domain;

namespace Infrastructure.ConnectStone;

public sealed class ConnectStoneGateway(IConnectStoneClient client) : IConnectStoneGateway
{
    public async Task<string> CreateOrderAsync(DemoOrder order, CancellationToken cancellationToken)
    {
        var request = new CreateOrderRequest(
            Customer: new CustomerRequest(order.CustomerName),
            Items: [new OrderItemRequest(order.AmountInCents, order.Description, Quantity: 1)],
            Closed: false,
            PoiPaymentSettings: new PoiPaymentSettings(
                Type: PaymentType.Credit,
                Installments: 1,
                InstallmentType: InstallmentType.Merchant,
                Visible: true,
                DisplayName: order.Description,
                PrintOrderReceipt: true));

        try
        {
            var connectStoneOrder = await client.CreateOrderAsync(request, cancellationToken);
            return connectStoneOrder.Id;
        }
        catch (TooManyOpenOrdersException)
        {
            throw new ConnectStoneOrderLimitExceededException();
        }
    }

    public Task CloseOrderAsync(string connectStoneOrderId, ConnectStoneCloseStatus status, CancellationToken cancellationToken) =>
        client.CloseOrderAsync(connectStoneOrderId, ToSdkStatus(status), cancellationToken);

    private static OrderCloseStatus ToSdkStatus(ConnectStoneCloseStatus status) => status switch
    {
        ConnectStoneCloseStatus.Paid => OrderCloseStatus.Paid,
        ConnectStoneCloseStatus.Canceled => OrderCloseStatus.Canceled,
        ConnectStoneCloseStatus.Failed => OrderCloseStatus.Failed,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };
}
