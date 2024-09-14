using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Digdir.BDB.Dialogporten.ServiceProvider.Auth;

public static class IdPortenAuthentication
{
    public static IServiceCollection AddIdportenAuthentication(this IServiceCollection services,
        IConfiguration configuration)
    {

        var authenticationBuilder = services.AddAuthentication(options =>
        {
          options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
          options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        });

        authenticationBuilder
         .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
         {
             options.Cookie.Name = "session";
             options.Cookie.SameSite = SameSiteMode.None;
             options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
             options.Cookie.IsEssential = true;
         })
         .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
         {
            options.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;
            options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.GetClaimsFromUserInfoEndpoint = true;
            options.AuthenticationMethod = OpenIdConnectRedirectBehavior.RedirectGet;
            options.ResponseMode = OpenIdConnectResponseMode.FormPost;
            options.Authority = "https://test.idporten.no/";
            options.ClientId = configuration["Idporten:ClientId"];
            options.ClientSecret = configuration["Idporten:Secret"];
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.UsePkce = true;
            options.CallbackPath = configuration["Idporten:CallbackPath"];
            options.SaveTokens = true;
            options.GetClaimsFromUserInfoEndpoint = false;
            options.Scope.Add("openid");
            options.Scope.Add("profile");

            options.Events = new OpenIdConnectEvents
            {
              OnRedirectToIdentityProviderForSignOut = context =>
              {
                  context.Response.Redirect(configuration["Idporten:RedirectOnSignOut"]!);
                  context.HandleResponse();

                  return Task.CompletedTask;
              },

              OnRemoteFailure = context =>
              {
                 context.Response.Redirect("/error");
                 context.HandleResponse();
                 return Task.FromResult(0);
              },
           };
         });

        return services;
    }
}
