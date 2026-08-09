namespace ATAG.Costing.Application.Currency;

public sealed record ExchangeRateSnapshot(
    DateOnly RateDate,
    DateTimeOffset RetrievedAt,
    string SourceLabel,
    string SourceUrl,
    IReadOnlyDictionary<string, decimal> RatesPerEuro,
    bool IsRetainedCache)
{
    public decimal ConvertFromGbp(decimal amount, string targetCurrencyCode)
    {
        var target = targetCurrencyCode.Trim().ToUpperInvariant();
        if (target == "GBP")
        {
            return amount;
        }

        if (!RatesPerEuro.TryGetValue("GBP", out var poundsPerEuro) ||
            poundsPerEuro <= 0m ||
            !RatesPerEuro.TryGetValue(target, out var targetPerEuro) ||
            targetPerEuro <= 0m)
        {
            throw new InvalidOperationException(
                $"No retained exchange rate is available for {target}.");
        }

        return amount / poundsPerEuro * targetPerEuro;
    }
}

public interface IExchangeRateService
{
    Task<ExchangeRateSnapshot> GetLatestAsync(
        CancellationToken cancellationToken = default);
}
