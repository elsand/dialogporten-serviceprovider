using System.Net.Http.Headers;
using Digdir.BDB.Dialogporten.ServiceProvider.Services;

namespace Digdir.BDB.Dialogporten.ServiceProvider.Clients;

public class TokenGeneratorMessageHandler : HttpClientHandler
{
    private readonly ITokenGenerator _tokenGenerator;

    public TokenGeneratorMessageHandler(ITokenGenerator tokenGenerator)
    {
        _tokenGenerator = tokenGenerator;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokenGenerator.GetToken();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }
}
