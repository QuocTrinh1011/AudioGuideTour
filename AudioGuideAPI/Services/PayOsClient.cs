using AudioGuideAPI.Helpers;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace AudioGuideAPI.Services;

public class PayOsClient
{
    private readonly HttpClient _httpClient;
    private readonly PayOsOptions _options;

    public PayOsClient(HttpClient httpClient, IOptions<PayOsOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.ClientId) &&
        !string.IsNullOrWhiteSpace(_options.ApiKey) &&
        !string.IsNullOrWhiteSpace(_options.ChecksumKey);

    public string AppDeepLinkBase => string.IsNullOrWhiteSpace(_options.AppDeepLinkBase)
        ? "audiotour://registration/result"
        : _options.AppDeepLinkBase;

    public string DefaultCallbackBaseUrl => _options.DefaultCallbackBaseUrl ?? string.Empty;
    public string WebhookUrl => _options.WebhookUrl ?? string.Empty;
    public string ChecksumKey => _options.ChecksumKey ?? string.Empty;

    public async Task<PayOsCreatePaymentResult> CreatePaymentLinkAsync(PayOsCreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var signature = PayOsSignatureHelper.CreatePaymentRequestSignature(
            request.Amount,
            request.CancelUrl,
            request.Description,
            request.OrderCode,
            request.ReturnUrl,
            _options.ChecksumKey);

        using var message = CreateRequest(HttpMethod.Post, "/v2/payment-requests");
        message.Content = JsonContent.Create(new
        {
            orderCode = request.OrderCode,
            amount = request.Amount,
            description = request.Description,
            buyerName = request.BuyerName,
            buyerEmail = request.BuyerEmail,
            buyerPhone = request.BuyerPhone,
            items = request.Items.Select(x => new
            {
                name = x.Name,
                quantity = x.Quantity,
                price = x.Price,
                unit = x.Unit
            }),
            cancelUrl = request.CancelUrl,
            returnUrl = request.ReturnUrl,
            expiredAt = request.ExpiredAt,
            signature
        });

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();

        var parsed = JsonSerializer.Deserialize<PayOsResponse<PayOsCreatePaymentData>>(payload, JsonDefaults.Options)
            ?? throw new InvalidOperationException("payOS không trả về dữ liệu tạo link thanh toán.");

        if (!string.Equals(parsed.Code, "00", StringComparison.OrdinalIgnoreCase) || parsed.Data == null)
        {
            throw new InvalidOperationException(parsed.Desc ?? "payOS tạo link thanh toán không thành công.");
        }

        return new PayOsCreatePaymentResult(
            parsed.Data.PaymentLinkId ?? string.Empty,
            parsed.Data.CheckoutUrl ?? string.Empty,
            parsed.Data.QrCode ?? string.Empty,
            parsed.Data.Status ?? "PENDING",
            parsed.Data.OrderCode);
    }

    public async Task<PayOsPaymentInfoResult> GetPaymentLinkInfoAsync(string paymentLookupId, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        using var message = CreateRequest(HttpMethod.Get, $"/v2/payment-requests/{Uri.EscapeDataString(paymentLookupId)}");
        using var response = await _httpClient.SendAsync(message, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();

        var parsed = JsonSerializer.Deserialize<PayOsResponse<PayOsPaymentInfoData>>(payload, JsonDefaults.Options)
            ?? throw new InvalidOperationException("payOS không trả về dữ liệu trạng thái thanh toán.");

        if (!string.Equals(parsed.Code, "00", StringComparison.OrdinalIgnoreCase) || parsed.Data == null)
        {
            throw new InvalidOperationException(parsed.Desc ?? "payOS không trả về trạng thái hợp lệ.");
        }

        return new PayOsPaymentInfoResult(
            parsed.Data.Id ?? string.Empty,
            parsed.Data.OrderCode,
            parsed.Data.Status ?? "PENDING",
            parsed.Data.Amount,
            parsed.Data.AmountPaid,
            parsed.Data.AmountRemaining);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var endpoint = (_options.Endpoint ?? "https://api-merchant.payos.vn").TrimEnd('/');
        var request = new HttpRequestMessage(method, $"{endpoint}{path}");
        request.Headers.Add("x-client-id", _options.ClientId);
        request.Headers.Add("x-api-key", _options.ApiKey);
        return request;
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Chưa cấu hình đủ PayOS trên API.");
        }
    }

    private static class JsonDefaults
    {
        public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    }
}

public record PayOsCreatePaymentRequest(
    long OrderCode,
    int Amount,
    string Description,
    string BuyerName,
    string BuyerEmail,
    string BuyerPhone,
    string CancelUrl,
    string ReturnUrl,
    int ExpiredAt,
    IReadOnlyCollection<PayOsPaymentItem> Items);

public record PayOsPaymentItem(string Name, int Quantity, int Price, string Unit);

public record PayOsCreatePaymentResult(string PaymentLinkId, string CheckoutUrl, string QrCode, string Status, long OrderCode);

public record PayOsPaymentInfoResult(string PaymentLinkId, long OrderCode, string Status, int Amount, int AmountPaid, int AmountRemaining);

internal sealed class PayOsResponse<TData>
{
    public string Code { get; set; } = "";
    public string Desc { get; set; } = "";
    public TData? Data { get; set; }
    public string Signature { get; set; } = "";
}

internal sealed class PayOsCreatePaymentData
{
    public string PaymentLinkId { get; set; } = "";
    public string CheckoutUrl { get; set; } = "";
    public string QrCode { get; set; } = "";
    public string Status { get; set; } = "";
    public long OrderCode { get; set; }
}

internal sealed class PayOsPaymentInfoData
{
    public string Id { get; set; } = "";
    public long OrderCode { get; set; }
    public int Amount { get; set; }
    public int AmountPaid { get; set; }
    public int AmountRemaining { get; set; }
    public string Status { get; set; } = "";
}
