using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Transaction.Models
{
    /// <summary>
    /// This class is for CreditCard which is used for deserialiation 
    /// </summary>
   public class CreditCard
    {
        [JsonPropertyName("scheme")]
        public string Scheme { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("brand")]
        public string Brand { get; set; }

        [JsonPropertyName("country")]
        public Country Country { get; set; }

        [JsonPropertyName("bank")]
        public Bank Bank { get; set; }

        public override string ToString()
        {
            return $"{Scheme} {Type}";
        }
    }
}
