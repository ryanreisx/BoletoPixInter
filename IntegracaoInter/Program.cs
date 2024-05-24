using IntegracaoInter.Models;
using IntegracaoInter.Service;
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
        EmissaoService.InicializarEmissao();
    }
   


}

