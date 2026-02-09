using System.Net.Http;
using System.Text.Json;
using DollarConverterApp.Models;

namespace DollarConverterApp.Services
{
    public class CurrencyService
    {
        // API Comercial (Sua atual)
        private const string AwesomeApiUrl = "https://awesomeapi.com.br/last/USD-BRL";
        
        // API Banco Central (PTAX Venda - Série 1)
        // ultimos/1 garante que pegamos o último fechamento, mesmo se for feriado
        private const string BcbApiUrl = "https://api.bcb.gov.br/dados/serie/1/dados/ultimos/1?formato=json";
        
        private readonly HttpClient _httpClient;

        public CurrencyService()
        {
            _httpClient = new HttpClient();
            // O BCB as vezes exige User-Agent para não bloquear a requisição
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "DollarConverterApp/1.0");
        }

        // Método Original (Mantido)
        public async Task<CurrencyRate> GetCurrentRateAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(AwesomeApiUrl);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = JsonSerializer.Deserialize<CurrencyApiResponse>(json, options);

                return data?.USDBRL;
            }
            catch 
            {
                return null;
            }
        }

        // NOVO: Método para buscar do Banco Central
        public async Task<decimal?> GetOfficialBcbRateAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(BcbApiUrl);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                
                // O BCB retorna uma LISTA de objetos, não um objeto único
                var data = JsonSerializer.Deserialize<List<BcbRate>>(json);

                if (data != null && data.Count > 0)
                {
                    // O valor vem como string "5.23" ou "5,23", precisamos tratar com cuidado
                    if (decimal.TryParse(data[0].Valor, 
                        System.Globalization.NumberStyles.Any, 
                        new System.Globalization.CultureInfo("pt-BR"), // BCB usa vírgula geralmente
                        out decimal rate))
                    {
                        return rate;
                    }
                }
                return null;
            }
            catch
            {
                // Em produção, logaríamos o erro aqui
                return null;
            }
        }
    }
}