using System.Text.Json.Serialization;

namespace Xenopool.Server.Networking.RpcWallet;

[JsonSerializable(typeof(GetWalletAddressByIndexResponse), GenerationMode = JsonSourceGenerationMode.Metadata)]
public sealed partial class GetWalletAddressByIndexResponseContext : JsonSerializerContext
{
}

public sealed class GetWalletAddressByIndexResponse
{
    [JsonPropertyName("result")]
    public string Result { get; set; }
}