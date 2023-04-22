using System.Text.Json.Serialization;

namespace Xenopool.Server.RpcWallet;

[JsonSerializable(typeof(GetTotalWalletIndexResponse), GenerationMode = JsonSourceGenerationMode.Metadata)]
public sealed partial class GetTotalWalletIndexResponseContext : JsonSerializerContext
{
}

public sealed class GetTotalWalletIndexResponse
{
    [JsonPropertyName("result")]
    public int Result { get; set; }
}