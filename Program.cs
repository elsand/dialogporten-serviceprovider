using System.Text.Json;
using System.Text.Json.Serialization;
using Digdir.BDB.Dialogporten.ServiceProvider.Auth;
using Digdir.BDB.Dialogporten.ServiceProvider.Clients;
using Digdir.BDB.Dialogporten.ServiceProvider.Services;
using Refit;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddUserSecrets<Program>();

builder.Services.AddControllers();

builder.Services
    .AddEndpointsApiExplorer()
    .AddSwaggerGen()
    .AddIdportenAuthentication(builder.Configuration)
    .AddDialogTokenAuthentication()
    .AddTransient<TokenGeneratorMessageHandler>()
    .AddTransient<ConsoleLoggingMessageHandler>()
    .AddSingleton<ITokenGenerator, TokenGenerator>()
    .AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>()
    .AddHostedService<EdDsaSecurityKeysCacheService>()
    .AddHostedService<QueuedHostedService>()
    .AddCors(options =>
    {
        options.AddPolicy("AllowedOriginsPolicy", builder =>
        {
            // This is to ease development (ie. various locahost ports)
            // In a production setting, this should be restricted to https://af.altinn.no
            builder.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod()
                .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
        });
    })
    .AddRefitClient<IDialogporten>(_ => new RefitSettings
    {
        ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        })
    })
    .ConfigureHttpClient(configuration =>
    {
        configuration.BaseAddress = new Uri(builder.Configuration["Dialogporten:BaseUrl"]!);
    })
    .AddHttpMessageHandler<ConsoleLoggingMessageHandler>()
    .ConfigurePrimaryHttpMessageHandler<TokenGeneratorMessageHandler>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowedOriginsPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();
