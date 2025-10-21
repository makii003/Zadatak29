using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Zadatak29Rijks
{
    public class RijksClient
    {
        private readonly HttpClient client = new HttpClient();
        private const string BASE_URL = "https://www.rijksmuseum.nl/api/en/collection";
        private const string API_KEY = "0fiuZFh4"; // Javni demo kljuc
        public string PretraziSlike(string query, string type)
        {
            try
            {
                var parametri = new List<string>
                {
                    $"key={API_KEY}",
                    "format=json",
                    "ps=10", // broj rezultata po strani
                    "imgonly=true"
                };

                if (!string.IsNullOrEmpty(query))
                    parametri.Add($"q={Uri.EscapeDataString(query)}");

                if (!string.IsNullOrEmpty(type))
                    parametri.Add($"type={Uri.EscapeDataString(type)}");

                string url = $"{BASE_URL}?{string.Join("&", parametri)}";
                HttpResponseMessage response = client.GetAsync(url).Result;
                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = response.Content.ReadAsStringAsync().Result;
                    throw new Exception($"Greska prilikom API poziva ({response.StatusCode}): {errorContent}");
                }
                string jsonOdgovor = response.Content.ReadAsStringAsync().Result;
                return EkstraktujInformacijeOSlikama(jsonOdgovor);
            }
            catch (Exception ex)
            {
                return $"Greska prilikom API poziva: {ex.Message}";
            }
        }

        private string EkstraktujInformacijeOSlikama(string jsonOdgovor)
        {
            try
            {
                JObject jsonObj = JObject.Parse(jsonOdgovor);
                if (jsonObj["error"] != null)
                    return $"API greska: {jsonObj["error"]}";

                JArray artObjects = (JArray)jsonObj["artObjects"];
                if (artObjects == null || artObjects.Count == 0)
                    return "Nema pronadjenih dela koja zadovoljavaju uslove pretrage.";

                var rezultati = new List<string>();
                rezultati.Add($"Pronadjeno {artObjects.Count} dela:\n");
                rezultati.Add(new string('=', 60));

                foreach (JToken artObject in artObjects)
                {
                    string naslov = artObject["title"]?.ToString() ?? "Nepoznat naslov";
                    string autor = artObject["principalOrFirstMaker"]?.ToString() ?? "Nepoznat autor";
                    string slikaUrl = artObject["webImage"]?["url"]?.ToString()
                                   ?? artObject["headerImage"]?["url"]?.ToString()
                                   ?? "Nema slike";
                    string objectNumber = artObject["objectNumber"]?.ToString() ?? "";

                    rezultati.Add($"\n{naslov}");
                    rezultati.Add($"Autor: {autor}");
                    rezultati.Add($"ID: {objectNumber}");

                    if (!string.IsNullOrEmpty(slikaUrl) && slikaUrl != "Nema slike")
                        rezultati.Add($"Slika dostupna: {slikaUrl}");

                    rezultati.Add(new string('-', 40));
                }
                return string.Join("\n", rezultati);
            }
            catch (Exception ex)
            {
                return $"Greska pri obradi podataka: {ex.Message}";
            }
        }
    }
}