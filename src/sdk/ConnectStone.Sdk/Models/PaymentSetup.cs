namespace ConnectStone.Sdk.Models;

public sealed record PaymentSetup(PaymentType Type, int Installments, InstallmentType InstallmentType);
