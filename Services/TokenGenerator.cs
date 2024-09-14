using System.Net.Http.Headers;
using System.Text;

namespace Digdir.BDB.Dialogporten.ServiceProvider.Services;

public interface ITokenGenerator
{
    Task<string> GetToken();
}
public class TokenGenerator : ITokenGenerator
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _client;

    private const string TokenGeneratorUrl = "https://altinn-testtools-token-generator.azurewebsites.net/api/GetEnterpriseToken";
    private const int TokenTtlSeconds = 300;

    public TokenGenerator(IHttpClientFactory clientFactory, IConfiguration configuration)
    {
        _configuration = configuration;
        _client = clientFactory.CreateClient(nameof(TokenGenerator));
    }

    public async Task<string> GetToken()
    {
        var byteArray = Encoding.ASCII.GetBytes($"{_configuration["TokenGenerator:Username"]}:{_configuration["TokenGenerator:Password"]}");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
        var response = await _client.GetAsync(string.Format(
            "{0}?env={1}&scopes={2}&orgNo={3}&ttl={4}",
            TokenGeneratorUrl,
            _configuration["TokenGenerator:Environment"],
            _configuration["TokenGenerator:Scopes"],
            "991825827",
            TokenTtlSeconds));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}
