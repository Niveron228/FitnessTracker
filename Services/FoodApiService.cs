using CaloriesTracker.DTOs;
using System.Text.Json;
using System.Net.Http.Headers;
namespace CaloriesTracker.Services
{
    public class FoodApiService : IFoodApiService
    { // Dependency injection - asp.net giving HttpClient, IConfiguration _config - asp.net giving FoodApiService, _accessToken - just empty string, future token place holder.
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private string _accessToken = string.Empty;
        // DI Constructor
        public FoodApiService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        private async Task<string> GetAccessTokenAsync()
        {
            // if token null or empty - return
            if (!string.IsNullOrEmpty(_accessToken)) return _accessToken;

           // api setting - id,secret and url
            var clientId = _config["ExternalApi:ClientId"];
            var clientSecret = _config["ExternalApi:ClientSecret"];
            var tokenUrl = "https://oauth.fatsecret.com/connect/token";

            // basic auth rule. We cant just send clientId and clientSecret, we should put it together - create credentials and include these two with ':' between them
            // base64Credentials - not hashing or encrypting, just a format way to translate set of symbols into like 'dGVzdDoxMjM=' for safer transfering
            var credentials = $"{clientId}:{clientSecret}";
            var base64Credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(credentials));

            // HttpRequestMessage - like empty envelope that says "Send with POST method to {tokenUrl} address
            var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);

            // including in header translated credentials with clientId and clientSecret - base64Credentials so the fatSecret api let us come in
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64Credentials);

            // like a envelope content - FormUrlEncodedContent is like a way to zip file: Access type(grant_type) = client_credentials
            var requestBody = new FormUrlEncodedContent(new[]
            {
        new KeyValuePair<string, string>("grant_type", "client_credentials"),
        new KeyValuePair<string, string>("scope", "basic")
    });
            // its like we are putting this content(requestBody) into envelope(request) .Content giving a hint that requestBody is a request.Content
            request.Content = requestBody;
            // Sending this "envelope" request to api service
            var response = await _httpClient.SendAsync(request);
            // if status code is not success than its fatsecret error - thor new exception
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"FatSecret Error: {response.StatusCode} - {errorContent}");
            }

            // reading the answer from api
            var jsonResponse = await response.Content.ReadAsStringAsync();
            // saving token into our class that we created with access_token and expires_in - TokenResponse into jsonResponse variable to not create token every time
            var tokenData = JsonSerializer.Deserialize<TokenResponse>(jsonResponse);

            // giving _accessToken our token reference from tokenData
            _accessToken = tokenData.access_token; 
            return _accessToken;
        }

        public async Task<List<ExternalFoodDto>> SearchFoodAsync(string query)
        {
            // setting token using method GetAccesTokenAsync
            var token = await GetAccessTokenAsync();

            // setting request header with "Bearer {token}" so successfully authorize to fatsecret api
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Uri.EscapeDataString - method to replase spaces with '%20' for example: apple pie -> apple%20pie
            var encodedQuery = Uri.EscapeDataString(query);
            var searchUrl = $"https://platform.fatsecret.com/rest/foods/search/v1?search_expression={encodedQuery}&format=json";
            var response = await _httpClient.GetAsync(searchUrl);

            // if status code is not success - return
            if (!response.IsSuccessStatusCode) return new List<ExternalFoodDto>();

            // if not -  loading data to jsonString as a string
            var jsonString = await response.Content.ReadAsStringAsync();

            // creating new list in dto format
            var resultList = new List<ExternalFoodDto>();

            // JsonDocument.Parse takes all text we got from api and creates like a tree with branches(RootElement -> foods -> food), so we can easily transfer between this data using TryGetProperty
            using JsonDocument doc = JsonDocument.Parse(jsonString);

            // try get property so we are looking for a word "foods" in foodsEmenebt and "food" in foodArray
            if (doc.RootElement.TryGetProperty("foods", out var foodsElement) &&
                foodsElement.TryGetProperty("food", out var foodArray))
            {
                // If api found few elements for example we were looking for apple and api service found apple,apple pie,apple juise etc.
                // If there are few elements, api returns an array.If one element,api returns an object.So we are checking - if api giving us an array(ValueKind == Array) - we are adding each element using foreach
                // If its one value-object(ValueKind == object), we are just adding one object
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
                // Here we are adding an object(api gave us one element,not array of few objects)
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
