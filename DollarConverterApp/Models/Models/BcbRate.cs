using System.Text.Json.Serialization;

namespace DollarConverterApp.Models
{
    public class BcbRate
    {
        [JsonPropertyName("data")]
        public string Data { get; set; }

        [JsonPropertyName("valor")]
        public string Valor { get; set; }
    }
}