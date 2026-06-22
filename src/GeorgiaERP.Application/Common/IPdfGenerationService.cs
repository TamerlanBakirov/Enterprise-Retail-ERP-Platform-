namespace GeorgiaERP.Application.Common;

public interface IPdfGenerationService
{
    byte[] GenerateReceipt(ReceiptData data);
    byte[] GenerateInvoice(InvoiceData data);
    byte[] GenerateBarcodeLabels(IReadOnlyList<BarcodeLabelData> labels, BarcodeLabelSize size);
}
