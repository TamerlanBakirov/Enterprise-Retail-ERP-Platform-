using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace GeorgiaERP.Tests.Integration;

[Collection("Integration")]
public class AdminJobsApiTests : IntegrationTestBase
{
    public AdminJobsApiTests(ErpApiFactory factory) : base(factory) { }

    [Fact]
    public async Task GetJobs_Authenticated_ReturnsJobList()
    {
        var client = await AuthenticatedClient("admin-jobs-user", "admin-jobs@test.com");

        var response = await client.GetAsync("/api/v1/admin/jobs");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().Be(JsonValueKind.Array);

        // Should have at least the 2 background services registered
        body.GetArrayLength().Should().BeGreaterThanOrEqualTo(2);

        // Check structure of first job
        var firstJob = body[0];
        firstJob.GetProperty("jobName").GetString().Should().NotBeNullOrEmpty();
        firstJob.GetProperty("description").GetString().Should().NotBeNullOrEmpty();
        firstJob.GetProperty("intervalMinutes").GetDouble().Should().BeGreaterThan(0);
        firstJob.GetProperty("state").GetString().Should().NotBeNullOrEmpty();
        firstJob.GetProperty("totalRuns").GetInt64().Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetJobs_Unauthenticated_Returns401()
    {
        var response = await NewClient().GetAsync("/api/v1/admin/jobs");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetJobByName_Existing_ReturnsJobStatus()
    {
        var client = await AuthenticatedClient("admin-jobs-detail-user", "admin-jobs-detail@test.com");

        // Get the job list first to find a valid job name
        var listResponse = await client.GetAsync("/api/v1/admin/jobs");
        var jobs = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var jobName = jobs[0].GetProperty("jobName").GetString()!;

        var response = await client.GetAsync($"/api/v1/admin/jobs/{jobName}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var job = await response.Content.ReadFromJsonAsync<JsonElement>();
        job.GetProperty("jobName").GetString().Should().Be(jobName);
    }

    [Fact]
    public async Task GetJobByName_NonExistent_Returns404()
    {
        var client = await AuthenticatedClient("admin-jobs-404-user", "admin-jobs-404@test.com");

        var response = await client.GetAsync("/api/v1/admin/jobs/NonExistentJob");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetJobs_ContainsKnownServices()
    {
        var client = await AuthenticatedClient("admin-jobs-known-user", "admin-jobs-known@test.com");

        var response = await client.GetAsync("/api/v1/admin/jobs");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        var jobNames = Enumerable.Range(0, body.GetArrayLength())
            .Select(i => body[i].GetProperty("jobName").GetString()!)
            .ToList();

        // These services should be registered by the infrastructure layer
        jobNames.Should().Contain("LowStockAlert");
        jobNames.Should().Contain("ExpiredRefreshTokenCleanup");
    }
}
