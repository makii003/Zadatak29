using System;

namespace Zadatak29Rijks
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int port = 8080;
            var server = new HttpServer(port);
            server.Start();
            Console.WriteLine($"Server je pokrenut na http://localhost:{port}/");
            Console.WriteLine("Pritisni ENTER da zaustavis server...");
            Console.ReadLine();
            server.Stop();
        }
    }
}
