namespace ConnectStone.Sdk.Models;

/// <summary>
/// Point-of-interaction settings controlling how the order is presented and paid on the physical card machine.
/// </summary>
/// <param name="DevicesSerialNumber">
/// Serial numbers of the specific terminal(s) the order should be routed to. Required for the
/// "direct" flow (order pushed straight to a terminal); omit to use the "listed" flow, where any
/// terminal linked to the account can pull the order from the queue.
/// </param>
public sealed record PoiPaymentSettings(
    PaymentType Type,
    int Installments,
    InstallmentType InstallmentType,
    bool Visible,
    string DisplayName,
    bool PrintOrderReceipt,
    IReadOnlyList<string>? DevicesSerialNumber = null,
    PaymentSetup? PaymentSetup = null);
