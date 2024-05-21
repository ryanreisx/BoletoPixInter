using System;
using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace Get;

class Program
{
    static async Task Main(string[] args)
    {
        HttpClient client;
        String? bearerToken;
        String permissoes = "boleto-cobranca.write boleto-cobranca.read";


        X509Certificate cert = obterCert();

        //Obtendo bearer token 
        bearerToken = obterBearerToken(permissoes, out client, cert);
        Console.WriteLine("Bearer Token: {0}", bearerToken);

        var cobranca = new
        {
            seuNumero = "71987670057",
            valorNominal = 100,
            dataVencimento = "2024-12-31",
            numDiasAgenda = 30,
            pagador = new
            {
                email = "ryanreis280903@gmail.com",
                ddd = "71",
                telefone = "987670057",
                numero = "21",
                complemento = "Apto 45",
                cpfCnpj = "24167101000110",
                tipoPessoa = "JURIDICA",
                nome = "Ryan Reis dos Santos",
                endereco = "Rua Exemplo",
                bairro = "Bairro Exemplo",
                cidade = "Salvador",
                uf = "BA",
                cep = "68906801"
            }
        };

        // Serializando o objeto em JSON
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(cobranca);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        using (HttpClientHandler handler = new HttpClientHandler())
        {
            // Obtendo o certificado
            handler.ClientCertificates.Add(cert);

            using (client = new HttpClient(handler))
            {
                // Adicionando headers
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {bearerToken}");

                // Fazendo a requisição POST
                HttpResponseMessage response = await client.PostAsync("https://cdpj.partners.bancointer.com.br/cobranca/v3/cobrancas", data);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Cobrança emitida com sucesso!");
                    Console.WriteLine(responseBody);
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Erro ao emitir cobrança:");
                    Console.WriteLine(errorResponse);
                }
            }
        }
    }

    private static X509Certificate obterCert()
    {
        String certPem = File.ReadAllText("crt.crt");
        String keyPem = File.ReadAllText("key.key");

        X509Certificate2 cert = X509Certificate2.CreateFromPem(certPem, keyPem);
        cert = new X509Certificate2(cert.Export(X509ContentType.Pfx));


        return cert;
    }

    private static String? obterBearerToken(String permissoes, out HttpClient client, X509Certificate cert)
    {
        var clientHandlerOauth = new HttpClientHandler();
        clientHandlerOauth.ClientCertificateOptions = ClientCertificateOption.Manual;
        clientHandlerOauth.ClientCertificates.Add(cert);

        String URI_Token = "https://cdpj.partners.bancointer.com.br/oauth/v2/token";

        var data = new[]
        {
            new KeyValuePair<string, string>("client_id", "dc82f735-5675-4a9b-9a93-cd4e3188593e"),
            new KeyValuePair<string, string>("client_secret", "eb2b7ada-c5b4-4897-aa6d-d94a643dc95c"),
            new KeyValuePair<string, string>("scope", permissoes),
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        };

        using (client = new HttpClient(clientHandlerOauth))
        {
            var response = client.PostAsync(URI_Token, new FormUrlEncodedContent(data)).GetAwaiter().GetResult();

            String jsonStr = response.Content.ReadAsStringAsync().Result;

            TokenModel? tokenModel = JsonSerializer.Deserialize<TokenModel>(jsonStr);
            String bearerToken = tokenModel?.access_token;

            client.Dispose();

            return bearerToken;
        }
    }


    public static async Task EmitirCobrancaAsync(string bearerToken, X509Certificate cert, object dadosBoleto, string contaCorrente)
    {
        
    }

    public class TokenModel
    {
        public string? access_token { get; set; }
        public string? token_type { get; set; }
        public int expires_in { get; set; }
        public string? scope { get; set; }
    }
}

