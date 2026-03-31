using CaloriesTracker.DTOs;
using System.Text.Json;
using System.Net.Http.Headers;
namespace CaloriesTracker.Services
{
    public class FoodApiService : IFoodApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private string _accessToken = string.Empty;

        public FoodApiService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        private async Task<string> GetAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_accessToken)) return _accessToken;

            var clientId = _config["ExternalApi:ClientId"];
            var clientSecret = _config["ExternalApi:ClientSecret"];
            var tokenUrl = "https://oauth.fatsecret.com/connect/token";

            var credentials = $"{clientId}:{clientSecret}";
            var base64Credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(credentials));

            var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);

            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64Credentials);

            var requestBody = new FormUrlEncodedContent(new[]
            {
        new KeyValuePair<string, string>("grant_type", "client_credentials"),
        new KeyValuePair<string, string>("scope", "basic")
    });

            request.Content = requestBody;

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Помилка від FatSecret: {response.StatusCode} - {errorContent}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var tokenData = JsonSerializer.Deserialize<TokenResponse>(jsonResponse);

            _accessToken = tokenData.access_token;
            return _accessToken;
        }

        public async Task<List<ExternalFoodDto>> SearchFoodAsync(string query)
        {
            var token = await GetAccessTokenAsync();

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var encodedQuery = Uri.EscapeDataString(query);
            var searchUrl = $"https://platform.fatsecret.com/rest/foods/search/v1?search_expression={encodedQuery}&format=json";

            var response = await _httpClient.GetAsync(searchUrl);

            if (!response.IsSuccessStatusCode) return new List<ExternalFoodDto>();

            var jsonString = await response.Content.ReadAsStringAsync();

            var resultList = new List<ExternalFoodDto>();

            using JsonDocument doc = JsonDocument.Parse(jsonString);

            if (doc.RootElement.TryGetProperty("foods", out var foodsElement) &&
                foodsElement.TryGetProperty("food", out var foodArray))
            {
                if (foodArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in foodArray.EnumerateArray())
                    {
                        resultList.Add(new ExternalFoodDto
                        {
                            Name = item.GetProperty("food_name").GetString(),
                            Description = item.GetProperty("food_description").GetString()
                        });
                    }
                }
                else if (foodArray.ValueKind == JsonValueKind.Object)
                {
                    resultList.Add(new ExternalFoodDto
                    {
                        Name = foodArray.GetProperty("food_name").GetString(),
                        Description = foodArray.GetProperty("food_description").GetString()
                    });
                }
            }

            return resultList;
        }

    }
}
