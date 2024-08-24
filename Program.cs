using Digdir.BDB.Dialogporten.ServiceProvider.Auth;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddUserSecrets<Program>();

builder.Services.AddControllers();

builder.Services
    .AddEndpointsApiExplorer()
    .AddSwaggerGen()
    .AddHttpClient()
    .AddIdportenAuthentication(builder.Configuration)
    .AddDialogTokenAuthentication()
    .AddHostedService<EdDsaSecurityKeysCacheService>()
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
    });

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
