namespace GatewayPatterns.SrvApi
{
    public class AuthApi
    {
        private readonly HttpClient _http;
        public AuthApi(HttpClient http) => _http = http;

        public Task<HttpResponseMessage> RegisterAsync(RegisterRequest dto, CancellationToken ct) =>
            _http.PostAsJsonAsync("/api/auth/register", dto, ct);

        public Task<HttpResponseMessage> LoginAsync(LoginRequest dto, CancellationToken ct) =>
            _http.PostAsJsonAsync("/api/auth/login", dto, ct);
    }
}
