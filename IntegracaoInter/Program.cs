using System;
using System.Net.Http.Json;
using System.Runtime.ConstrainedExecution;
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

        var codigoSolicitacao = await EmitirCobrancaAsync(client, bearerToken, cert);

        var cobrancaBase64 = await RecuperarCobrancaPDF(client, bearerToken, cert, codigoSolicitacao);

        SavePdfFromBase64(cobrancaBase64, "C:\\Users\\ryan0\\source\\repos\\IntegracaoInter\\cobranca.pdf");

    }
    public static async Task<String?> EmitirCobrancaAsync(HttpClient client, string bearerToken, X509Certificate cert)
    {
        var cobranca = new
        {
            seuNumero = "180",
            valorNominal = 19.90M,
            dataVencimento = "2024-05-31",
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
                nome = "Ryan Reis",
                endereco = "Rua Exemplo",
                bairro = "Bairro Exemplo",
                cidade = "Salvador",
                uf = "BA",
                cep = "68906801"
            }
        };

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(cobranca);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        using (HttpClientHandler handler = new HttpClientHandler())
        {
            handler.ClientCertificates.Add(cert);

            using (client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {bearerToken}");

                HttpResponseMessage response = await client.PostAsync("https://cdpj.partners.bancointer.com.br/cobranca/v3/cobrancas", data);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var responseJson = Newtonsoft.Json.Linq.JObject.Parse(responseBody);
                    string codigoSolicitacao = responseJson.Value<string>("codigoSolicitacao");

                    return codigoSolicitacao;
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
 
                    return null;
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

    public static async Task<String?> RecuperarCobrancaPDF(HttpClient client, string bearerToken, X509Certificate cert, string codigoSolicitacao)
    {
        var clientHandler = new HttpClientHandler();
        clientHandler.ClientCertificates.Add(cert);
        clientHandler.ClientCertificateOptions = ClientCertificateOption.Manual;

        using (client = new HttpClient(clientHandler))
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + bearerToken);

            HttpResponseMessage response_pdf = client.GetAsync($"https://cdpj.partners.bancointer.com.br/cobranca/v3/cobrancas/{codigoSolicitacao}/pdf").GetAwaiter().GetResult();
            if (response_pdf.IsSuccessStatusCode)
            {
                string responseBody = await response_pdf.Content.ReadAsStringAsync();
                var responseJson = Newtonsoft.Json.Linq.JObject.Parse(responseBody);
                string cobrancaBase64 = responseJson.Value<string>("pdf");
                return cobrancaBase64;
            }
            else
            {
                Console.WriteLine("Error, received status code {0}: {1}", response_pdf.StatusCode, response_pdf.ReasonPhrase);
                return null;
            }
        }
    }

    public static void SavePdfFromBase64(string base64String, string filePath)
    {
        byte[] bytes = Convert.FromBase64String(base64String);

        System.IO.File.WriteAllBytes(filePath, bytes);
    }

    public class TokenModel
    {
        public string? access_token { get; set; }
        public string? token_type { get; set; }
        public int expires_in { get; set; }
        public string? scope { get; set; }
    }
}

