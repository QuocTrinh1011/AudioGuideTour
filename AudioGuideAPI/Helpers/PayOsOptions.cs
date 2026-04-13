namespace AudioGuideAPI.Helpers;

public class PayOsOptions
{
    public string ClientId { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string ChecksumKey { get; set; } = "";
    public string Endpoint { get; set; } = "https://api-merchant.payos.vn";
    public string AppDeepLinkBase { get; set; } = "audiotour://registration/result";
    public string DefaultCallbackBaseUrl { get; set; } = "";
    public string WebhookUrl { get; set; } = "";
}
