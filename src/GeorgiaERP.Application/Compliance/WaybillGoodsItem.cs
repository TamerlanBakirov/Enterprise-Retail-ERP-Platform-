namespace GeorgiaERP.Application.Compliance;

/// <summary>
/// Serialized shape of a single waybill line, persisted as JSON in
/// <c>RsGeWaybill.GoodsData</c> and rehydrated by the submission processor
/// when building the save_waybill SOAP request.
/// </summary>
public record WaybillGoodsItem(
    string ProductName,
    int UnitId,
    decimal Quantity,
    decimal Price,
    string? BarCode);
