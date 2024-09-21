namespace FactoryServerApi.Http;

//internal class HttpLogger : IHttpClientAsyncLogger
//{
//    private readonly ILogger<HttpLogger> _logger;

//    public HttpLogger(ILogger<HttpLogger> logger)
//    {
//        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//    }

//    public async ValueTask<object?> LogRequestStartAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
//    {
//        var method = request.Method;
//        var requestUri = request.RequestUri;
//        var headers = string.Join("\n", request.Headers.Select(header => $"{header.Key}: {string.Join(", ", header.Value)}"));
//        var content = request.Content != null ? await request.Content.ReadAsStringAsync(cancellationToken) : "None";

//        _logger.LogInformation("""
//                       ----- HTTP Request -----
//                       Method: {Method}
//                       Request URI: {RequestUri}
//                       Headers: {Headers}
//                       Request Content: {Content}
//                       ------------------------
//                       """, method, requestUri, headers, content);

//        return null;
//    }

//    public async ValueTask LogRequestStopAsync(object? context, HttpRequestMessage request, HttpResponseMessage response, TimeSpan elapsed, CancellationToken cancellationToken = default)
//    {
//        var statusCodeInt = (int)response.StatusCode;
//        var statusCodeString = response.StatusCode.ToString();
//        var elapsedMilliseconds = elapsed.TotalMilliseconds.ToString("F1");
//        var headers = string.Join("\n", response.Headers.Select(header => $"{header.Key}: {string.Join(", ", header.Value)}"));
//        var content = response.Content != null ? await response.Content.ReadAsStringAsync(cancellationToken) : "None";

//        var logTemplate = """
//                   ----- HTTP Response -----
//                   Status Code: {StatusCodeInt} {StatusCodeString}
//                   Elapsed Time: {ElapsedMilliseconds}ms
//                   Headers: {Headers}
//                   Response Content: {Content}
//                   -------------------------
//                   """;

//        _logger.LogInformation(logTemplate, statusCodeInt, statusCodeString, elapsedMilliseconds, headers, content);
//    }

//    public ValueTask LogRequestFailedAsync(object? context, HttpRequestMessage request, HttpResponseMessage? response, Exception exception, TimeSpan elapsed, CancellationToken cancellationToken = default)
//    {
//        var host = request.RequestUri?.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped);
//        var path = request.RequestUri!.PathAndQuery;
//        var elapsedMilliseconds = elapsed.TotalMilliseconds.ToString("F1");

//        var logTemplate = "Request towards '{Host}{Path}' failed after {ElapsedMilliseconds}ms";

//        _logger.LogError(exception, logTemplate, host, path, elapsedMilliseconds);

//        return ValueTask.CompletedTask;
//    }

//    public object? LogRequestStart(HttpRequestMessage request)
//    {
//        var task = LogRequestStartAsync(request);
//        return task.IsCompleted
//            ? task.Result
//            : task.AsTask().Result;
//    }

//    public void LogRequestStop(object? context, HttpRequestMessage request, HttpResponseMessage response, TimeSpan elapsed)
//    {
//        var task = LogRequestStopAsync(context, request, response, elapsed);
//        if (task.IsCompleted)
//            task.GetAwaiter().GetResult();
//        else
//            task.AsTask().GetAwaiter().GetResult();
//    }

//    public void LogRequestFailed(object? context, HttpRequestMessage request, HttpResponseMessage? response, Exception exception, TimeSpan elapsed)
//    {
//        var task = LogRequestFailedAsync(context, request, response, exception, elapsed);
//        if (task.IsCompleted)
//            task.GetAwaiter().GetResult();
//        else
//            task.AsTask().GetAwaiter().GetResult();
//    }

//}