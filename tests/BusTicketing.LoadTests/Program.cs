using System.Net.Http.Headers;
using System.Text;

var baseUrl = Environment.GetEnvironmentVariable("LOAD_TEST_URL") ?? "http://localhost:5000";
var token = Environment.GetEnvironmentVariable("LOAD_TEST_TOKEN") ?? string.Empty;
var totalRequests = int.Parse(Environment.GetEnvironmentVariable("LOAD_TEST_REQUESTS") ?? "100");
var concurrency = int.Parse(Environment.GetEnvironmentVariable("LOAD_TEST_CONCURRENCY") ?? "10");

if (string.IsNullOrWhiteSpace(token))
{
    Console.WriteLine("Warning: LOAD_TEST_TOKEN not set. Requests may return 401.");
}

Console.WriteLine($"Load test: {totalRequests} requests, concurrency {concurrency}, target {baseUrl}");

var ticketId = Environment.GetEnvironmentVariable("LOAD_TEST_TICKET_ID") ?? Guid.NewGuid().ToString();
var url = $"{baseUrl}/api/v1/booking/tickets/{ticketId}/print";

using var http = new HttpClient();
if (!string.IsNullOrWhiteSpace(token))
{
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
}

var semaphore = new SemaphoreSlim(concurrency);
var successes = 0;
var failures = 0;
var latencies = new List<long>();
var lockObj = new object();

var tasks = new List<Task>();
for (var i = 0; i < totalRequests; i++)
{
    await semaphore.WaitAsync();
    tasks.Add(Task.Run(async () =>
    {
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await http.GetAsync(url);
            sw.Stop();
            lock (lockObj)
            {
                latencies.Add(sw.ElapsedMilliseconds);
                if (response.IsSuccessStatusCode)
                    successes++;
                else
                    failures++;
            }
        }
        catch
        {
            lock (lockObj) { failures++; }
        }
        finally
        {
            semaphore.Release();
        }
    }));
}

await Task.WhenAll(tasks);

latencies.Sort();
var avg = latencies.Count > 0 ? latencies.Average() : 0;
var p50 = latencies.Count > 0 ? latencies[(int)(latencies.Count * 0.50)] : 0;
var p95 = latencies.Count > 0 ? latencies[(int)(latencies.Count * 0.95)] : 0;
var p99 = latencies.Count > 0 ? latencies[(int)(latencies.Count * 0.99)] : 0;
var min = latencies.Count > 0 ? latencies.Min() : 0;
var max = latencies.Count > 0 ? latencies.Max() : 0;

Console.WriteLine();
Console.WriteLine("=== Load Test Results ===");
Console.WriteLine($"Total Requests:  {totalRequests}");
Console.WriteLine($"Successes:       {successes}");
Console.WriteLine($"Failures:        {failures}");
Console.WriteLine($"Success Rate:    {(double)successes / totalRequests * 100:F1}%");
Console.WriteLine($"Avg Latency:     {avg:F1} ms");
Console.WriteLine($"Min Latency:     {min} ms");
Console.WriteLine($"Max Latency:     {max} ms");
Console.WriteLine($"P50 Latency:     {p50} ms");
Console.WriteLine($"P95 Latency:     {p95} ms");
Console.WriteLine($"P99 Latency:     {p99} ms");

if (failures > 0)
{
    Console.WriteLine();
    Console.WriteLine("WARNING: Some requests failed. Check server logs.");
    Environment.Exit(1);
}
else
{
    Console.WriteLine();
    Console.WriteLine("All requests succeeded.");
}
