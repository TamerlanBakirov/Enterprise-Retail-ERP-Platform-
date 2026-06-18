using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GeorgiaERP.Infrastructure.RsGe;

public class RsGeSoapClient : IRsGeSoapClient
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

    public Task<IReadOnlyList<RsGeTransportType>> GetTransportTypesAsync()
    {
        throw new NotImplementedException("GetTransportTypesAsync is not yet implemented.");
    }

    public Task<IReadOnlyList<RsGeWaybillType>> GetWaybillTypesAsync()
    {
        throw new NotImplementedException("GetWaybillTypesAsync is not yet implemented.");
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

    public Task<RsGeResult> SendWaybillAsync(int waybillId)
    {
        throw new NotImplementedException("SendWaybillAsync is not yet implemented.");
    }

    public Task<RsGeResult> ConfirmWaybillAsync(int waybillId)
    {
        throw new NotImplementedException("ConfirmWaybillAsync is not yet implemented.");
    }

    public Task<RsGeResult> CloseWaybillAsync(int waybillId)
    {
        throw new NotImplementedException("CloseWaybillAsync is not yet implemented.");
    }

    public Task<RsGeResult> RejectWaybillAsync(int waybillId)
    {
        throw new NotImplementedException("RejectWaybillAsync is not yet implemented.");
    }

    public Task<RsGeWaybillData?> GetWaybillAsync(int waybillId)
    {
        throw new NotImplementedException("GetWaybillAsync is not yet implemented.");
    }

    public Task<RsGeResult> SaveInvoiceAsync(RsGeInvoiceRequest request)
    {
        throw new NotImplementedException("SaveInvoiceAsync is not yet implemented.");
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

        _logger.LogDebug("RS.GE SOAP Request [{SoapAction}]: {Request}", soapAction, xmlString);

        var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl)
        {
            Content = new StringContent(xmlString, Encoding.UTF8, "text/xml")
        };

        request.Headers.Add("SOAPAction", $"http://tempuri.org/{soapAction}");

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

        _logger.LogDebug("RS.GE SOAP Response [{SoapAction}] (HTTP {StatusCode}): {Response}",
            soapAction, (int)response.StatusCode, responseContent);

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
