using FocusTrack.Gateway.Api.Authentication;
using FocusTrack.Gateway.Api.Middleware;
using FocusTrack.Gateway.Api.Services;
using Serilog;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Threading.RateLimiting;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

// 1.Logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "FocusTrack.Gateway")
    .CreateLogger();

builder.Host.UseSerilog();

// 2.OpenTelemetry Tracing
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("FocusTrack.Gateway"))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(opt => opt.Endpoint = new Uri(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://jaeger:4317")));

// 3.Rate Limiting – login: 5 attempts per minute per IP
builder.Services.AddSingleton<PartitionedRateLimiter<HttpContext>>(sp =>
{
    return PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        if (!context.Request.Path.StartsWithSegments("/signin-oidc", StringComparison.OrdinalIgnoreCase))
            return RateLimitPartition.GetNoLimiter<string>("default");
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(1),
            PermitLimit = 5,
            QueueLimit = 0
        });
    });
});

// 4.Authentication (BFF: Cookie + OIDC; Postman: Bearer token from Keycloak)
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    // When Postman sends Authorization: Bearer, forward Authenticate to JWT Bearer
    options.ForwardDefaultSelector = context =>
        !string.IsNullOrEmpty(context.Request.Headers.Authorization) ? JwtBearerDefaults.AuthenticationScheme : null;
    options.Cookie.Name = "__Host-FocusTrack-Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
    //API calls must get 401 Unauthorized after logout, not redirect to login
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
})
.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    //Authorization code flow; PKCE is enabled by default in ASP.NET Core for this flow.
    //For /api paths, forward challenge to Cookie so OnRedirectToLogin returns 401 instead of redirect
    options.ForwardDefaultSelector = context =>
        context.Request.Path.StartsWithSegments("/api") ? CookieAuthenticationDefaults.AuthenticationScheme : null;

    //SINGLE source of truth
    options.Authority = builder.Configuration["Oidc:Authority"];

    options.ClientId = builder.Configuration["Oidc:ClientId"];
    options.ClientSecret = builder.Configuration["Oidc:ClientSecret"];
    options.MetadataAddress = $"{builder.Configuration["Oidc:Authority"]}/.well-known/openid-configuration";

    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidIssuers = new[]
        {
            "http://keycloak:8080/realms/focus-track",
            "http://localhost:9080/realms/focus-track"
        },
        ValidateIssuer = true
    };
    options.Events = new OpenIdConnectEvents
    {
        OnRedirectToIdentityProvider = context =>
        {
            context.ProtocolMessage.IssuerAddress = context.ProtocolMessage.IssuerAddress
                .Replace("keycloak:8080", "localhost:9080");
            return Task.CompletedTask;
        },
        //Log auth errors with correlation ID; never expose stack traces to clients
        OnRemoteFailure = context =>
        {
            var correlationId = context.HttpContext.Items["CorrelationId"]?.ToString() ?? context.HttpContext.Response.Headers["X-Correlation-ID"].FirstOrDefault();
            context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>()
                .LogWarning("OIDC remote failure: {Failure}, CorrelationId: {CorrelationId}", context.Failure?.Message, correlationId);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsJsonAsync(new { error = "Authentication failed. Please try again." });
        },
        OnTokenValidated = async context =>
        {
            var userId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return;
            var statusService = context.HttpContext.RequestServices.GetService<IUserStatusService>();
            if (statusService == null) return;
            var status = await statusService.GetStatusAsync(userId, context.HttpContext.RequestAborted);
            if (status == null) return;
            if (string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase)) return;
            context.Fail(new Exception("Account is not active."));
            var reason = string.Equals(status, "Suspended", StringComparison.OrdinalIgnoreCase) ? "suspended" : "deactivated";
            context.Response.Redirect($"/account-disabled?reason={Uri.EscapeDataString(reason)}");
        }
    };
    options.ResponseType = OpenIdConnectResponseType.Code;
    options.Scope.Add("offline_access"); //required for refresh token so we can revoke on logout
    options.SaveTokens = true;

    options.NonceCookie.SameSite = SameSiteMode.Lax;
    options.CorrelationCookie.SameSite = SameSiteMode.Lax;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.Authority = builder.Configuration["Oidc:Authority"];
    options.Audience = builder.Configuration["Oidc:Audience"] ?? "account";
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidIssuers = new[]
        {
            "http://keycloak:8080/realms/focus-track",
            "http://localhost:9080/realms/focus-track"
        },
        ValidateIssuer = true
    };
});

// 5.Reverse Proxy with Cookie-to-Bearer Transformation
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("Yarp"))
    .AddTransforms(builderContext =>
    {
        builderContext.AddRequestTransform(async transformContext =>
        {
            // 1.Extract User ID from Claims
            var userId = transformContext.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                transformContext.ProxyRequest.Headers.Add("X-User-Id", userId);
            }

            // 2.Forward roles so backends can enforce Admin on /admin/* routes
            var roles = transformContext.HttpContext.User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
            if (roles.Count > 0)
            {
                transformContext.ProxyRequest.Headers.Add("X-User-Roles", string.Join(",", roles));
            }

            // 3.Attach token to backend: use incoming Bearer (Postman) or cookie token (browser, with silent refresh)
            var authHeader = transformContext.HttpContext.Request.Headers.Authorization.FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                transformContext.ProxyRequest.Headers.Authorization = System.Net.Http.Headers.AuthenticationHeaderValue.Parse(authHeader);
            }
            else
            {
                var tokenRefresh = transformContext.HttpContext.RequestServices.GetService<IOidcTokenRefreshService>();
                var accessToken = tokenRefresh != null
                    ? await tokenRefresh.GetValidAccessTokenAsync(transformContext.HttpContext, transformContext.HttpContext.RequestAborted)
                    : await transformContext.HttpContext.GetTokenAsync("access_token");
                if (!string.IsNullOrEmpty(accessToken))
                    transformContext.ProxyRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            }
        });
    });

builder.Services.AddHttpClient<IOidcRevokeService, OidcRevokeService>(); //revoke refresh token with OIDC
builder.Services.AddHttpClient(); // for OidcTokenRefreshService
builder.Services.AddScoped<IOidcTokenRefreshService, OidcTokenRefreshService>(); //silent token refresh
builder.Services.Configure<SessionApiOptions>(builder.Configuration.GetSection(SessionApiOptions.SectionName));
builder.Services.AddHttpClient<IUserStatusService, UserStatusService>((sp, client) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SessionApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseAddress);
    client.DefaultRequestHeaders.Add("X-Internal-Api-Key", options.InternalApiKey);
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IClaimsTransformation, KeycloakRolesClaimsTransformation>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAuth", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

builder.Services.AddHealthChecks();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<LoginRateLimitMiddleware>();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/healthz");

// Root: requires login (redirects to Keycloak); after login redirects to sessions list so browser flow works
app.MapGet("/", () => Results.Redirect("/api/sessions?page=1&pageSize=20")).RequireAuthorization();

app.MapReverseProxy();

//Secure Logout: revoke refresh token with OIDC, clear all client-side tokens/cookies; subsequent protected calls return 401
//When called with Bearer token (e.g. Postman), we cannot sign out server-side; client discards the token. Return 204.
app.MapPost("/api/auth/logout", async (HttpContext context) =>
{
    var refreshToken = await GetStoredRefreshTokenAsync(context);
    var revoke = context.RequestServices.GetService<IOidcRevokeService>();
    if (revoke != null)
        await revoke.RevokeRefreshTokenAsync(refreshToken, context.RequestAborted);
    var isBearer = context.Request.Headers.Authorization.Any(h => h?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true);
    if (!isBearer)
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
    }
    return Results.NoContent();
});
app.MapGet("/api/auth/logout", async (HttpContext context) =>
{
    var refreshToken = await GetStoredRefreshTokenAsync(context);
    var revoke = context.RequestServices.GetService<IOidcRevokeService>();
    if (revoke != null)
        await revoke.RevokeRefreshTokenAsync(refreshToken, context.RequestAborted);
    var isBearer = context.Request.Headers.Authorization.Any(h => h?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true);
    if (!isBearer)
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
    }
    return Results.Redirect("/");
});

static async Task<string?> GetStoredRefreshTokenAsync(HttpContext context)
{
    var result = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return result.Properties?.GetTokenValue("refresh_token");
}

//Clear error page when account is Suspended/Deactivated
app.MapGet("/account-disabled", (HttpContext context) =>
{
    var reason = context.Request.Query["reason"].FirstOrDefault() ?? "disabled";
    var message = reason == "suspended"
        ? "Your account has been suspended."
        : reason == "deactivated"
            ? "Your account is deactivated."
            : "Your account is not active.";
    return Results.Content(
        $"<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>Account disabled</title></head><body><h1>Account disabled</h1><p>{System.Net.WebUtility.HtmlEncode(message)}</p><p><a href=\"/api/auth/logout\">Sign out</a> (clears session and returns to home)</p></body></html>",
        "text/html");
}).AllowAnonymous();

app.Run();