namespace GeorgiaERP.Application.Common;

public interface IExcelService
{
    byte[] ExportProducts(IReadOnlyList<ProductExportRow> rows);
    byte[] ExportStockLevels(IReadOnlyList<StockExportRow> rows);
    byte[] GenerateProductImportTemplate();
    Result<List<ProductImportRow>> ParseProductImport(Stream fileStream);
}
