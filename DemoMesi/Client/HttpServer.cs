using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using Avalonia.Threading;
using Client;
using Client.HeaderConfigSection;

namespace HTTPServer
{
    public class HttpServer
    {
        private static TcpListener? myListener;
        private static int port = int.TryParse(ConfigurationManager.AppSettings["inbound_port"], out int parsedPort) ? parsedPort : 5050;
        private static IPAddress localAddr = IPAddress.Parse(ConfigurationManager.AppSettings["inbound_address"] ?? "127.0.0.1"); 
        public static String dbConnectionString = ConfigurationManager.AppSettings["ConnectionStrings"] ?? "Data Source=mydatabase.db;";
        public static int MessagesReceived = 0;
        public static bool _isRunning = false;
        public static MainWindow MainWindow;
        public static Dictionary<String, String> Headers = new Dictionary<String, String>();

        public static void InitServer(MainWindow mainWindow)
        {
            MainWindow = mainWindow;
            StartServer(localAddr, port);
        }
        
        public static void StartServer(IPAddress ipAddress, int port)
        {
            if (_isRunning)
            {
                Console.WriteLine("Server is already running. Stop the server first.");
                return;
            }
            
            Headers.Clear();
            
            var headersSection = (HeadersSection)ConfigurationManager.GetSection("HeadersSection");

            if (headersSection != null)
            {
                foreach (HeaderElement header in headersSection.Headers)
                {
                    Headers.Add(header.Key, header.Value);
                }
            }

            myListener = new TcpListener(ipAddress, port);
            myListener.Start();
            _isRunning = true;

            Console.WriteLine($"Web Server Running on {ipAddress} on port {port}... Press ^C to Stop...");
            Thread listenerThread = new Thread(new ThreadStart(StartListen));
            listenerThread.Start();
        }
        
        public static void StopServer()
        {
            if (_isRunning)
            {
                _isRunning = false;

                try
                {
                    myListener?.Stop();
                    Console.WriteLine("Server stopped.");
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"SocketException while stopping the server: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Server is not running.");
            }
        }

        
        public static void ChangeIPAddress(IPAddress newAddress, int port)
        {
            StopServer();
                
            StartServer(newAddress, port);
        }

        private static void StartListen()
        {
            using (var connection = new SqliteConnection(dbConnectionString))
            {
                connection.Open();

                string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS Requests (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Json TEXT NOT NULL
                    )";

                using (var command = new SqliteCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                    Console.WriteLine("Table created successfully.");
                }

                while (_isRunning)
                {
                    if (!myListener?.Pending() ?? false)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    TcpClient client = myListener!.AcceptTcpClient();
                    NetworkStream stream = client.GetStream();
                    
                    byte[] requestBytes = new byte[1024];
                    int bytesRead = stream.Read(requestBytes, 0, requestBytes.Length);

                    MessagesReceived++;

                    string request = Encoding.UTF8.GetString(requestBytes, 0, bytesRead);
                    
                    Console.WriteLine(request);

                    var requestHeaders = ParseHeaders(request);

                    Dispatcher.UIThread.Post(() =>
                    {
                        Console.WriteLine("Updating the request on the UI");
                        DateTime currentUtcTime = DateTime.UtcNow;
                        MainWindow.UpdateLastRequest(request, currentUtcTime);
                    });
                    
                    var requestFirstLine = requestHeaders.requestType.Split(" ");

                    string httpVersion = requestFirstLine.LastOrDefault();
                    string contentType = requestHeaders.headers.GetValueOrDefault("Accept");
                    string contentEncoding = requestHeaders.headers.GetValueOrDefault("Accept-Encoding");
                    
                    if (requestFirstLine.Length < 2)
                    {
                        Console.WriteLine("Invalid request: " + requestHeaders.requestType);
                        SendHeaders(httpVersion, 400, "Bad Request", contentType, contentEncoding, 0, ref stream);
                        stream.Close();
                        continue;
                    }
                    
                    string requestPath = requestFirstLine[1];

                    if (requestPath.Equals("/settings", StringComparison.Ordinal))
                    {
                        if (!requestFirstLine[0].Contains("POST"))
                        {
                            SendHeaders(httpVersion, 405, "Method Not Allowed", contentType, contentEncoding, 0, ref stream);
                        }
                        else
                        {
                            HandleSettingsRequest(request, ref stream, connection, httpVersion, contentType, contentEncoding);
                        }

                        continue;
                    }
                    
                    SendHeaders(httpVersion, 200, "Ok", contentType, contentEncoding, 0, ref stream);
                    Console.WriteLine("Request answered");
                    stream.Close();
                }
            }
        }

        private static void HandleSettingsRequest(String request, ref NetworkStream stream,
            SqliteConnection? connection,
            String httpVersion, String contentType, String contentEncoding)
        {
            int headerEndIndex = request.IndexOf("\r\n\r\n", StringComparison.Ordinal);

            string content = request.Substring(headerEndIndex + 4);

            StringBuilder updatedSettings = new StringBuilder();
            updatedSettings.Append("Settings Updated:\n");

            bool inboundSettingsChanged = false;
            
            IPAddress inboundAddressIP = localAddr;
            int inboundPortNumber = port;

            try {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                
                using (JsonDocument doc = JsonDocument.Parse(content))
                {
                    var root = doc.RootElement;
                    
                    if (root.TryGetProperty("outbound_address", out JsonElement outboundAddressElement))
                    {
                        string outboundAddress = outboundAddressElement.GetString();
                        
                        AddOrUpdateConfig(config, "outbound_address", outboundAddress);
                        updatedSettings.Append("outbound_address\n");
                        Console.WriteLine("Setting outbound address: " + outboundAddress);
                    }
                    if (root.TryGetProperty("outbound_port", out JsonElement outboundPortElement))
                    {
                        string outboundPort = outboundPortElement.GetString();
                        if (int.TryParse(outboundPort, out _))
                        {
                            AddOrUpdateConfig(config, "outbound_port", outboundPort);
                            updatedSettings.Append("outbound_port\n");
                            Console.WriteLine("Setting outbound port: " + outboundPort);
                        }
                        else
                        {
                            updatedSettings.Append("Failed: outbound_port - Port is not a int\n");
                        }
                    }
                    
                    if (root.TryGetProperty("path", out JsonElement pathElement))
                    {
                        string path = pathElement.GetString();
                        AddOrUpdateConfig(config, "path", path);
                        updatedSettings.Append("path\n");
                        Console.WriteLine("Setting outbound port: " + path);
                    }
                    
                    if (root.TryGetProperty("inbound_address", out JsonElement inboundAddressElement))
                    {
                        string inboundAddress = inboundAddressElement.GetString();
                        bool parsed = false;
                        
                        if (inboundAddress.Equals("*", StringComparison.Ordinal))
                        {
                            inboundAddressIP = IPAddress.Any;
                            inboundSettingsChanged = true;
                            parsed = true;
                        }
                        else
                        {
                            try
                            {
                                inboundAddressIP = IPAddress.Parse(inboundAddress);
                                inboundSettingsChanged = true;
                                parsed = true;
                            }
                            catch (Exception ex) when (ex is FormatException || ex is ArgumentNullException) { }
                        }

                        if (parsed)
                        {
                            AddOrUpdateConfig(config, "inbound_address", inboundAddress);
                            updatedSettings.Append("inbound_address\n");
                            Console.WriteLine("Setting inbound address: " + inboundAddress);
                        }
                        else
                        {
                            updatedSettings.Append("Failed: inbound_address - Invalid IP\n");
                        }
                    }
                    
                    if (root.TryGetProperty("inbound_port", out JsonElement inboundPortElement))
                    {
                        string inboundPort = inboundPortElement.GetString();
                        if (int.TryParse(inboundPort, out _))
                        {
                            inboundSettingsChanged = true;
                            AddOrUpdateConfig(config, "inbound_port", inboundPort);
                            updatedSettings.Append("inbound_port\n");
                            Console.WriteLine("Setting inbound port: " + inboundPort);
                        }
                        else
                        {
                            updatedSettings.Append("Failed: inbound_port - Port is not a int\n");
                        }
                    }

                    if (root.TryGetProperty("add_header", out JsonElement addHeaderElement))
                    {
                        var headersSection = (HeadersSection)config.GetSection("HeadersSection");
                        
                        foreach (JsonProperty headerProperty in addHeaderElement.EnumerateObject())
                        {
                            if (Headers.ContainsKey(headerProperty.Name))
                            {
                                Headers[headerProperty.Name] = headerProperty.Value.GetString();
                                updatedSettings.Append($"Header updated: {headerProperty.Name} - {headerProperty.Value}\n");
                            }
                            else
                            {
                                Headers.Add(headerProperty.Name, headerProperty.Value.GetString());
                                updatedSettings.Append($"Header added: {headerProperty.Name} - {headerProperty.Value}\n");
                            }
                            
                            var existingHeader = headersSection.Headers.Cast<HeaderElement>().FirstOrDefault(h => h.Key == headerProperty.Name);
                            if (existingHeader != null)
                            {
                                existingHeader.Value = headerProperty.Value.GetString();
                            }
                            else
                            {
                                HeaderElement newHeader = new HeaderElement
                                {
                                    Key = headerProperty.Name,
                                    Value = headerProperty.Value.GetString()
                                };
                                headersSection.Headers.Add(newHeader);
                            }
                            
                            
                        }
                    }

                    if (root.TryGetProperty("remove_header", out JsonElement removeHeaderElement))
                    {
                        var headersSection = (HeadersSection)config.GetSection("HeadersSection");

                        foreach (var headerProperty in removeHeaderElement.EnumerateObject())
                        {
                            Headers.Remove(headerProperty.Name);
                            headersSection.Headers.Remove(headerProperty.Name);
                            updatedSettings.Append($"Header removed: {headerProperty.Name}\n");
                        }
                    }
                    
                    
                    if (inboundSettingsChanged)
                    {
                        ChangeIPAddress(inboundAddressIP, inboundPortNumber);
                    }
                    
                    config.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection("HeadersSection");
                    ConfigurationManager.RefreshSection("appSettings");
                    
                    if (root.TryGetProperty("get_settings", out _))
                    {
                        updatedSettings.Append("\n\n{\n" +
                                               $"   inbound_address: {ConfigurationManager.AppSettings["inbound_address"]},\n" +
                                               $"   inbound_port: {ConfigurationManager.AppSettings["inbound_port"]},\n" +
                                               $"   outbound_address: {ConfigurationManager.AppSettings["outbound_address"]},\n" +
                                               $"   outbound_port: {ConfigurationManager.AppSettings["outbound_port"]},\n" +
                                               $"   path: {ConfigurationManager.AppSettings["path"]},\n" +
                                               "   headers: {\n");

                        foreach (var header in Headers)
                        {
                            updatedSettings.Append($"       {header.Key}: {header.Value},\n");
                        }
                                               
                        updatedSettings.Append("    }\n}");
                    }
                }
            }
            catch (JsonException jsonEx)
            {
                SendHeaders(httpVersion, 400, "Bad request", contentType, contentEncoding, 0, ref stream);
                Console.WriteLine("Invalid JSON format: " + jsonEx.Message);
            }
            
            HandleDbInsert(connection, content);

            byte[] updatedSettingsByteArray = Encoding.UTF8.GetBytes(updatedSettings.ToString());
            
            SendHeaders(httpVersion, 200, "OK", contentType, contentEncoding, 0, ref stream);
            stream.Write(updatedSettingsByteArray, 0, updatedSettingsByteArray.Length);
            stream.Close();
        }

        private static void AddOrUpdateConfig(Configuration config, String key, String value)
        {
            if (config.AppSettings.Settings[key] == null)
            {
                config.AppSettings.Settings.Add(key, value);
            }
            else
            {
                config.AppSettings.Settings[key].Value = value;
            }
        }

        private static void HandleDbInsert(SqliteConnection? connection, String content)
        {
            string insertQuery = "INSERT INTO Requests (Json) VALUES (@Json)";
            using (var command = new SqliteCommand(insertQuery, connection))
            {
                command.Parameters.AddWithValue("@Json", content);
                command.ExecuteNonQuery();
                Console.WriteLine("Data inserted successfully.");
            }
        }

        private static (Dictionary<string, string> headers, string requestType) ParseHeaders(string headerString)
        {
            var headerLines = headerString.Split('\r', '\n');
            string firstLine = headerLines[0];
            var headerValues = new Dictionary<string, string>();
            foreach (var headerLine in headerLines)
            {
                var headerDetail = headerLine.Trim();
                var delimiterIndex = headerLine.IndexOf(':');
                if (delimiterIndex >= 0)
                {
                    var headerName = headerLine.Substring(0, delimiterIndex).Trim();
                    var headerValue = headerLine.Substring(delimiterIndex + 1).Trim();

                    if (headerValues.ContainsKey(headerName))
                    {
                        headerValues[headerName] = headerValue;
                    }
                    else
                    {
                        headerValues.Add(headerName, headerValue);
                    }
                }
            }
            return (headerValues, firstLine);
        }

        private static void SendHeaders(string? httpVersion, int statusCode, string statusMsg, string? contentType, string? contentEncoding,
            int byteLength, ref NetworkStream networkStream)
        {
            
            
            StringBuilder sb = new StringBuilder();
            sb.Append($"{httpVersion} {statusCode} {statusMsg}\r\n");
            sb.Append("Connection: Keep-Alive\r\n");
            sb.Append($"Date: {DateTime.UtcNow.ToString()}\r\n");
            sb.Append($"Content-Type: application/json\r\n");
            sb.Append($"Content-Encoding: {contentEncoding}\r\n");

            foreach (var header in Headers)
            {
                sb.Append($"{header.Key}: {header.Value}\r\n");
            }
            
            sb.Append("\r\n");

            byte[] responseBytes = Encoding.UTF8.GetBytes(sb.ToString());
            networkStream.Write(responseBytes, 0, responseBytes.Length);
        }
    }
}
    
