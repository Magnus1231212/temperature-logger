using nanoFramework.Runtime.Native;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using temperature_logger.Models;

namespace temperature_logger.Modules
{
    public class WebServer
    {
        private HttpListener _listener;
        private Thread _serverThread;

        public void Start()
        {
            if (_listener == null)
            {
                _listener = new HttpListener("http", 80);
                _serverThread = new Thread(RunServer);
                _serverThread.Start();
                Debug.WriteLine("[WebServer] Listening on http://192.168.4.1/");
            }
        }

        public void Stop()
        {
            _listener?.Stop();
            _listener = null;
        }

        private void RunServer()
        {
            _listener.Start();
            while (_listener.IsListening)
            {
                var context = _listener.GetContext();
                if (context != null)
                    ProcessRequest(context);
            }
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            var req = context.Request;
            var res = context.Response;

            try
            {
                if (req.HttpMethod == "POST")
                {
                    Hashtable pars = ParseParamsFromStream(req.InputStream);
                    string ssid = pars["ssid"]?.ToString();
                    string password = pars["password"]?.ToString();

                    if (!string.IsNullOrEmpty(ssid) && !string.IsNullOrEmpty(password))
                    {
                        Debug.WriteLine($"[Web] Received new Wi-Fi config: {ssid}/{password}");

                        var cfg = new DeviceConfig
                        {
                            WifiSSID = ssid,
                            WifiPassword = password
                        };

                        JsonStorage.Save(cfg, "config.json");

                        string msg = "<p>Wi-Fi configuration saved.<br>Device will reboot now.</p>";
                        SendHtml(res, BuildFormPage(msg));

                        Thread.Sleep(2000);
                        WirelessAP.Disable();
                        Power.RebootDevice();
                        return;
                    }
                    else
                    {
                        SendHtml(res, BuildFormPage("<p>SSID or password missing.</p>"));
                    }
                }
                else
                {
                    SendHtml(res, BuildFormPage("<p>Enter your Wi-Fi credentials below:</p>"));
                }
            }
            catch (Exception ex)
            {
                SendHtml(res, $"<h3>Error: {ex.Message}</h3>");
            }
            finally
            {
                res.Close();
            }
        }

        private static Hashtable ParseParamsFromStream(Stream stream)
        {
            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);
            string body = new string(Encoding.UTF8.GetChars(buffer));
            var hash = new Hashtable();

            foreach (var pair in body.Split('&'))
            {
                var kv = pair.Split('=');
                if (kv.Length == 2)
                    hash[kv[0]] = UrlDecode(kv[1]);
            }
            return hash;
        }

        private static string UrlDecode(string value)
        {
            if (value == null)
                return null;

            var output = new StringBuilder();
            int i = 0;
            while (i < value.Length)
            {
                if (value[i] == '%' && i + 2 < value.Length)
                {
                    string hex = value.Substring(i + 1, 2);
                    output.Append((char)Convert.ToInt32(hex, 16));
                    i += 3;
                }
                else if (value[i] == '+')
                {
                    output.Append(' ');
                    i++;
                }
                else
                {
                    output.Append(value[i]);
                    i++;
                }
            }
            return output.ToString();
        }

        private static void SendHtml(HttpListenerResponse res, string html)
        {
            byte[] data = Encoding.UTF8.GetBytes(html);
            res.ContentType = "text/html";
            res.ContentLength64 = data.Length;
            res.OutputStream.Write(data, 0, data.Length);
        }

        private static string BuildFormPage(string message)
        {
            return $@"<!DOCTYPE html><html><head>
                    <meta charset='utf-8'>
                    <title>ESP32 Wi-Fi Setup</title>
                    <style>
                    body {{ font-family: sans-serif; margin: 20px; text-align: center; }}
                    input {{ width: 80%; margin: 5px; padding: 8px; }}
                    </style></head><body>
                    <h2>ESP32 Wi-Fi Configuration</h2>
                    {message}
                    <form method='POST'>
                    SSID:<br><input name='ssid'><br>
                    Password:<br><input name='password' type='password'><br>
                    <input type='submit' value='Save & Reboot'>
                    </form></body></html>";
        }
    }
}
