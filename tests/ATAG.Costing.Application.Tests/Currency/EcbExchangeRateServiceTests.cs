using System.Net;
using System.Text;
using ATAG.Costing.Infrastructure.Currency;
using Xunit;

namespace ATAG.Costing.Application.Tests.Currency;

public sealed class EcbExchangeRateServiceTests
{
    [Fact]
    public async Task LatestRates_ConvertFromGbpAndRetainAWorkingCache()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            $"atag-rates-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var cachePath = Path.Combine(directory, "exchange-rates.json");

        try
        {
            const string xml =
                """
                <?xml version="1.0" encoding="UTF-8"?>
                <gesmes:Envelope
                  xmlns:gesmes="http://www.gesmes.org/xml/2002-08-01"
                  xmlns="http://www.ecb.int/vocabulary/2002-08-01/eurofxref">
                  <Cube>
                    <Cube time="2026-07-28">
                      <Cube currency="USD" rate="1.2000"/>
                      <Cube currency="GBP" rate="0.8000"/>
                    </Cube>
                  </Cube>
                </gesmes:Envelope>
                """;
            var onlineService = new EcbExchangeRateService(
                new HttpClient(
                    new StubHandler(
                        new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                xml,
                                Encoding.UTF8,
                                "application/xml"),
                        })),
                cachePath);

            var online = await onlineService.GetLatestAsync();

            Assert.False(online.IsRetainedCache);
            Assert.Equal(150m, online.ConvertFromGbp(100m, "USD"));
            Assert.True(File.Exists(cachePath));

            var offlineService = new EcbExchangeRateService(
                new HttpClient(
                    new StubHandler(
                        new HttpResponseMessage(
                            HttpStatusCode.ServiceUnavailable))),
                cachePath);

            var retained = await offlineService.GetLatestAsync();

            Assert.True(retained.IsRetainedCache);
            Assert.Equal(150m, retained.ConvertFromGbp(100m, "USD"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class StubHandler(HttpResponseMessage response)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(response);
    }
}
