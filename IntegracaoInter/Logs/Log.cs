using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuvemFiscalAPI.Logs
{
    public static class Log
    {
        public static void LogToFile(string title, string logMessage)
        {
            string FileName = DateTime.Now.ToString("ddMMyyyy") + ".txt";
            StreamWriter swLog;
            if (File.Exists(FileName))
            {
                swLog = File.AppendText(FileName);
            }
            else 
            {
                swLog= new StreamWriter(FileName);
            }

            swLog.WriteLine("Log:");
            swLog.WriteLine(DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString());

            swLog.WriteLine("MÉTODO:  {0}", title);
            swLog.WriteLine("Mensagem:  {0}", logMessage);
            swLog.WriteLine("----------------------------------");
            swLog.WriteLine("");
            swLog.Close();

        }
    }
}
