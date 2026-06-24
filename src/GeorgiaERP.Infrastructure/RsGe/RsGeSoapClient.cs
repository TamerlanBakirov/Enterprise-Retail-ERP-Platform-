using System.Diagnostics;
using System.Text;
using System.Xml.Linq;
using GeorgiaERP.Application.Compliance;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GeorgiaERP.Infrastructure.RsGe;

public class RsGeSoapClient : Application.Compliance.IRsGeSoapClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RsGeSoapClient> _logger;
    private readonly string _serviceUser;
    private readonly string _servicePassword;
    private readonly string _baseUrl;

    private static readonly XNamespace SoapNs = "http://schemas.xmlsoap.org/soap/envelope/";
    private static readonly XNamespace WaybillNs = "http://tempuri.org/";

    public RsGeSoapClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<RsGeSoapClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _serviceUser = _configuration["RsGe:ServiceUser"] ?? "";
        _servicePassword = _configuration["RsGe:ServicePassword"] ?? "";
        _baseUrl = _configuration["RsGe:WaybillServiceUrl"]
            ?? "https://services.rs.ge/WayBillService/WayBillService.asmx";
    }

    public async Task<string> GetMyIpAsync()
    {
        var soapBody = new XElement(WaybillNs + "what_is_my_ip");
        var response = await SendSoapRequestAsync("what_is_my_ip", soapBody);

        var resultElement = response
            .Descendants(WaybillNs + "what_is_my_ipResult")
            .FirstOrDefault();

        return resultElement?.Value ?? "";
    }

    public async Task<RsGeServiceUser> CheckServiceUserAsync(string serviceUser, string servicePassword)
    {
        var soapBody = new XElement(WaybillNs + "chek_service_user",
            new XElement(WaybillNs + "su", serviceUser),
            new XElement(WaybillNs + "sp", servicePassword));

        var response = await SendSoapRequestAsync("chek_service_user", soapBody);

        var resultElement = response
            .Descendants(WaybillNs + "chek_service_userResult")
            .FirstOrDefault();

        var payerId = resultElement?.Element(WaybillNs + "un_id")?.Value ?? "";
        var userId = resultElement?.Element(WaybillNs + "user_id")?.Value ?? "";

        return new RsGeServiceUser(payerId, userId);
    }

    public async Task<RsGeNameResult> GetNameFromTinAsync(string tin)
    {
        var soapBody = new XElement(WaybillNs + "get_name_from_tin",
            new XElement(WaybillNs + "su", _serviceUser),
            new XElement(WaybillNs + "sp", _servicePassword),
            new XElement(WaybillNs + "tin", tin));

        var response = await SendSoapRequestAsync("get_name_from_tin", soapBody);

        var resultElement = response
            .Descendants(WaybillNs + "get_name_from_tinResult")
            .FirstOrDefault();

        var name = resultElement?.Value ?? "";
        var found = !string.IsNullOrWhiteSpace(name);

        return new RsGeNameResult(name, found);
    }

    public async Task<bool> IsVatPayerAsync(string tin)
    {
        var soapBody = new XElement(WaybillNs + "is_vat_payer_tin",
            new XElement(WaybillNs + "su", _serviceUser),
            new XElement(WaybillNs + "sp", _servicePassword),
            new XElement(WaybillNs + "tin", tin));

        var response = await SendSoapRequestAsync("is_vat_payer_tin", soapBody);

        var resultElement = response
            .Descendants(WaybillNs + "is_vat_payer_tinResult")
            .FirstOrDefault();

        return resultElement?.Value == "1" || resultElement?.Value?.ToLowerInvariant() == "true";
    }

    public async Task<IReadOnlyList<RsGeUnit>> GetUnitsAsync()
    {
        var soapBody = new XElement(WaybillNs + "get_waybill_units",
            new XElement(WaybillNs + "su", _serviceUser),
            new XElement(WaybillNs + "sp", _servicePassword));

        var response = await SendSoapRequestAsync("get_waybill_units", soapBody);

        var units = response
            .Descendants(WaybillNs + "get_waybill_unitsResult")
            .Descendants()
            .Where(e => e.Name.LocalName == "UNIT")
            .Select(e => new RsGeUnit(
                Id: int.TryParse(e.Element("ID")?.Value ?? e.Element(WaybillNs + "ID")?.Value, out var id) ? id : 0,
                Name: e.Element("UNIT_NAME")?.Value ?? e.Element(WaybillNs + "UNIT_NAME")?.Value ?? ""))
            .ToList();

        return units;
    }

    public async Task<IReadOnlyList<RsGeTransportType>> GetTransportTypesAsync()
    {
        var soapBody = new XElement(WaybillNs + "get_transport_types",
            new XElement(WaybillNs + "su", _serviceUser),
            new XElement(WaybillNs + "sp", _servicePassword));

        var response = await SendSoapRequestAsync("get_transport_types", soapBody);

        var types = response
            .Descendants(WaybillNs + "get_transport_typesResult")
            .Descendants()
            .Where(e => e.Name.LocalName == "TRANSPORT_TYPE")
            .Select(e => new RsGeTransportType(
                Id: int.TryParse(e.Element("ID")?.Value ?? e.Element(WaybillNs + "ID")?.Value, out var id) ? id : 0,
                Name: e.Element("NAME")?.Value ?? e.Element(WaybillNs + "NAME")?.Value ?? ""))
            .ToList();

        return types;
    }

    public async Task<IReadOnlyList<RsGeWaybillType>> GetWaybillTypesAsync()
    {
        var soapBody = new XElement(WaybillNs + "get_waybill_types",
            new XElement(WaybillNs + "su", _serviceUser),
            new XElement(WaybillNs + "sp", _servicePassword));

        var response = await SendSoapRequestAsync("get_waybill_types", soapBody);

        var types = response
            .Descendants(WaybillNs + "get_waybill_typesResult")
            .Descendants()
            .Where(e => e.Name.LocalName == "WAYBILL_TYPE")
            .Select(e => new RsGeWaybillType(
                Id: int.TryParse(e.Element("ID")?.Value ?? e.Element(WaybillNs + "ID")?.Value, out var id) ? id : 0,
                Name: e.Element("NAME")?.Value ?? e.Element(WaybillNs + "NAME")?.Value ?? ""))
            .ToList();

        return types;
    }

    public async Task<RsGeWaybillResult> SaveWaybillAsync(RsGeWaybillRequest request)
    {
        var goodsXml = new XElement(WaybillNs + "goods",
            request.Goods.Select((g, i) => new XElement(WaybillNs + "GOODS",
                new XElement(WaybillNs + "ID", i + 1),
                new XElement(WaybillNs + "W_NAME", g.ProductName),
                new XElement(WaybillNs + "UNIT_ID", g.UnitId),
                new XElement(WaybillNs + "QUANTITY", g.Quantity),
                new XElement(WaybillNs + "PRICE", g.Price),
                g.BarCode != null ? new XElement(WaybillNs + "BAR_CODE", g.BarCode) : null!
            )));

        var soapBody = new XElement(WaybillNs + "save_waybill",
            new XElement(WaybillNs + "su", _serviceUser),
            new XElement(WaybillNs + "sp", _servicePassword),
            new XElement(WaybillNs + "waybill_type", request.WaybillType),
            new XElement(WaybillNs + "buyer_tin", request.BuyerTin),
            new XElement(WaybillNs + "start_address", request.StartAddress),
            new XElement(WaybillNs + "end_address", request.EndAddress),
            request.TransportTypeId.HasValue ? new XElement(WaybillNs + "transport_type_id", request.TransportTypeId.Value) : null!,
            !string.IsNullOrEmpty(request.CarNumber) ? new XElement(WaybillNs + "car_number", request.CarNumber) : null!,
            !string.IsNullOrEmpty(request.DriverTin) ? new XElement(WaybillNs + "driver_tin", request.DriverTin) : null!,
            !string.IsNullOrEmpty(request.Comment) ? new XElement(WaybillNs + "comment", request.Comment) : null!,
            goodsXml);

        var response = await SendSoapRequestAsync("save_waybill", soapBody);

        var resultElement = response
            .Descendants(WaybillNs + "save_waybillResult")
            .FirstOrDefault();

        var waybillIdStr = resultElement?.Element(WaybillNs + "WAYBILL_ID")?.Value
            ?? resultElement?.Element("WAYBILL_ID")?.Value;
        var waybillNumber = resultElement?.Element(WaybillNs + "WAYBILL_NUMBER")?.Value
            ?? resultElement?.Element("WAYBILL_NUMBER")?.Value;
        var errorCode = resultElement?.Element(WaybillNs + "ERROR_CODE")?.Value
            ?? resultElement?.Element("ERROR_CODE")?.Value;
        var errorMessage = resultElement?.Element(WaybillNs + "ERROR_MESSAGE")?.Value
            ?? resultElement?.Element("ERROR_MESSAGE")?.Value;

        var success = string.IsNullOrEmpty(errorCode) || errorCode == "0";
        int? waybillId = int.TryParse(waybillIdStr, out var wid) ? wid : null;

        return new RsGeWaybillResult(success, waybillId, waybillNumber, errorCode, errorMessage);
    }

    public async Task<RsGeResult> SendWaybillAsync(int waybillId)
    {
        var soapBody = new XElement(WaybillNs + "send_waybill",
            new XElement(WaybillNs + "su", _serviceUser),
            new XElement(WaybillNs + "sp", _servicePassword),
            new XElement(WaybillNs + "waybill_id", waybillId));

        var response = await SendSoapRequestAsync("send_waybill", soapBody);
        return ParseSimpleResult(response, "send_waybillResult");
    }

    public async Task<RsGeResult> ConfirmWaybillAsync(int waybillId)
    {
        var soapBody = new XElement(WaybillNs + "confirm_waybill",
            new XElement(WaybillNs + "su", _serviceUser),
            new XElement(WaybillNs + "sp", _servicePassword),
            new XElement(WaybillNs + "waybill_id", waybillId));

        var response = await SendSoapRequestAsync("confirm_waybill", soapBody);
        return ParseSimpleResult(response, "confirm_waybillResult");
    }

    public async Task<RsGeResult> CloseWaybillAsync(int waybillId)
    {
        var soapBody = new XElement(WaybillNs + "close_waybill",
            new XElement(WaybillNs + "su", _serviceUser),
            new XElement(WaybillNs + "sp", _servicePassword),
            new XElement(WaybillNs + "waybill_id", waybillId));

        var response = await SendSoapRequestAsync("close_waybill", soapBody);
        return ParseSimpleResult(response, "close_waybillResult");
    }

    public async Task<RsGeResult> RejectWaybillAsync(int waybillId)
    {
        var soapBody = new XElement(WaybillNs + "reject_waybill",
            new XElement(WaybillNs + "su", _serviceUser),
            new XElement(WaybillNs + "sp", _servicePassword),
            new XElement(WaybillNs + "waybill_id", waybillId));

        var response = await SendSoapRequestAsync("reject_waybill", soapBody);
        return ParseSimpleResult(response, "reject_waybillResult");
    }

    public async Task<RsGeWaybillData?> GetWaybillAsync(int waybillId)
    {
        var soapBody = new XElement(WaybillNs + "get_waybill",
            new XElement(WaybillNs + "su", _serviceUser),
            new XElement(WaybillNs + "sp", _servicePassword),
            new XElement(WaybillNs + "waybill_id", waybillId));

        var response = await SendSoapRequestAsync("get_waybill", soapBody);

        var resultElement = response
            .Descendants(WaybillNs + "get_waybillResult")
            .FirstOrDefault();

        if (resultElement is null)
            return null;

        return new RsGeWaybillData
        {
            Id = int.TryParse(resultElement.Element(WaybillNs + "ID")?.Value ?? resultElement.Element("ID")?.Value, out var id) ? id : 0,
            WaybillNumber = resultElement.Element(WaybillNs + "WAYBILL_NUMBER")?.Value ?? resultElement.Element("WAYBILL_NUMBER")?.Value,
            WaybillType = int.TryParse(resultElement.Element(WaybillNs + "TYPE")?.Value ?? resultElement.Element("TYPE")?.Value, out var type) ? type : 0,
            SellerTin = resultElement.Element(WaybillNs + "SELLER_TIN")?.Value ?? resultElement.Element("SELLER_TIN")?.Value,
            BuyerTin = resultElement.Element(WaybillNs + "BUYER_TIN")?.Value ?? resultElement.Element("BUYER_TIN")?.Value,
            Status = int.TryParse(resultElement.Element(WaybillNs + "STATUS")?.Value ?? resultElement.Element("STATUS")?.Value, out var status) ? status : 0,
            StatusText = resultElement.Element(WaybillNs + "STATUS_TEXT")?.Value ?? resultElement.Element("STATUS_TEXT")?.Value
        };
    }

    public async Task<RsGeResult> SaveInvoiceAsync(RsGeInvoiceRequest request)
    {
        var itemsXml = new XElement(WaybillNs + "items",
            request.Items.Select((item, i) => new XElement(WaybillNs + "ITEM",
                new XElement(WaybillNs + "ID", i + 1),
                new XElement(WaybillNs + "DESCRIPTION", item.Description),
                new XElement(WaybillNs + "QUANTITY", item.Quantity),
                new XElement(WaybillNs + "UNIT_PRICE", item.UnitPrice),
                new XElement(WaybillNs + "VAT_AMOUNT", item.VatAmount)
            )));

        var soapBody = new XElement(WaybillNs + "save_invoice",
            new XElement(WaybillNs + "su", _serviceUser),
            new XElement(WaybillNs + "sp", _servicePassword),
            new XElement(WaybillNs + "buyer_tin", request.BuyerTin),
            new XElement(WaybillNs + "buyer_name", request.BuyerName),
            new XElement(WaybillNs + "invoice_date", request.InvoiceDate.ToString("yyyy-MM-dd")),
            itemsXml);

        var response = await SendSoapRequestAsync("save_invoice", soapBody);
        return ParseSimpleResult(response, "save_invoiceResult");
    }

    public async Task<RsGeResult> SubmitVatDeclarationAsync(RsGeVatDeclarationRequest request)
    {
        var soapBody = new XElement(WaybillNs + "save_vat_declaration",
            new XElement(WaybillNs + "su", _serviceUser),
            new XElement(WaybillNs + "sp", _servicePassword),
            new XElement(WaybillNs + "period_start", request.PeriodStart.ToString("yyyy-MM-dd")),
            new XElement(WaybillNs + "period_end", request.PeriodEnd.ToString("yyyy-MM-dd")),
            new XElement(WaybillNs + "output_vat", request.TotalOutputVat),
            new XElement(WaybillNs + "input_vat", request.TotalInputVat),
            new XElement(WaybillNs + "net_vat", request.NetVat));

        var response = await SendSoapRequestAsync("save_vat_declaration", soapBody);
        return ParseSimpleResult(response, "save_vat_declarationResult");
    }

    private RsGeResult ParseSimpleResult(XDocument response, string resultElementName)
    {
        var resultElement = response
            .Descendants(WaybillNs + resultElementName)
            .FirstOrDefault();

        var errorCode = resultElement?.Element(WaybillNs + "ERROR_CODE")?.Value
            ?? resultElement?.Element("ERROR_CODE")?.Value;
        var errorMessage = resultElement?.Element(WaybillNs + "ERROR_MESSAGE")?.Value
            ?? resultElement?.Element("ERROR_MESSAGE")?.Value;

        var success = string.IsNullOrEmpty(errorCode) || errorCode == "0";
        return new RsGeResult(success, errorCode, errorMessage);
    }

    private async Task<XDocument> SendSoapRequestAsync(string soapAction, XElement bodyContent)
    {
        var soapEnvelope = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(SoapNs + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soap", SoapNs),
                new XAttribute(XNamespace.Xmlns + "tem", WaybillNs),
                new XElement(SoapNs + "Header"),
                new XElement(SoapNs + "Body", bodyContent)));

        var xmlString = soapEnvelope.Declaration + soapEnvelope.ToString();

        // SECURITY: Log only the SOAP action, never the raw XML which contains
        // RS.GE service credentials (<su> and <sp> elements).
        _logger.LogDebug("RS.GE SOAP Request [{SoapAction}] sent to {Endpoint}", soapAction, _baseUrl);

        var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl)
        {
            Content = new StringContent(xmlString, Encoding.UTF8, "text/xml")
        };

        request.Headers.Add("SOAPAction", $"http://tempuri.org/{soapAction}");

        // Propagate correlation ID for end-to-end tracing through RS.GE pipeline.
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        request.Headers.Add("X-Correlation-Id", correlationId);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "RS.GE SOAP request failed for action {SoapAction}", soapAction);
            throw;
        }

        var responseContent = await response.Content.ReadAsStringAsync();

        // SECURITY: Do not log full response XML at Debug level; it may contain
        // TIN numbers, buyer details, and other PII. Log only metadata.
        _logger.LogDebug("RS.GE SOAP Response [{SoapAction}] HTTP {StatusCode}, Length={ContentLength}",
            soapAction, (int)response.StatusCode, responseContent.Length);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("RS.GE SOAP request returned HTTP {StatusCode} for action {SoapAction}",
                (int)response.StatusCode, soapAction);
            throw new HttpRequestException(
                $"RS.GE service returned HTTP {(int)response.StatusCode} for action {soapAction}");
        }

        return XDocument.Parse(responseContent);
    }
}
