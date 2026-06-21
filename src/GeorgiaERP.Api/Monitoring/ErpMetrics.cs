using Prometheus;

namespace GeorgiaERP.Api.Monitoring;

/// <summary>
/// Central registry of Prometheus metrics for the Georgia ERP Platform.
/// All custom counters, histograms, and gauges are defined here so they can be
/// referenced from middleware, controllers, and background services.
/// </summary>
public static class ErpMetrics
{
    // ── HTTP Request Metrics ──────────────────────────────────────────

    /// <summary>
    /// Histogram of HTTP request durations in seconds, labeled by method,
    /// endpoint, and status code. Default buckets cover typical API latency.
    /// </summary>
    public static readonly Histogram HttpRequestDuration = Metrics.CreateHistogram(
        "erp_http_request_duration_seconds",
        "Duration of HTTP requests in seconds.",
        new HistogramConfiguration
        {
            LabelNames = ["method", "endpoint", "status_code"],
            Buckets = [0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1.0, 2.0, 5.0, 10.0]
        });

    /// <summary>Total count of HTTP requests, labeled by method, endpoint, and status code.</summary>
    public static readonly Counter HttpRequestTotal = Metrics.CreateCounter(
        "erp_http_requests_total",
        "Total number of HTTP requests.",
        new CounterConfiguration
        {
            LabelNames = ["method", "endpoint", "status_code"]
        });

    // ── POS Session Metrics ───────────────────────────────────────────

    /// <summary>Number of currently active POS sessions.</summary>
    public static readonly Gauge ActivePosSessions = Metrics.CreateGauge(
        "erp_pos_active_sessions",
        "Number of currently active POS sessions.");

    /// <summary>Total POS transactions processed, labeled by type (sale, return, void).</summary>
    public static readonly Counter PosTransactionsTotal = Metrics.CreateCounter(
        "erp_pos_transactions_total",
        "Total POS transactions processed.",
        new CounterConfiguration
        {
            LabelNames = ["transaction_type"]
        });

    /// <summary>Histogram of POS transaction values in GEL.</summary>
    public static readonly Histogram PosTransactionValue = Metrics.CreateHistogram(
        "erp_pos_transaction_value_gel",
        "POS transaction value in GEL.",
        new HistogramConfiguration
        {
            Buckets = [1, 5, 10, 25, 50, 100, 250, 500, 1000, 5000, 10000]
        });

    // ── RS.GE Compliance Metrics ──────────────────────────────────────

    /// <summary>
    /// Counter for RS.GE submissions, labeled by operation (submit_waybill,
    /// confirm_waybill, save_invoice, etc.) and result (success, transient, permanent).
    /// </summary>
    public static readonly Counter RsGeSubmissions = Metrics.CreateCounter(
        "erp_rsge_submissions_total",
        "Total RS.GE fiscal submissions.",
        new CounterConfiguration
        {
            LabelNames = ["operation", "result"]
        });

    /// <summary>Histogram of RS.GE SOAP call durations in seconds.</summary>
    public static readonly Histogram RsGeSoapDuration = Metrics.CreateHistogram(
        "erp_rsge_soap_duration_seconds",
        "Duration of RS.GE SOAP calls in seconds.",
        new HistogramConfiguration
        {
            LabelNames = ["operation"],
            Buckets = [0.1, 0.25, 0.5, 1.0, 2.0, 5.0, 10.0, 30.0]
        });

    /// <summary>Number of retries for RS.GE submissions.</summary>
    public static readonly Counter RsGeRetries = Metrics.CreateCounter(
        "erp_rsge_retries_total",
        "Total RS.GE submission retries.",
        new CounterConfiguration
        {
            LabelNames = ["operation"]
        });

    // ── Queue Metrics ─────────────────────────────────────────────────

    /// <summary>Current depth of the RS.GE submission queue.</summary>
    public static readonly Gauge RsGeQueueDepth = Metrics.CreateGauge(
        "erp_rsge_queue_depth",
        "Current number of pending messages in the RS.GE submission queue.");

    /// <summary>Current depth of the RS.GE dead-letter queue.</summary>
    public static readonly Gauge RsGeDeadLetterQueueDepth = Metrics.CreateGauge(
        "erp_rsge_deadletter_queue_depth",
        "Current number of messages in the RS.GE dead-letter queue.");

    // ── Database Metrics ──────────────────────────────────────────────

    /// <summary>Histogram of database query durations in seconds.</summary>
    public static readonly Histogram DatabaseQueryDuration = Metrics.CreateHistogram(
        "erp_database_query_duration_seconds",
        "Duration of database queries in seconds.",
        new HistogramConfiguration
        {
            LabelNames = ["operation"],
            Buckets = [0.001, 0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1.0, 5.0]
        });

    /// <summary>Number of active database connections in the pool.</summary>
    public static readonly Gauge DatabaseActiveConnections = Metrics.CreateGauge(
        "erp_database_active_connections",
        "Number of active database connections.");
}
