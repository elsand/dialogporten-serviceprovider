namespace Digdir.BDB.Dialogporten.ServiceProvider.Clients;

public class ConsoleLoggingMessageHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Log the request body
        if (request.Content != null)
        {
            var requestBody = await request.Content.ReadAsStringAsync();
            Console.WriteLine("Request:");
            Console.WriteLine(requestBody);
        }

        // Send the request to the next handler in the pipeline
        var response = await base.SendAsync(request, cancellationToken);

        // Log the response body
        if (response.Content != null)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Response:");
            Console.WriteLine(responseBody);
        }

        return response;
    }
}
