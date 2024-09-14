namespace Digdir.BDB.Dialogporten.ServiceProvider.Clients;

public class ConsoleLoggingMessageHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content != null)
        {
            var requestBody = await request.Content.ReadAsStringAsync();
            Console.WriteLine("Request:");
            Console.WriteLine(requestBody);
        }

        var response = await base.SendAsync(request, cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine("Response:");
        Console.WriteLine(responseBody);

        return response;
    }
}
