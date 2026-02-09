using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using DollarConverterApp.Models;

namespace DollarConverterApp.Services
{
    public class CurrencyService
    {
        private const string ApiUrl = "https://awesomeapi.com.br/last/USD-BRL";
        private readonly HttpClient _httpClient;

        public CurrencyService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<CurrencyRate> GetCurrentRateAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(ApiUrl);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = JsonSerializer.Deserialize<CurrencyApiResponse>(json, options);

                return data?.USDBRL;
            }
            catch (Exception ex)
            {
                // In a real app, we should log this
                Console.WriteLine($"Error fetching currency rate: {ex.Message}");
                return null;
            }
        }
    }
}
