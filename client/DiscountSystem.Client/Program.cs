using DiscountSystem.Protos;

using Grpc.Net.Client;

// Enable HTTP/3 support in client
var address = "https://server:5001";
var channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions
{
    HttpHandler = new SocketsHttpHandler
    {
        EnableMultipleHttp3Connections = true ,
        SslOptions = new System.Net.Security.SslClientAuthenticationOptions
        {
            RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
        }
    }
});
var client = new DiscountService.DiscountServiceClient(channel);

// Generate codes
int requestedCount = 20;
int codeLength = 7;
var genResponse = await client.GenerateDiscountCodesAsync(new GenerateDiscountCodeRequest { Count = requestedCount, Length = codeLength });
Console.WriteLine($"Generation success: {genResponse.Result}");

// Use a discount code, update codeToUse with good/actual code
string codeToUse = "F2DFGSJ";
var useResponse = await client.UseDiscountCodeAsync(new UseDiscountCodeRequest { Code = codeToUse });
Console.WriteLine($"Use result: {useResponse.Result}");