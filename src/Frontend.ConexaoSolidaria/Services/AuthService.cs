using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Frontend.ConexaoSolidaria.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace Frontend.ConexaoSolidaria.Services;

/// <summary>
/// Owns the JWT lifecycle for the SPA: login, logout, persistence in localStorage,
/// and exposing the decoded claims to Blazor's &lt;AuthorizeView&gt;/&lt;CascadingAuthenticationState&gt;.
/// The API issues the role claim under the long ClaimTypes.Role URI (not the short "role" name) --
/// see AuthController/JwtTokenService in Api.CampanhasUsuarios, which builds the JwtSecurityToken
/// directly from raw Claim objects rather than a ClaimsIdentity, so no outbound claim-type mapping applies.
/// </summary>
public class AuthService : AuthenticationStateProvider
{
    private const string TokenStorageKey = "conexao-solidaria.token";
    private const string RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

    private readonly HttpClient _http;
    private readonly IJSRuntime _js;
    private string? _cachedToken;

    public AuthService(HttpClient http, IJSRuntime js)
    {
        _http = http;
        _js = js;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await GetTokenAsync();
        var identity = string.IsNullOrWhiteSpace(token)
            ? new ClaimsIdentity()
            : ParseClaimsFromJwt(token);

        ApplyAuthorizationHeader(token);
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public async Task<bool> LoginAsync(string email, string senha)
    {
        var response = await _http.PostAsJsonAsync("auth/login", new LoginRequest(email, senha));
        if (!response.IsSuccessStatusCode)
            return false;

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        if (body is null || string.IsNullOrWhiteSpace(body.Token))
            return false;

        await _js.InvokeVoidAsync("localStorage.setItem", TokenStorageKey, body.Token);
        _cachedToken = body.Token;
        ApplyAuthorizationHeader(body.Token);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        return true;
    }

    public async Task LogoutAsync()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", TokenStorageKey);
        _cachedToken = null;
        ApplyAuthorizationHeader(null);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private async Task<string?> GetTokenAsync()
    {
        if (_cachedToken is not null)
            return _cachedToken;

        _cachedToken = await _js.InvokeAsync<string?>("localStorage.getItem", TokenStorageKey);
        return _cachedToken;
    }

    private void ApplyAuthorizationHeader(string? token)
    {
        _http.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(token)
            ? null
            : new AuthenticationHeaderValue("Bearer", token);
    }

    private static ClaimsIdentity ParseClaimsFromJwt(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length != 3)
            return new ClaimsIdentity();

        var payload = ParseBase64WithoutPadding(parts[1]);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payload);
        if (keyValuePairs is null)
            return new ClaimsIdentity();

        var expiraEm = keyValuePairs.TryGetValue("exp", out var expElement) && expElement.TryGetInt64(out var exp)
            ? DateTimeOffset.FromUnixTimeSeconds(exp)
            : (DateTimeOffset?)null;

        if (expiraEm is not null && expiraEm <= DateTimeOffset.UtcNow)
            return new ClaimsIdentity();

        var claims = new List<Claim>();
        foreach (var (key, value) in keyValuePairs)
        {
            var claimType = key == RoleClaimType ? ClaimTypes.Role : key;
            claims.Add(new Claim(claimType, value.ToString()));
        }

        return new ClaimsIdentity(claims, authenticationType: "jwt");
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        var normalized = base64.Replace('-', '+').Replace('_', '/');
        var padded = normalized.PadRight(normalized.Length + (4 - normalized.Length % 4) % 4, '=');
        return Convert.FromBase64String(padded);
    }
}
