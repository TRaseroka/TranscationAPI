namespace TransactionAggregation.Contracts;

public enum PaymentMethod
{
    CashDeposit = 1,
    ElectronicFundsTransfer = 2,
    ImmediatePayment = 3,
    DebitOrder = 4,
    StopOrder = 5,
    CardPayment = 6,
    InternationalTransfer = 7,
    ATMTransfer = 8,
    MobileBankingPayment = 9,
    ChequePayment = 10,
    RealTimeGrossSettlement = 11,
    PayShap = 12
}