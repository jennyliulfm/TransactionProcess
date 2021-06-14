using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace Transaction.Models
{
    /// <summary>
    /// This class is bank details of credit card which is used for deserialiation 
    /// </summary>
    /// 
    public class Bank
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("url")]
        public string URL { get; set; }

        [JsonPropertyName("phone")]
        public string Phone { get; set; }

        [JsonPropertyName("city")]
        public string City { get; set; }

        public override string ToString()
        {
            return $"Name: {Name} URL: {URL} Phone: {Phone} City: {City}";
        }
    }
}
