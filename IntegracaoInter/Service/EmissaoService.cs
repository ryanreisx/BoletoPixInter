using IntegracaoInter.Models;
using NuvemFiscalAPI.Logs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IntegracaoInter.Service
{
    public class EmissaoService
    {
        public static async Task InicializarEmissao()
        {
            HttpClient client;
            String? bearerToken;
            String permissoes = "boleto-cobranca.write boleto-cobranca.read";


            X509Certificate cert = obterCert();

            bearerToken = obterBearerToken(permissoes, out client, cert);

            var codigoSolicitacao = await EmitirCobrancaAsync(client, bearerToken, cert);

            var cobrancaBase64 = await RecuperarCobrancaPDF(client, bearerToken, cert, codigoSolicitacao);

            SalvarPDFPorBase64(cobrancaBase64, "C:[SEU_CAMINHO]\\cobranca.pdf");
        }

        public static async Task<String?> EmitirCobrancaAsync(HttpClient client, string bearerToken, X509Certificate cert)
        {
            var cobranca = new
            {
                seuNumero = "",
                valorNominal = 0.0,
                dataVencimento = "",
                numDiasAgenda = 0,
                pagador = new
                {
                    email = "",
                    ddd = "",
                    telefone = "",
                    numero = "",
                    complemento = "",
                    cpfCnpj = "",
                    tipoPessoa = "",
                    nome = "",
                    endereco = ".",
                    bairro = "",
                    cidade = ".",
                    uf = "",
                    cep = ""
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
                        Log.LogToFile("RecuperarCobrancaPDF Erro:", errorResponse);

                        return null;
                    }
                }
            }
        }

        private static X509Certificate obterCert()
        {
            String certPem = File.ReadAllText("[ARQUIVOCRT]");
            String keyPem = File.ReadAllText("[ARQUIVOKEY]");

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
            new KeyValuePair<string, string>("client_id", "[CLIENTEID]"),
            new KeyValuePair<string, string>("client_secret", "[CLIENTESECRET]"),
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
                    Log.LogToFile("RecuperarCobrancaPDF Erro:", response_pdf.StatusCode.ToString());
                    return null;
                }
            }
        }

        public static void SalvarPDFPorBase64(string base64String, string filePath)
        {
            byte[] bytes = Convert.FromBase64String(base64String);

            System.IO.File.WriteAllBytes(filePath, bytes);
        }
    }
}
