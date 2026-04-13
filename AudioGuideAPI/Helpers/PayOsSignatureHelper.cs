using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AudioGuideAPI.Helpers;

public static class PayOsSignatureHelper
{
    public static string CreatePaymentRequestSignature(int amount, string cancelUrl, string description, long orderCode, string returnUrl, string checksumKey)
    {
        var payload = $"amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}";
        return ComputeHmac(payload, checksumKey);
    }

    public static bool IsValidWebhookSignature(JsonElement data, string signature, string checksumKey)
    {
        if (string.IsNullOrWhiteSpace(signature))
        {
            return false;
        }

        var queryString = ConvertObjectToQueryString(data);
        var expected = ComputeHmac(queryString, checksumKey);
        return string.Equals(expected, signature, StringComparison.OrdinalIgnoreCase);
    }

    private static string ConvertObjectToQueryString(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        foreach (var property in element.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal))
        {
            parts.Add($"{property.Name}={ConvertValue(property.Value)}");
        }

        return string.Join("&", parts);
    }

    private static string ConvertValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Null => string.Empty,
            JsonValueKind.Undefined => string.Empty,
            JsonValueKind.False => bool.FalseString.ToLowerInvariant(),
            JsonValueKind.True => bool.TrueString.ToLowerInvariant(),
            JsonValueKind.Number => value.ToString(),
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Array => JsonSerializer.Serialize(
                value.EnumerateArray().Select(SortJsonElement).ToList(),
                new JsonSerializerOptions { WriteIndented = false }),
            JsonValueKind.Object => JsonSerializer.Serialize(
                SortJsonElement(value),
                new JsonSerializerOptions { WriteIndented = false }),
            _ => value.ToString()
        };
    }

    private static object? SortJsonElement(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Object => value.EnumerateObject()
                .OrderBy(x => x.Name, StringComparer.Ordinal)
                .ToDictionary(x => x.Name, x => SortJsonElement(x.Value)),
            JsonValueKind.Array => value.EnumerateArray().Select(SortJsonElement).ToList(),
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.TryGetInt64(out var longValue)
                ? longValue
                : value.TryGetDecimal(out var decimalValue)
                    ? decimalValue
                    : value.ToString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => value.ToString()
        };
    }

    private static string ComputeHmac(string payload, string checksumKey)
    {
        var keyBytes = Encoding.UTF8.GetBytes(checksumKey ?? string.Empty);
        var payloadBytes = Encoding.UTF8.GetBytes(payload ?? string.Empty);
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(payloadBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
