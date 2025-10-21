using System;
using System.Net;
using System.Text;
using System.Threading;
using System.IO;

namespace Zadatak29Rijks
{
    public class HttpServer
    {
        private readonly HttpListener listener;
        private readonly InMemoryCache cache = new InMemoryCache();
        private readonly Logger logger = new Logger();
        private readonly RijksClient client = new RijksClient();
        private bool running = false;
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(5);
        private readonly ManualResetEvent stopSignal = new ManualResetEvent(false);
        private int activeRequests = 0;

        public HttpServer(int port)
        {
            listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{port}/");
        }

        public void Start()
        {
            running = true;
            listener.Start();
            logger.LogInfo("Server je pokrenut.");
            ThreadPool.QueueUserWorkItem(_ =>
            {
                while (running)
                {
                    try
                    {
                        var context = listener.GetContext();
                        Interlocked.Increment(ref activeRequests);
                        ThreadPool.QueueUserWorkItem(state =>
                        {
                            try
                            {
                                ObradiZahtev(context);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError($"Greska u obradi zahteva: {ex.Message}");
                            }
                            finally
                            {
                                Interlocked.Decrement(ref activeRequests);
                            }
                        });
                    }
                    catch (HttpListenerException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Greska u glavnoj petlji: {ex.Message}");
                    }
                }
                stopSignal.Set();
            });

            ThreadPool.QueueUserWorkItem(_ =>
            {
                while (running)
                {
                    cache.CleanExpired();
                    Thread.Sleep(60000);
                }
            });
        }

        public void Stop()
        {
            running = false;
            if (listener.IsListening)
            {
                listener.Stop();
                listener.Close();
            }
            logger.LogInfo("Cekam da se svi aktivni zahtevi zavrse...");
            stopSignal.WaitOne();
            while (activeRequests > 0)
                Thread.Sleep(50);
            logger.LogInfo("Server je zaustavljen.");
        }

        private void ObradiZahtev(HttpListenerContext context)
        {
            semaphore.Wait();
            try
            {
                string path = context.Request.Url?.AbsolutePath ?? "/";
                string query = context.Request.Url?.Query ?? "";
                string fullKey = path + query;
                logger.LogInfo($"Primljen zahtev: {fullKey}");

                if (path.StartsWith("/api/rijks/search"))
                {
                    string q = context.Request.QueryString["q"];
                    string type = context.Request.QueryString["type"];
                    if (string.IsNullOrEmpty(q) && string.IsNullOrEmpty(type))
                    {
                        PosaljiOdgovor(context, 400, "Morate navesti parametar ?q=<pojam> ili ?type=<vrsta>.\n\nPrimeri:\n/api/rijks/search?q=rembrandt\n/api/rijks/search?type=painting\n/api/rijks/search?q=van+gogh&type=painting");
                        return;
                    }
                    string cached = cache.Get(fullKey);
                    if (cached != null)
                    {
                        logger.LogInfo($"Kes pogodak: {fullKey}");
                        PosaljiOdgovor(context, 200, cached);
                        return;
                    }
                    string odgovor = client.PretraziSlike(q, type);
                    cache.Set(fullKey, odgovor);
                    logger.LogInfo($"API poziv: q={q}, type={type}");
                    PosaljiOdgovor(context, 200, odgovor);
                }
                else
                {
                    PosaljiOdgovor(context, 404, "Nepoznat URL. Koristite /api/rijks/search?q=<pojam> ili /api/rijks/search?type=<vrsta>");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Greska pri obradi zahteva: {ex.Message}");
                PosaljiOdgovor(context, 500, "Doslo je do greske na serveru.");
            }
            finally
            {
                semaphore.Release();
            }
        }

        private void PosaljiOdgovor(HttpListenerContext context, int status, string tekst)
        {
            try
            {
                context.Response.StatusCode = status;
                byte[] buffer = Encoding.UTF8.GetBytes(tekst);
                context.Response.ContentLength64 = buffer.Length;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                logger.LogError($"Greska pri slanju odgovora: {ex.Message}");
            }
        }
    }
}