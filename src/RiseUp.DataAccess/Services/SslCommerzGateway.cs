using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using RiseUp.Entities.Abstractions;

namespace RiseUp.DataAccess.Services;

public sealed class DisabledPaymentGateway : IPaymentGateway
{
    public string Name => "Purchases closed";
    public PaymentMode Mode => PaymentMode.Disabled;
    public Task<CheckoutSession> CreateCheckoutAsync(string orderNumber, decimal amountBdt, CheckoutCustomer customer, CancellationToken ct) =>
        throw new InvalidOperationException("Credit purchases are closed.");
}

public sealed class SslCommerzOptions
{
    public string StoreId { get; set; } = "";
    public string StorePassword { get; set; } = "";
    public bool TestMode { get; set; } = true;
    public string PublicOrigin { get; set; } = "";

    public void Validate(bool production)
    {
        if (production && TestMode) throw new InvalidOperationException("Production requires a live SSLCOMMERZ store.");
        if (string.IsNullOrWhiteSpace(StoreId) || string.IsNullOrWhiteSpace(StorePassword))
            throw new InvalidOperationException("Configure SSLCOMMERZ merchant credentials in the secret store.");
        if (!Uri.TryCreate(PublicOrigin, UriKind.Absolute, out var origin) || origin.Scheme != "https" ||
            origin.UserInfo.Length != 0 || origin.PathAndQuery != "/" || origin.Fragment.Length != 0)
            throw new InvalidOperationException("Site:PublicOrigin must be the public HTTPS origin.");
    }
}

public sealed class SslCommerzGateway(SslCommerzOptions settings, HttpClient http) : IHostedPaymentGateway, IDisposable
{
    private Uri Provider => new(settings.TestMode ? "https://sandbox.sslcommerz.com" : "https://securepay.sslcommerz.com");
    public string Name => settings.TestMode ? "SSLCOMMERZ test checkout" : "SSLCOMMERZ";
    public PaymentMode Mode => settings.TestMode ? PaymentMode.HostedTest : PaymentMode.Live;

    public async Task<CheckoutSession> CreateCheckoutAsync(string orderNumber, decimal amountBdt, CheckoutCustomer customer, CancellationToken ct)
    {
        var origin = settings.PublicOrigin.TrimEnd('/');
        var data = Credentials();
        data["tran_id"] = orderNumber;
        data["total_amount"] = amountBdt.ToString("0.00", CultureInfo.InvariantCulture);
        data["currency"] = "BDT";
        data["success_url"] = data["cancel_url"] = data["fail_url"] = origin + "/payments/return";
        data["ipn_url"] = origin + "/payments/sslcommerz/ipn";
        data["cus_name"] = customer.Name;
        data["cus_email"] = customer.Email;
        data["cus_phone"] = customer.Phone;
        data["cus_add1"] = customer.Billing!.Address;
        data["cus_city"] = customer.Billing.City;
        data["cus_postcode"] = customer.Billing.Postcode;
        data["cus_country"] = customer.Billing.Country;
        data["shipping_method"] = "NO";
        data["num_of_item"] = "1";
        data["product_name"] = "RiseUp Credits";
        data["product_category"] = "Digital services";
        data["product_profile"] = "non-physical-goods";
        data["emi_option"] = "0";
        using var form = new FormUrlEncodedContent(data);
        using var response = await http.PostAsync(new Uri(Provider, "/gwprocess/v4/api.php"), form, ct);
        using var json = await ReadAsync(response, ct);
        var root = json.RootElement;
        if (Field(root, "status") != "SUCCESS" || !Uri.TryCreate(Field(root, "GatewayPageURL"), UriKind.Absolute, out var checkout) ||
            checkout.Scheme != "https" || !checkout.IsDefaultPort || checkout.UserInfo.Length > 0 || checkout.Host != Provider.Host)
            throw new InvalidDataException("SSLCOMMERZ did not return a usable checkout.");
        return new(checkout.AbsoluteUri, Name, settings.TestMode);
    }

    public async Task<ProviderPayment> CheckAsync(string orderNumber, string? validationId, CancellationToken ct)
    {
        var parameters = Credentials();
        parameters["format"] = "json";
        var path = "validationserverAPI.php";
        if (validationId is null)
        {
            path = "merchantTransIDvalidationAPI.php";
            parameters["tran_id"] = orderNumber;
        }
        else parameters["val_id"] = validationId;
        // The provider requires credentials on these GET requests. Never log the request URI.
        using var query = new FormUrlEncodedContent(parameters);
        var url = new Uri(Provider, "/validator/api/" + path + "?" + await query.ReadAsStringAsync(ct));
        using var response = await http.GetAsync(url, ct);
        using var json = await ReadAsync(response, ct);
        var root = json.RootElement;
        if (validationId != null) return Parse(root, orderNumber, validationId);

        if (Field(root, "APIConnect") != "DONE") throw new InvalidDataException("SSLCOMMERZ lookup unavailable.");
        if (Field(root, "no_of_trans_found") == "0") return Empty(ProviderOutcome.Pending);
        if (!root.TryGetProperty("element", out var rows) || rows.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("SSLCOMMERZ returned an incomplete lookup.");
        var attempts = rows.EnumerateArray().Select(row => Parse(row, orderNumber, null)).ToList();
        if (attempts.Any(p => p.Outcome == ProviderOutcome.Review)) return Empty(ProviderOutcome.Review);
        var paid = attempts.Where(p => p.Outcome == ProviderOutcome.Paid).Distinct().ToList();
        if (paid.Count == 1) return paid[0];
        if (paid.Count > 1) return Empty(ProviderOutcome.Review);
        return Empty(attempts.Count > 0 && attempts.All(p => p.Outcome == ProviderOutcome.Failed) ? ProviderOutcome.Failed : ProviderOutcome.Pending);
    }

    private static ProviderPayment Parse(JsonElement row, string order, string? validationId)
    {
        if (Field(row, "tran_id") != order || (validationId != null && Field(row, "val_id") != validationId))
            return Empty(ProviderOutcome.Review);
        var status = Field(row, "status");
        if (status is "FAILED" or "CANCELLED") return Empty(ProviderOutcome.Failed);
        if (status == "PENDING") return Empty(ProviderOutcome.Pending);
        if (status is not ("VALID" or "VALIDATED") || Field(row, "risk_level") != "0" ||
            Field(row, "currency_type") is not ("" or "BDT") || Field(row, "bank_tran_id").Length is 0 or > 100 ||
            !decimal.TryParse(Field(row, "amount"), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount))
            return Empty(ProviderOutcome.Review);
        return new(ProviderOutcome.Paid, order, amount, Field(row, "currency"), Field(row, "bank_tran_id"), Field(row, "card_type"));
    }

    private static ProviderPayment Empty(ProviderOutcome outcome) => new(outcome, "", 0, "", "", "");
    private Dictionary<string, string> Credentials() => new() { ["store_id"] = settings.StoreId, ["store_passwd"] = settings.StorePassword };
    private static string Field(JsonElement row, string key) => row.ValueKind == JsonValueKind.Object && row.TryGetProperty(key, out var field) ? field.ToString() : "";
    private static async Task<JsonDocument> ReadAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode) throw new InvalidDataException("SSLCOMMERZ service unavailable.");
        return await response.Content.ReadFromJsonAsync<JsonDocument>(ct) ?? throw new InvalidDataException("Missing SSLCOMMERZ response.");
    }
    public void Dispose() => http.Dispose();
}
