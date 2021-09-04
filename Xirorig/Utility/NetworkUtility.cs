using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xirorig.Algorithm.Xiropht.Decentralized;
using Xirorig.Network;
using Xirorig.Network.Api.JsonConverter;
using Xirorig.Options;

namespace Xirorig.Utility
{
    internal static class NetworkUtility
    {
        public static string DefaultUserAgent { get; } = $"{ApplicationUtility.Name}/{ApplicationUtility.Version}";

        public static JsonSerializerOptions DefaultJsonSerializerOptions { get; } = new()
        {
            Converters = { new BigIntegerJsonConverter(), new JsonStringEnumConverter() },
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public static INetwork CreateNetwork(ApplicationContext context, Pool pool)
        {
            var poolAlgorithm = pool.GetAlgorithm();

            if (poolAlgorithm.Equals("xiropht_decentralized", StringComparison.OrdinalIgnoreCase))
            {
                return new XirophtDecentralizedNetwork(context, pool);
            }

            throw new NotImplementedException($"{pool.Algorithm} is not implemented.");
        }

        public static INetwork[] CreateDevNetworks(ApplicationContext context)
        {
            return new[]
            {
                CreateNetwork(context, new Pool
                {
                    Algorithm = "Xiropht_Decentralized",
                    Coin = "Xiropht",
                    Url = "127.0.0.1:2401",
                    Username = "3tzCoDt5nBykMhEEXgnsiKrDNyqT7BmWUzsU1zf1ivhFCHfbZXJJ9YDbmBeKCG1b6k3419BRZuXGnWECtUxncnGB3JYxjvNARKqdFpDads89RL",
                    UserAgent = DefaultUserAgent
                })
            };
        }
    }
}