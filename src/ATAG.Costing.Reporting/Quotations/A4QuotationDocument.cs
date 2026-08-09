namespace ATAG.Costing.Reporting.Quotations;

public sealed record A4QuotationDocument(
    string QuoteNumber,
    DateOnly QuoteDate,
    string CustomerName,
    IReadOnlyList<string> CustomerAddressLines,
    string QuotedBy,
    string Description,
    decimal ItemCount,
    decimal LengthPerItemMetres,
    decimal TotalQuantityMetres,
    string CurrencyCode,
    string CurrencySymbol,
    decimal UnitPricePerMetre,
    decimal GoodsTotal,
    decimal DeliveryCharge,
    string ConductorSpecification,
    string InsulationSpecification,
    string ColourSpecification,
    string Packaging,
    string ReelSize,
    string EstimatedDelivery,
    string SpecialNotes,
    string TermsAndConditions)
{
    public string IssuerName { get; init; } = "Costing App";

    public IReadOnlyList<string> IssuerAddressLines { get; init; } = [];

    public decimal TotalPrice => GoodsTotal + DeliveryCharge;
}
