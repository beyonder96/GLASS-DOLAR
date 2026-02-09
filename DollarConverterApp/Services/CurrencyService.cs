using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using DollarConverterApp.Models;

namespace DollarConverterApp.Services
{
    public class CurrencyService
    {
        private const string RealTimeApiUrl = "https://awesomeapi.com.br/last/USD-BRL";
        private const string BcbBaseUrl = "https://api.bcb.gov.br/dados/serie/1/dados";
        
        private readonly HttpClient _httpClient;

        public CurrencyService()
        {
            // CONFIGURAÇÃO DE SEGURANÇA (SSL BYPASS)
            // Isso evita erros de "certificado inválido" comuns em desenvolvimento
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        public async Task<decimal?> GetRealTimeRateAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(RealTimeApiUrl);
                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = JsonSerializer.Deserialize<CurrencyApiResponse>(json, options);

                // Tenta converter o valor que vem como string "5.75"
                if (data?.USDBRL != null && decimal.TryParse(data.USDBRL.Bid, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal rate))
                {
                    return rate;
                }
                return null;
            }
            catch 
            {
                return null;
            }
        }

        public async Task<decimal?> GetHistoricalRateAsync(DateTime date)
        {
            int attempts = 0;
            while (attempts < 5) 
            {
                try
                {
                    string dateStr = date.ToString("dd/MM/yyyy");
                    string url = $"{BcbBaseUrl}?formato=json&dataInicial={dateStr}&dataFinal={dateStr}";

                    var response = await _httpClient.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var data = JsonSerializer.Deserialize<List<BcbRate>>(json);

                        if (data != null && data.Count > 0)
                        {
                            if (decimal.TryParse(data[0].Valor, System.Globalization.NumberStyles.Any, new System.Globalization.CultureInfo("pt-BR"), out decimal rate))
                            {
                                return rate;
                            }
                        }
                    }
                }
                catch { }

                date = date.AddDays(-1);
                attempts++;
            }
            return null;
        }
    }
}