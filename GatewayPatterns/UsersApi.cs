
    sealed class UsersApi
    {
        private readonly HttpClient _http;
        public UsersApi(HttpClient http) => _http = http;

        public Task<HttpResponseMessage> PingAsync(string msg, CancellationToken ct)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "/ping");
            req.Headers.Remove("X-Echo");
            req.Headers.TryAddWithoutValidation("X-Echo", msg);
            return _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        }
    }


