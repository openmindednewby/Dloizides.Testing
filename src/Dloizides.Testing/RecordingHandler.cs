namespace Dloizides.Testing;

/// <summary>Wraps a fake server's handler and records each call it carries into the current test's call log.</summary>
public sealed class RecordingHandler : DelegatingHandler
{
    private readonly string from;
    private readonly string to;
    private readonly CallLog log;

    /// <summary>Records calls from one party to another through the fake server's handler.</summary>
    public RecordingHandler(string from, string to, HttpMessageHandler? inner = null, CallLog? log = null)
    {
        this.from = from;
        this.to = to;
        this.log = log ?? CallLog.Shared;
        if (inner is not null)
            InnerHandler = inner;
    }

    /// <summary>Sends the request through the fake server and records it with the response status.</summary>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var scope = TestScope.Resolve();
        var seq = log.Next(scope);
        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        log.Write(new CallLine
        {
            Test = scope.Test,
            Case = scope.Case,
            Seq = seq,
            From = from,
            To = to,
            Method = request.Method.Method,
            Path = request.RequestUri?.AbsolutePath ?? string.Empty,
            Status = (int)response.StatusCode,
        });
        return response;
    }
}
