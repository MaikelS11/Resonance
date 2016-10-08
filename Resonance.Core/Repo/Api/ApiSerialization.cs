using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Repo.Api
{
    /// <summary>
    /// Helper methods for serialization functionality for api-calls
    /// </summary>
    public static class ApiSerialization
    {
        private static JsonSerializerSettings _serSettings;

        static ApiSerialization()
        {
            var serSetting = new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Include,
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
            };
            // Enums as string instead of integer, no camel-casing for values
            serSetting.Converters.Add(new StringEnumConverter { AllowIntegerValues = false, CamelCaseText = false });

            _serSettings = serSetting;
        }

        /// <summary>
        /// Serialize object to json
        /// </summary>
        /// <param name="theObject">The object to serialize</param>
        /// <returns>string containing json</returns>
        public static string ToJson(this object theObject)
        {
            return JsonConvert.SerializeObject(theObject, _serSettings);
        }

        /// <summary>
        /// Deserialize json-string to object
        /// </summary>
        /// <typeparam name="T">Type of the object to deserialize to</typeparam>
        /// <param name="jsonString">String containing the json</param>
        /// <returns>T</returns>
        public static T FromJson<T>(this string jsonString)
        {
            return JsonConvert.DeserializeObject<T>(jsonString, _serSettings);
        }

        /// <summary>
        /// Creates UTF-8 StringContent object from the passed jsonString and mediaType
        /// </summary>
        /// <param name="jsonString"></param>
        /// <param name="mediaType"></param>
        /// <returns></returns>
        public static StringContent ToStringContent(this string jsonString, string mediaType = "application/json")
        {
            return new StringContent(jsonString, Encoding.UTF8, mediaType);
        }
    }
}
