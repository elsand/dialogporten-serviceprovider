using Microsoft.IdentityModel.Tokens;
using ScottBrady.IdentityModel;
using ScottBrady.IdentityModel.Tokens;

namespace Digdir.BDB.Dialogporten.ServiceProvider.Auth;

public static class DialogTokenServiceCollectionExtension
{
    public static IServiceCollection AddDialogTokenAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication()
            .AddJwtBearer("DialogToken", options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    // Because IssuerSigningKeyResolver does not have a async counterpart, we need
                    // to proxy the keys via a static field. Also, ConfigurationManager does not
                    // support EdDsa, so we need to roll our own refresh/cache of JWKS in the service
                    // below.
                    IssuerSigningKeyResolver = (_, _, _, _) => EdDsaSecurityKeysCacheService.EdDsaSecurityKeys
                };
                options.RequireHttpsMetadata = true;
            });

        return services;
    }
}

public class EdDsaSecurityKeysCacheService : IHostedService, IDisposable
{
    public static List<EdDsaSecurityKey> EdDsaSecurityKeys => _keys;
    private static volatile List<EdDsaSecurityKey> _keys = new();

    private Timer? _timer;
    private readonly IHttpClientFactory _httpClientFactory;

    // In this service we allow keys for all non-production environments for
    // simplicity. Usually one would only allow a single environment (issuer) here
    private readonly List<string> _wellKnownEndpoints =
    [
        "https://localhost:7214/api/v1/.well-known/jwks.json",
        "https://altinn-dev-api.azure-api.net/dialogporten/api/v1/.well-known/jwks.json",
        "https://platform.tt02.altinn.no/dialogporten/api/v1/.well-known/jwks.json"
    ];

    public EdDsaSecurityKeysCacheService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(UpdateCache, null, TimeSpan.Zero, TimeSpan.FromHours(12));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }

    private void UpdateCache(object? state)
    {
        RefreshAsync().Wait();
    }

    private async Task RefreshAsync()
    {
        var httpClient = _httpClientFactory.CreateClient();
        var keys = new List<EdDsaSecurityKey>();

        foreach (var endpoint in _wellKnownEndpoints)
        {
            try
            {
                var response = await httpClient.GetStringAsync(endpoint);
                var jwks = new JsonWebKeySet(response);
                foreach (var jwk in jwks.Keys)
                {
                    if (ExtendedJsonWebKeyConverter.TryConvertToEdDsaSecurityKey(jwk, out var edDsaKey))
                    {
                        keys.Add(edDsaKey);
                    }
                }
            }
            catch
            {
                // ignored
            }
        }

        var newKeys = keys.ToList();
        _keys = newKeys; // Atomic replace
    }
}
