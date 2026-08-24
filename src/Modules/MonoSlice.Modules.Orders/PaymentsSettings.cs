namespace MonoSlice.Modules.Orders;

public sealed class PaymentsSettings
{
    public const string SectionName = "Payments";

    public string WebhookSecret { get; set; } = "default_payments_webhook_secret_key_change_in_production";
    public string DefaultCurrency { get; set; } = "IDR";
    public string PaymentGatewayCheckoutUrl { get; set; } = "https://checkout.paymentgateway.local/pay";
}
