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

    private DateTimeOffset _tokenExpiresAt = DateTimeOffset.MinValue;
    private string _cachedToken = string.Empty;

    public TokenGenerator(IHttpClientFactory clientFactory, IConfiguration configuration)
    {
        _configuration = configuration;
        _client = clientFactory.CreateClient(nameof(TokenGenerator));

        var byteArray = Encoding.ASCII.GetBytes($"{_configuration["TokenGenerator:Username"]}:{_configuration["TokenGenerator:Password"]}");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
    }

    public async Task<string> GetToken()
    {
        if (!string.IsNullOrEmpty(_cachedToken) && DateTimeOffset.UtcNow < _tokenExpiresAt)
        {
            return _cachedToken;
        }

        var response = await _client.GetAsync(string.Format(
            "{0}?env={1}&scopes={2}&orgNo={3}&ttl={4}",
            TokenGeneratorUrl,
            _configuration["TokenGenerator:Environment"],
            _configuration["TokenGenerator:Scopes"],
            "991825827", // TODO! This should probably be possible to pass as a global query parameters
            TokenTtlSeconds));

        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadAsStringAsync();
        _cachedToken = token;
        _tokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(TokenTtlSeconds);

        return _cachedToken;
    }
}
