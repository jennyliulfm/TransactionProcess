using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace Transaction.Models
{
    /// <summary>
    /// This class is country details of credit card which is used for deserialiation 
    /// </summary>
    /// 
    public class Country
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        public override string ToString() => $"Name: {Name} Currency: {Currency}";
    }
}
