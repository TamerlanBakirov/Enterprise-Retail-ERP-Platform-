using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace GeorgiaERP.Tests.Integration;

[Collection("Integration")]
public class FinanceApiTests : IntegrationTestBase
{
    public FinanceApiTests(ErpApiFactory factory) : base(factory) { }

    private Task<HttpClient> AuthenticatedClient()
        => AuthenticatedClient("fin_admin", "finadmin@test.local", "Fin", "Admin", "ფინანსები");

    // === Auth ===

    [Fact]
    public async Task Finance_WithoutAuth_Returns401()
    {
        var response = await NewClient().GetAsync("/api/v1/finance/chart-of-accounts");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // === Chart of Accounts ===

    [Fact]
    public async Task ChartOfAccounts_CreateAndList()
    {
        var client = await AuthenticatedClient();
        var code = $"ACCT-{Guid.NewGuid():N}"[..15];

        var createResponse = await client.PostAsJsonAsync("/api/v1/finance/chart-of-accounts", new
        {
            accountCode = code,
            name = "Test Account",
            nameKa = "ტესტი ანგარიში",
            accountType = "Asset",
            balanceType = "Debit",
            parentId = (Guid?)null,
            isHeader = false
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        created.GetProperty("id").GetString().Should().NotBeNullOrEmpty();

        var listResponse = await client.GetAsync("/api/v1/finance/chart-of-accounts?isActive=true");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        list.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task ChartOfAccounts_DuplicateCode_ReturnsConflict()
    {
        var client = await AuthenticatedClient();
        var code = $"DUP-{Guid.NewGuid():N}"[..15];

        await client.PostAsJsonAsync("/api/v1/finance/chart-of-accounts", new
        {
            accountCode = code,
            name = "First Account",
            accountType = "Asset",
            balanceType = "Debit",
            isHeader = false
        });

        var response = await client.PostAsJsonAsync("/api/v1/finance/chart-of-accounts", new
        {
            accountCode = code,
            name = "Second Account",
            accountType = "Liability",
            balanceType = "Credit",
            isHeader = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // === Journal Entries ===

    [Fact]
    public async Task JournalEntries_CreateBalanced_ReturnsCreated()
    {
        var client = await AuthenticatedClient();

        // Create two accounts for journal lines
        var code1 = $"JE1-{Guid.NewGuid():N}"[..15];
        var code2 = $"JE2-{Guid.NewGuid():N}"[..15];

        var acct1Response = await client.PostAsJsonAsync("/api/v1/finance/chart-of-accounts", new
        {
            accountCode = code1,
            name = "Debit Account",
            accountType = "Asset",
            balanceType = "Debit",
            isHeader = false
        });
        acct1Response.StatusCode.Should().Be(HttpStatusCode.Created);
        var acct1 = await acct1Response.Content.ReadFromJsonAsync<JsonElement>();
        var accountId1 = Guid.Parse(acct1.GetProperty("id").GetString()!);

        var acct2Response = await client.PostAsJsonAsync("/api/v1/finance/chart-of-accounts", new
        {
            accountCode = code2,
            name = "Credit Account",
            accountType = "Liability",
            balanceType = "Credit",
            isHeader = false
        });
        acct2Response.StatusCode.Should().Be(HttpStatusCode.Created);
        var acct2 = await acct2Response.Content.ReadFromJsonAsync<JsonElement>();
        var accountId2 = Guid.Parse(acct2.GetProperty("id").GetString()!);

        var response = await client.PostAsJsonAsync("/api/v1/finance/journal-entries", new
        {
            entryDate = DateTimeOffset.UtcNow,
            description = "Test balanced entry",
            sourceType = (string?)null,
            sourceId = (Guid?)null,
            createdBy = Guid.NewGuid(),
            lines = new[]
            {
                new { accountId = accountId1, debitAmount = 1000m, creditAmount = 0m, description = "Debit line" },
                new { accountId = accountId2, debitAmount = 0m, creditAmount = 1000m, description = "Credit line" }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("entryNumber").GetString().Should().StartWith("JE-");
        body.GetProperty("totalDebit").GetDecimal().Should().Be(1000m);
        body.GetProperty("totalCredit").GetDecimal().Should().Be(1000m);
    }

    [Fact]
    public async Task JournalEntries_Unbalanced_ReturnsBadRequest()
    {
        var client = await AuthenticatedClient();

        var code1 = $"UB1-{Guid.NewGuid():N}"[..15];
        var code2 = $"UB2-{Guid.NewGuid():N}"[..15];

        var acct1Response = await client.PostAsJsonAsync("/api/v1/finance/chart-of-accounts", new
        {
            accountCode = code1,
            name = "Unbal Debit",
            accountType = "Asset",
            balanceType = "Debit",
            isHeader = false
        });
        var acct1 = await acct1Response.Content.ReadFromJsonAsync<JsonElement>();
        var accountId1 = Guid.Parse(acct1.GetProperty("id").GetString()!);

        var acct2Response = await client.PostAsJsonAsync("/api/v1/finance/chart-of-accounts", new
        {
            accountCode = code2,
            name = "Unbal Credit",
            accountType = "Liability",
            balanceType = "Credit",
            isHeader = false
        });
        var acct2 = await acct2Response.Content.ReadFromJsonAsync<JsonElement>();
        var accountId2 = Guid.Parse(acct2.GetProperty("id").GetString()!);

        var response = await client.PostAsJsonAsync("/api/v1/finance/journal-entries", new
        {
            entryDate = DateTimeOffset.UtcNow,
            description = "Unbalanced entry",
            createdBy = Guid.NewGuid(),
            lines = new[]
            {
                new { accountId = accountId1, debitAmount = 500m, creditAmount = 0m },
                new { accountId = accountId2, debitAmount = 0m, creditAmount = 300m }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task JournalEntries_PostEntry_ReturnsOk()
    {
        var client = await AuthenticatedClient();

        var code1 = $"PE1-{Guid.NewGuid():N}"[..15];
        var code2 = $"PE2-{Guid.NewGuid():N}"[..15];

        var acct1Response = await client.PostAsJsonAsync("/api/v1/finance/chart-of-accounts", new
        {
            accountCode = code1,
            name = "Post Debit",
            accountType = "Expense",
            balanceType = "Debit",
            isHeader = false
        });
        var acct1 = await acct1Response.Content.ReadFromJsonAsync<JsonElement>();
        var accountId1 = Guid.Parse(acct1.GetProperty("id").GetString()!);

        var acct2Response = await client.PostAsJsonAsync("/api/v1/finance/chart-of-accounts", new
        {
            accountCode = code2,
            name = "Post Credit",
            accountType = "Revenue",
            balanceType = "Credit",
            isHeader = false
        });
        var acct2 = await acct2Response.Content.ReadFromJsonAsync<JsonElement>();
        var accountId2 = Guid.Parse(acct2.GetProperty("id").GetString()!);

        var createResponse = await client.PostAsJsonAsync("/api/v1/finance/journal-entries", new
        {
            entryDate = DateTimeOffset.UtcNow,
            description = "Entry to post",
            createdBy = Guid.NewGuid(),
            lines = new[]
            {
                new { accountId = accountId1, debitAmount = 250m, creditAmount = 0m },
                new { accountId = accountId2, debitAmount = 0m, creditAmount = 250m }
            }
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var entryId = created.GetProperty("id").GetString()!;

        var postResponse = await client.PostAsync($"/api/v1/finance/journal-entries/{entryId}/post", null);
        postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task JournalEntries_ListPaged_ReturnsOk()
    {
        var client = await AuthenticatedClient();

        var response = await client.GetAsync("/api/v1/finance/journal-entries?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // === Bank Accounts ===

    [Fact]
    public async Task BankAccounts_CreateAndList()
    {
        var client = await AuthenticatedClient();
        var accountNumber = $"BA-{Guid.NewGuid():N}"[..20];

        var createResponse = await client.PostAsJsonAsync("/api/v1/finance/bank-accounts", new
        {
            accountName = "Test Bank Account",
            bankName = "Bank of Georgia",
            accountNumber,
            iban = "GE29NB0000000101904917",
            currency = "GEL",
            glAccountId = (Guid?)null
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        created.GetProperty("id").GetString().Should().NotBeNullOrEmpty();

        var listResponse = await client.GetAsync("/api/v1/finance/bank-accounts");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        list.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task BankAccounts_DuplicateNumber_ReturnsConflict()
    {
        var client = await AuthenticatedClient();
        var accountNumber = $"DBN-{Guid.NewGuid():N}"[..20];

        await client.PostAsJsonAsync("/api/v1/finance/bank-accounts", new
        {
            accountName = "First Bank Account",
            bankName = "TBC Bank",
            accountNumber,
            currency = "GEL"
        });

        var response = await client.PostAsJsonAsync("/api/v1/finance/bank-accounts", new
        {
            accountName = "Second Bank Account",
            bankName = "Liberty Bank",
            accountNumber,
            currency = "USD"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
