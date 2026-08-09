using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using ATAG.Costing.Application.Currency;

namespace ATAG.Costing.Infrastructure.Currency;

/// <summary>
/// Loads the ECB daily euro reference rates and retains the last successful
/// response locally. The rates are informational reference rates rather than a
/// transactional foreign-exchange quote.
/// </summary>
public sealed class EcbExchangeRateService : IExchangeRateService
{
    public const string DailyRatesUrl =
        "https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly HttpClient _httpClient;
    private readonly string _cachePath;

    public EcbExchangeRateService(
        HttpClient? httpClient = null,
        string? cachePath = null)
    {
        _httpClient = httpClient ?? new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(12),
        };
        _cachePath = cachePath ?? Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "ATAG Design Ltd",
            "ATAG Costing",
            "exchange-rates.json");
    }

    public async Task<ExchangeRateSnapshot> GetLatestAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync(
                DailyRatesUrl,
                cancellationToken);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(
                cancellationToken);
            var snapshot = Parse(stream);
            await SaveCacheAsync(snapshot, cancellationToken);
            return snapshot;
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            TaskCanceledException or
            InvalidDataException)
        {
            var retained = await TryLoadCacheAsync(cancellationToken);
            if (retained is not null)
            {
                return retained with { IsRetainedCache = true };
            }

            throw new InvalidOperationException(
                "The ECB reference-rate service is unavailable and no previous rate cache exists.",
                exception);
        }
    }

    private static ExchangeRateSnapshot Parse(Stream stream)
    {
        var document = XDocument.Load(stream);
        XNamespace cubeNamespace =
            "http://www.ecb.int/vocabulary/2002-08-01/eurofxref";
        var datedCube = document
            .Descendants(cubeNamespace + "Cube")
            .FirstOrDefault(element => element.Attribute("time") is not null)
            ?? throw new InvalidDataException(
                "The ECB response does not contain a rate date.");
        var rateDateText = (string?)datedCube.Attribute("time");
        if (!DateOnly.TryParseExact(
                rateDateText,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var rateDate))
        {
            throw new InvalidDataException(
                "The ECB response contains an invalid rate date.");
        }

        var rates = datedCube
            .Elements(cubeNamespace + "Cube")
            .Select(element => new
            {
                Currency = ((string?)element.Attribute("currency"))?
                    .Trim()
                    .ToUpperInvariant(),
                Rate = (string?)element.Attribute("rate"),
            })
            .Where(item =>
                !string.IsNullOrWhiteSpace(item.Currency) &&
                decimal.TryParse(
                    item.Rate,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out _))
            .ToDictionary(
                item => item.Currency!,
                item => decimal.Parse(
                    item.Rate!,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture),
                StringComparer.OrdinalIgnoreCase);
        rates["EUR"] = 1m;

        if (!rates.ContainsKey("GBP"))
        {
            throw new InvalidDataException(
                "The ECB response does not contain the GBP reference rate.");
        }

        return new ExchangeRateSnapshot(
            rateDate,
            DateTimeOffset.UtcNow,
            "European Central Bank daily reference rates",
            DailyRatesUrl,
            rates,
            IsRetainedCache: false);
    }

    private async Task SaveCacheAsync(
        ExchangeRateSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_cachePath)
            ?? throw new InvalidOperationException(
                "The exchange-rate cache path has no directory.");
        Directory.CreateDirectory(directory);
        var temporaryPath = _cachePath + ".tmp";
        await using (var stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(
                stream,
                snapshot,
                JsonOptions,
                cancellationToken);
        }

        File.Move(temporaryPath, _cachePath, overwrite: true);
    }

    private async Task<ExchangeRateSnapshot?> TryLoadCacheAsync(
        CancellationToken cancellationToken)
    {
        if (!File.Exists(_cachePath))
        {
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(_cachePath);
            return await JsonSerializer.DeserializeAsync<ExchangeRateSnapshot>(
                stream,
                JsonOptions,
                cancellationToken);
        }
        catch (Exception exception) when (
            exception is IOException or JsonException)
        {
            return null;
        }
    }
}
