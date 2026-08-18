using C2C.DevicePlatform.Api.Services;
using System.Text.Json;

namespace C2C.DevicePlatform.Tests;

public sealed class SensitiveDataMaskingServiceTests
{
    [Fact]
    public void MaskJson_MasksSensitiveProperties()
    {
        var service = new SensitiveDataMaskingService();
        const string input = "{\"apiKey\":\"ABCDEF1234567890\",\"timeout\":30}";

        var masked = service.MaskJson(input);
        using var doc = JsonDocument.Parse(masked);

        var apiKeyValue = doc.RootElement.GetProperty("apiKey").GetString();
        Assert.NotNull(apiKeyValue);
        Assert.NotEqual("ABCDEF1234567890", apiKeyValue);
        Assert.Equal(30, doc.RootElement.GetProperty("timeout").GetInt32());
    }

    [Fact]
    public void MaskJson_BlocksScriptContent()
    {
        var service = new SensitiveDataMaskingService();
        const string input = "{\"notes\":\"<script>alert('xss')</script>\"}";

        var masked = service.MaskJson(input);
        using var doc = JsonDocument.Parse(masked);

        Assert.Equal("[blocked-script-content]", doc.RootElement.GetProperty("notes").GetString());
    }
}
