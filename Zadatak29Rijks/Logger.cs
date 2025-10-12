using System;
using System.IO;

namespace Zadatak29Rijks
{
    public class Logger
    {
        private readonly string logFolder = "logs";

        public Logger()
        {
            if (!Directory.Exists(logFolder))
                Directory.CreateDirectory(logFolder);
        }

        private void Zapisi(string poruka)
        {
            string fajl = Path.Combine(logFolder, $"log-{DateTime.Now:yyyy-MM-dd}.txt");
            string linija = $"{DateTime.Now:HH:mm:ss} {poruka}{Environment.NewLine}";
            File.AppendAllText(fajl, linija);
        }

        public void LogInfo(string poruka) => Zapisi($"INFO  {poruka}");
        public void LogError(string poruka) => Zapisi($"GRESKA {poruka}");
    }
}
