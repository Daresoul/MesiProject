using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Xml;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HTTPServer;
using Microsoft.Data.Sqlite;

namespace Client
{
    public partial class MainWindow : Window
    {
        private bool _isDragging;
        private Point _lastPosition;
        
        private NameValueCollection _config;
        
        private Canvas main;
        private StartRect bodyStart;
        private HeaderStartBlock headerStart;
        public SaveManager SaveManager;
        private Button startButton;
        private Button stopButton;
        private TextBlock ResponseTextBlock;

        public MainWindow()
        {
            InitializeComponent();
            
            main = this.FindControl<Canvas>("MainCanvas");
            ResponseTextBlock = this.FindControl<TextBlock>("ResponseTextBox");
            var initialKeyValue = this.FindControl<KeyValueRect>("InitialKeyValueRect");
            initialKeyValue.PointerPressed += CreateKeyValueRect;
            
            var initialObject = this.FindControl<ObjectRect>("InitialObjectRect");
            initialObject.PointerPressed += CreateObjectRect;
            
            var initialArray = this.FindControl<ArrayRect>("InitialArrayRect");
            initialArray.PointerPressed += CreateArrayRect;
            
            var initialHeaderKeyValueRect = this.FindControl<HeaderKeyValueBlock>("InitialHeaderKeyValueRect");
            initialHeaderKeyValueRect.PointerPressed += CreateHeaderKeyValueRect;

            bodyStart = new StartRect();
            bodyStart.isStatic = false;
            
            Canvas.SetLeft(bodyStart, 350);
            Canvas.SetTop(bodyStart, 25);
            
            main.Children.Add(bodyStart);
            
            headerStart = new HeaderStartBlock();
            headerStart.isStatic = false;
            
            Canvas.SetLeft(headerStart, 700);
            Canvas.SetTop(headerStart, 25);
            
            main.Children.Add(headerStart);

            using (var connection = new SqliteConnection(HttpServer.dbConnectionString))
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
            }
            
            HttpServer.InitServer(this);
            
            /*var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            timer.Tick += TimerTick;
            timer.Start();*/
        }

        private void TimerTick(object? sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var rect in FindAllMoveableRectangles(this))
            {
                sb.Append(rect + ": " + rect.NextBlock + " - " + rect.NextBlockOf + " - " + rect.PartOf + "\n");
            }

            ResponseTextBlock.Text = "All Stats: \n" + sb.ToString();
        }

        public void UpdateLastRequest(String request, DateTime time)
        {
            ResponseTextBlock.Text = $"{time.ToString("yyyy-MM-dd HH:mm:ss.fff")}:\n{request}";
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            main = this.FindControl<Canvas>("MainCanvas");
            SaveManager = new SaveManager();
            LoadState();
        }
        
        
        
        public static List<BaseRect> FindAllRectangles(Visual parent)
        {
            var rectangles = new List<BaseRect>();
    
            foreach (var child in parent.GetVisualChildren())
            {
                if (child is BaseRect rect && !rect.isStatic)
                {
                    rectangles.Add(rect);
                }
                else if (child is Visual visualChild)
                {
                    rectangles.AddRange(FindAllRectangles(visualChild));
                }
            }

            return rectangles;
        }
        
        public static List<BaseRect> FindAllMoveableRectangles(Visual parent)
        {
            var rectangles = new List<BaseRect>();
    
            foreach (var child in parent.GetVisualChildren())
            {
                if (child is BaseRect rect && !rect.isStatic && child.GetType() != typeof(StartRect) && child.GetType() != typeof(HeaderStartBlock))
                {
                    rectangles.Add(rect);
                }
                else if (child is Visual visualChild)
                {
                    rectangles.AddRange(FindAllMoveableRectangles(visualChild));
                }
            }

            return rectangles;
        }
        
        
        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is BaseRect rect)
            {
                _isDragging = true;
                _lastPosition = e.GetPosition(this);
            }
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            
            if (_isDragging && sender is BaseRect rect)
            {
                var currentPosition = e.GetPosition(this);
                
                if (rect.PartOf != null)
                {
                    rect.PartOf.RemoveBlock(rect);
                    rect.PartOf = null;
                }

                if (rect.NextBlockOf != null)
                {
                    rect.NextBlockOf.NextBlock = null;
                }

                rect.NextBlockOf = null;
                
                rect.MoveChildren(currentPosition, _lastPosition);
                
                _lastPosition = currentPosition;
            }
        }

        public void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_isDragging && sender is BaseRect block)
            {
                var rectangles = FindAllRectangles(this);
                foreach (var rectangle in rectangles)
                {
                    if (rectangle is not HeaderStartBlock && rectangle is not HeaderKeyValueBlock)
                    {
                        if (check_bottom(block, rectangle.Bounds.Bottom, rectangle.Bounds.Left))
                        {
                            rectangle.NextBlock = block;
                            block.NextBlockOf = rectangle;
                            block.SnapIntoPlace(rectangle.Bounds.Left);
                            rectangle.PartOf?.AddBlock(block);
                        }

                        if (rectangle is BaseContainer objectRect)
                        {
                            var middle = objectRect.GetMiddle();
                            if (check_bottom(block, middle, objectRect.Bounds.Left))
                            {
                                objectRect.AddBlock(block);
                                block.SnapIntoPlace(objectRect.Bounds.Left);
                            }
                        }
                    }
                }
                
                _isDragging = false;
            }

            Dispatcher.UIThread.Post(() =>
            {
                SaveManager.SaveState(FindAllMoveableRectangles(this), bodyStart, headerStart);
            });
        }
        
        private void OnPointerReleasedHeader(object? sender, PointerReleasedEventArgs e)
        {
            if (_isDragging && sender is HeaderKeyValueBlock block)
            {
                var rectangles = FindAllHeaderBlocks(this);
                foreach (var rectangle in rectangles)
                {
                    if (check_bottom(block, rectangle.Bounds.Bottom, rectangle.Bounds.Left))
                    {
                        rectangle.NextBlock = block;
                        block.NextBlockOf = rectangle;
                        block.SnapIntoPlace(rectangle.Bounds.Left);
                    }
                }
                
                _isDragging = false;
            }
            
            Dispatcher.UIThread.Post(() =>
            {
                SaveManager.SaveState(FindAllMoveableRectangles(this), bodyStart, headerStart);
            });
        }

        public static List<BaseRect> FindAllHeaderBlocks(Visual parent)
        {
            var rectangles = new List<BaseRect>();
    
            foreach (var child in parent.GetVisualChildren())
            {
                if (child is HeaderKeyValueBlock rect && !rect.isStatic)
                {
                    rectangles.Add(rect);
                }
                else if (child is HeaderStartBlock startBlock)
                {
                    rectangles.Add(startBlock);
                }
                else if (child is Visual visualChild)
                {
                    rectangles.AddRange(FindAllHeaderBlocks(visualChild));
                }
            }

            return rectangles;
        }

        private bool check_bottom(BaseRect block, double bottom, double left)
        {
            return block.Bounds.Top > bottom - 20 &&
                   block.Bounds.Top < bottom + 20 &&
                   block.Bounds.Left > left - 200 &&
                   block.Bounds.Left < left + 200;
        }

        private async void SendOutboundMessage(object? sender, RoutedEventArgs e)
        {
            String json = FetchJson();
            Dictionary<String, String> headers = FetchHeaders();
            SendHttpRequest(
                json,
                headers, 
                ConfigurationManager.AppSettings["outbound_address"] ?? "127.0.0.1",
                ConfigurationManager.AppSettings["outbound_port"] ?? "5050",
                ConfigurationManager.AppSettings["path"] ?? "");
        }

        private async void SendLocalMessage(object? sender, RoutedEventArgs e)
        {
            var headers = new Dictionary<string, string>
            {
                { "Accept", "application/json" },
                { "User-Agent", "MyHttpClient/1.0" }
            };
            
            String json = FetchJson();
            SendHttpRequest(json, headers, "127.0.0.1", "5050", "settings");
        }
        
        private String FetchJson()
        {
            StringBuilder sb = new StringBuilder();
            var currentBlock = bodyStart as BaseRect;
            sb.Append("{\n");
            while (currentBlock.NextBlock != null)
            {
                sb.Append(currentBlock.GetJsonValue(1));
                currentBlock = currentBlock.NextBlock;
            }
            sb.Append(currentBlock.GetJsonValue(1));
            sb.Append("}");
            
            Console.WriteLine(sb.ToString());

            return sb.ToString();
        }

        private Dictionary<String, String> FetchHeaders()
        {
            var headers = new Dictionary<String, String>();
            var currentBlock = bodyStart.NextBlock;
            while (currentBlock.NextBlock != null)
            {
                if (currentBlock is HeaderKeyValueBlock headerBlock)
                {
                    headers.Add(headerBlock.TextBox1.Text, headerBlock.TextBox2.Text);
                }
                currentBlock = currentBlock.NextBlock;
            }
            
            return headers;
        }

        private async void SendHttpRequest(String message, Dictionary<string, string> headers, String address, String port, String path)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage? response = null;
                
                try
                {
                    string url = $"http://{address}:{port}/{path}";
                    HttpContent content = new StringContent(message, Encoding.UTF8, "application/json");
                    
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    
                    client.DefaultRequestHeaders.Clear();
                    
                    foreach (var header in headers)
                    {
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                    
                    response = await client.PostAsync(url, content);
                    
                    response.EnsureSuccessStatusCode();
                    
                    string statusLine = $"HTTP/{response.Version} {(int)response.StatusCode} {response.ReasonPhrase}";
                    
                    var headersString = string.Join("\r\n", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"));
                    
                    string responseBody = await response.Content.ReadAsStringAsync();
                    
                    DateTime currentUtcTime = DateTime.UtcNow;
                    string fullResponse = $"Message recieved at: {currentUtcTime.ToString("yyyy-MM-dd HH:mm:ss.fff")}\n{statusLine}\r\n{headersString}\r\n\r\n{responseBody}";
                    
                    Console.WriteLine(responseBody);
                    ResponseTextBlock.Foreground = Brushes.Black;
                    ResponseTextBlock.Text = fullResponse;
                }
                catch (HttpRequestException e) when (response != null)
                {
                    string statusLine = $"HTTP/{response.Version} {(int)response.StatusCode} {response.ReasonPhrase}";
                    
                    var headersString = string.Join("\r\n", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"));
                    
                    string responseBody = await response.Content.ReadAsStringAsync();
                    
                    DateTime currentUtcTime = DateTime.UtcNow;
                    
                    string fullResponse = $"Message recieved at: {currentUtcTime.ToString("yyyy-MM-dd HH:mm:ss.fff")}\n{statusLine}\r\n{headersString}\r\n\r\n{responseBody}";
                    
                    ResponseTextBlock.Foreground = Brushes.Red;
                    ResponseTextBlock.Text = fullResponse;
                    Console.WriteLine($"Failed request:\n {fullResponse}");
                }
                catch (Exception e)
                {
                    ResponseTextBlock.Foreground = Brushes.Red;
                    ResponseTextBlock.Text = $"Unexpected error: {e.Message}";
                    Console.WriteLine($"Unexpected error: {e.Message}");
                }
            }
        }

        private void CreateKeyValueRect(object? sender, PointerPressedEventArgs e)
        {
            if (sender is KeyValueRect rect)
            {
                rect.PointerPressed -= CreateKeyValueRect;
                rect.isStatic = false;
                
                
                var keyVal = new KeyValueRect();
                keyVal.isStatic = true;
            
                keyVal.PointerPressed += OnPointerPressed;
                keyVal.PointerPressed += CreateKeyValueRect;
                keyVal.PointerMoved += OnPointerMoved;
                keyVal.PointerReleased += OnPointerReleased;
                
                Canvas.SetLeft(keyVal, rect.Bounds.Left);
                Canvas.SetTop(keyVal, rect.Bounds.Top);
            
                main.Children.Add(keyVal);
            }
        }
        
        private void CreateObjectRect(object? sender, PointerPressedEventArgs e)
        {
            if (sender is ObjectRect rect)
            {
                rect.PointerPressed -= CreateObjectRect;
                rect.isStatic = false;
                
                var objectRect = new ObjectRect();
                objectRect.isStatic = true;
                objectRect.PointerPressed += OnPointerPressed;
                objectRect.PointerPressed += CreateObjectRect;
                objectRect.PointerMoved += OnPointerMoved;
                objectRect.PointerReleased += OnPointerReleased;

                Canvas.SetLeft(objectRect, rect.Bounds.Left);
                Canvas.SetTop(objectRect, rect.Bounds.Top);

                main.Children.Add(objectRect);
            }
        }

        private void CreateArrayRect(object? sender, PointerPressedEventArgs e)
        {
            if (sender is ArrayRect rect)
            {
                rect.PointerPressed -= CreateArrayRect;
                rect.isStatic = false;
                
                var arrayRect = new ArrayRect();
                arrayRect.isStatic = true;
                arrayRect.PointerPressed += OnPointerPressed;
                arrayRect.PointerPressed += CreateArrayRect;
                arrayRect.PointerMoved += OnPointerMoved;
                arrayRect.PointerReleased += OnPointerReleased;

                Canvas.SetLeft(arrayRect, rect.Bounds.Left);
                Canvas.SetTop(arrayRect, rect.Bounds.Top);

                main.Children.Add(arrayRect);
            }
        }
        
        private void CreateHeaderKeyValueRect(object? sender, PointerPressedEventArgs e)
        {
            if (sender is HeaderKeyValueBlock rect)
            {
                rect.PointerPressed -= CreateHeaderKeyValueRect;
                rect.isStatic = false;
                
                var headerBlock = new HeaderKeyValueBlock();
                headerBlock.isStatic = true;
                headerBlock.PointerPressed += OnPointerPressed;
                headerBlock.PointerPressed += CreateHeaderKeyValueRect;
                headerBlock.PointerMoved += OnPointerMoved;
                headerBlock.PointerReleased += OnPointerReleased;

                Canvas.SetLeft(headerBlock, rect.Bounds.Left);
                Canvas.SetTop(headerBlock, rect.Bounds.Top);

                main.Children.Add(headerBlock);
            }
        }

        private void LoadState()
        {
            var saveState = SaveManager.LoadState();

            foreach (var uiElement in saveState.Elements)
            {
                try
                {
                    Console.WriteLine($"Deserializing UI Element with Type: {uiElement.TypeName}");

                    var y = uiElement.GetElementType() switch
                    {
                        Type t when t == typeof(StartRect) => new StartRect(uiElement),
                        Type t when t == typeof(KeyValueRect) => new KeyValueRect(uiElement),
                        Type t when t == typeof(ObjectRect) => new ObjectRect(uiElement),
                        Type t when t == typeof(ArrayRect) => new ArrayRect(uiElement),
                        Type t when t == typeof(HeaderStartBlock) => new HeaderStartBlock(uiElement),
                        Type t when t == typeof(HeaderKeyValueBlock) => new HeaderKeyValueBlock(uiElement),
                        _ => new BaseRect()
                    };
                    
                    y.PointerPressed += OnPointerPressed;
                    y.PointerMoved += OnPointerMoved;
                    if (uiElement.GetElementType() == typeof(HeaderKeyValueBlock))
                    {
                        y.PointerReleased += OnPointerReleasedHeader;
                    }
                    else
                    {
                        y.PointerReleased += OnPointerReleased;
                    }
                    
                    Canvas.SetTop(y, uiElement.PositionY);
                    Canvas.SetLeft(y, uiElement.PositionX);
                    
                    main.Children.Add(y);
                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deserializing UI Element: {ex.Message}");
                }
            }
            
            Dispatcher.UIThread.Post(() =>
            {
                var rects = FindAllMoveableRectangles(this);

                foreach (var rect in rects)
                {
                    Console.WriteLine(rect.Id + ": " + rect._saveUiElement.Next + " - " + rect._saveUiElement.NextOf + " - " + rect._saveUiElement.PartOf);
                    if (rect._saveUiElement.Next != null) rect.NextBlock = FindRectBasedOnId(rect._saveUiElement.Next, rects);
                    if (rect._saveUiElement.NextOf != null) rect.NextBlockOf = FindRectBasedOnId(rect._saveUiElement.NextOf, rects);
                    if (rect._saveUiElement.PartOf != null) rect.PartOf = FindBaseContainerById(rect._saveUiElement.PartOf, rects);

                    switch (rect)
                    {
                        case KeyValueRect keyValueRect:
                            keyValueRect.TextBox1.Text = keyValueRect._saveUiElement.Value1;
                            keyValueRect.TextBox2.Text = keyValueRect._saveUiElement.Value2;
                            break;
                        case ObjectRect objectRect:
                            objectRect.TextBox.Text = objectRect._saveUiElement.Value1;
                            foreach (var guid in objectRect._saveUiElement.Contains)
                            {
                                var block = FindRectBasedOnId(guid, rects);
                                if (block != null) objectRect.Properties.Add(block);
                            }
                            break;
                        case ArrayRect arrayRect:
                            arrayRect.TextBox.Text = arrayRect._saveUiElement.Value1;
                            foreach (var guid in arrayRect._saveUiElement.Contains)
                            {
                                var block = FindRectBasedOnId(guid, rects);
                                if (block != null) arrayRect.Properties.Add(block);
                            }
                            break;
                        default:
                            break;
                    }
                }

                var startNextBlock = FindRectBasedOnId(saveState.StartBlockNext, rects);
                bodyStart.NextBlock = startNextBlock;
                if (bodyStart.NextBlock != null) bodyStart.NextBlock.NextBlockOf = bodyStart;

                foreach (var rect in rects)
                {
                    if (rect is BaseContainer bc)
                    {
                        bc.Resize();
                    }
                }
            });
        }


        private BaseContainer? FindBaseContainerById(Guid? id, List<BaseRect> rects)
        {
            if (id == null) return null;
            
            foreach (var rect in rects)
            {
                if (rect is BaseContainer container)
                {
                    if (id == container.Id)
                    {
                        return container;
                    }
                }
            }
            return null;
        }

        private BaseRect? FindRectBasedOnId(Guid? id, List<BaseRect> rects)
        {
            if (id == null) return null;
            
            foreach (var rect in rects)
            {
                if (id == rect.Id)
                {
                    return rect;
                }
            }
            return null;
        }
        
        private void StartServer_Click(object? sender, RoutedEventArgs e)
        {
            if (!HttpServer._isRunning)
            {
                HttpServer.InitServer(this);
                Console.WriteLine("Server started.");
            }
            else
            {
                Console.WriteLine("Server is already running.");
            }
            
        }
        
        private void StopServer_Click(object? sender, RoutedEventArgs e)
        {
            if (HttpServer._isRunning)
            {
                HttpServer.StopServer();
                Console.WriteLine("Server stopped.");
            }
            else
            {
                Console.WriteLine("Server is already stopped.");
            }
        }

        private void DiscardConfig(object? sender, RoutedEventArgs e)
        {
            SaveManager.DiscardSaveStates();
            foreach (var rect in FindAllMoveableRectangles(this))
            {
                main.Children.Remove(rect);
            }
        }

        private void SaveConfig(object? sender, RoutedEventArgs e)
        {
            SaveManager.SaveState(FindAllMoveableRectangles(this), bodyStart, headerStart);
        }
    }
    
    
}