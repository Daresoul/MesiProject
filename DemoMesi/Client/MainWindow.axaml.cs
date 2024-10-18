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
        
        private Canvas? _main;
        private StartRect? _bodyStart;
        private HeaderStartBlock? _headerStart;
        private SaveManager _saveManager = null!;
        private TextBlock? _responseTextBlock;

        public MainWindow()
        {
            InitializeComponent();
            
            _main = this.FindControl<Canvas>("MainCanvas");
            _responseTextBlock = this.FindControl<TextBlock>("ResponseTextBox");
            var initialKeyValue = this.FindControl<KeyValueRect>("InitialKeyValueRect");
            if (initialKeyValue != null) initialKeyValue.PointerPressed += CreateKeyValueRect;
            
            var initialObject = this.FindControl<ObjectRect>("InitialObjectRect");
            if (initialObject != null) initialObject.PointerPressed += CreateObjectRect;
            
            var initialArray = this.FindControl<ArrayRect>("InitialArrayRect");
            if (initialArray != null) initialArray.PointerPressed += CreateArrayRect;
            
            var initialHeaderKeyValueRect = this.FindControl<HeaderKeyValueBlock>("InitialHeaderKeyValueRect");
            if (initialHeaderKeyValueRect != null) initialHeaderKeyValueRect.PointerPressed += CreateHeaderKeyValueRect;

            _bodyStart = new StartRect();
            _bodyStart.IsStatic = false;
            
            Canvas.SetLeft(_bodyStart, 350);
            Canvas.SetTop(_bodyStart, 25);
            
            _main?.Children.Add(_bodyStart);
            
            _headerStart = new HeaderStartBlock();
            _headerStart.IsStatic = false;
            
            Canvas.SetLeft(_headerStart, 700);
            Canvas.SetTop(_headerStart, 25);
            
            _main?.Children.Add(_headerStart);

            using (var connection = new SqliteConnection(HttpServer.DbConnectionString))
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

            if (_responseTextBlock != null) _responseTextBlock.Text = "All Stats: \n" + sb.ToString();
        }

        public void UpdateLastRequest(String request, DateTime time)
        {
            if (_responseTextBlock != null) _responseTextBlock.Text = $"{time:yyyy-MM-dd HH:mm:ss.fff}:\n{request}";
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            _main = this.FindControl<Canvas>("MainCanvas");
            _saveManager = new SaveManager();
            LoadState();
        }
        
        
        
        public static List<BaseRect> FindAllRectangles(Visual parent)
        {
            var rectangles = new List<BaseRect>();
    
            foreach (var child in parent.GetVisualChildren())
            {
                if (child is BaseRect rect && !rect.IsStatic)
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
                if (child is BaseRect rect && !rect.IsStatic && child.GetType() != typeof(StartRect) && child.GetType() != typeof(HeaderStartBlock))
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
                _saveManager.SaveState(FindAllMoveableRectangles(this), _bodyStart!, _headerStart!);
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
                _saveManager.SaveState(FindAllMoveableRectangles(this), _bodyStart!, _headerStart!);
            });
        }

        public static List<BaseRect> FindAllHeaderBlocks(Visual parent)
        {
            var rectangles = new List<BaseRect>();
    
            foreach (var child in parent.GetVisualChildren())
            {
                if (child is HeaderKeyValueBlock rect && !rect.IsStatic)
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

        private void SendOutboundMessage(object? sender, RoutedEventArgs e)
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

        private void SendLocalMessage(object? sender, RoutedEventArgs e)
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
            var currentBlock = _bodyStart as BaseRect;
            sb.Append("{\n");
            while (currentBlock?.NextBlock != null)
            {
                sb.Append(currentBlock.GetJsonValue(1));
                currentBlock = currentBlock.NextBlock;
            }
            sb.Append(currentBlock?.GetJsonValue(1));
            sb.Append("}");
            
            Console.WriteLine(sb.ToString());

            return sb.ToString();
        }

        private Dictionary<String, String> FetchHeaders()
        {
            var headers = new Dictionary<String, String>();
            var currentBlock = _bodyStart!.NextBlock;
            while (currentBlock?.NextBlock != null)
            {
                if (currentBlock is HeaderKeyValueBlock headerBlock)
                {
                    headers.Add(headerBlock.TextBox1.Text!, headerBlock.TextBox2.Text!);
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
                    _responseTextBlock!.Foreground = Brushes.Black;
                    _responseTextBlock.Text = fullResponse;
                }
                catch (HttpRequestException) when (response != null)
                {
                    string statusLine = $"HTTP/{response.Version} {(int)response.StatusCode} {response.ReasonPhrase}";
                    
                    var headersString = string.Join("\r\n", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"));
                    
                    string responseBody = await response.Content.ReadAsStringAsync();
                    
                    DateTime currentUtcTime = DateTime.UtcNow;
                    
                    string fullResponse = $"Message recieved at: {currentUtcTime.ToString("yyyy-MM-dd HH:mm:ss.fff")}\n{statusLine}\r\n{headersString}\r\n\r\n{responseBody}";
                    
                    _responseTextBlock!.Foreground = Brushes.Red;
                    _responseTextBlock.Text = fullResponse;
                    Console.WriteLine($"Failed request:\n {fullResponse}");
                }
                catch (Exception e)
                {
                    _responseTextBlock!.Foreground = Brushes.Red;
                    _responseTextBlock.Text = $"Unexpected error: {e.Message}";
                    Console.WriteLine($"Unexpected error: {e.Message}");
                }
            }
        }

        private void CreateKeyValueRect(object? sender, PointerPressedEventArgs e)
        {
            if (sender is KeyValueRect rect)
            {
                rect.PointerPressed -= CreateKeyValueRect;
                rect.IsStatic = false;
                
                
                var keyVal = new KeyValueRect();
                keyVal.IsStatic = true;
            
                keyVal.PointerPressed += OnPointerPressed;
                keyVal.PointerPressed += CreateKeyValueRect;
                keyVal.PointerMoved += OnPointerMoved;
                keyVal.PointerReleased += OnPointerReleased;
                
                Canvas.SetLeft(keyVal, rect.Bounds.Left);
                Canvas.SetTop(keyVal, rect.Bounds.Top);
            
                _main?.Children.Add(keyVal);
            }
        }
        
        private void CreateObjectRect(object? sender, PointerPressedEventArgs e)
        {
            if (sender is ObjectRect rect)
            {
                rect.PointerPressed -= CreateObjectRect;
                rect.IsStatic = false;
                
                var objectRect = new ObjectRect();
                objectRect.IsStatic = true;
                objectRect.PointerPressed += OnPointerPressed;
                objectRect.PointerPressed += CreateObjectRect;
                objectRect.PointerMoved += OnPointerMoved;
                objectRect.PointerReleased += OnPointerReleased;

                Canvas.SetLeft(objectRect, rect.Bounds.Left);
                Canvas.SetTop(objectRect, rect.Bounds.Top);

                _main?.Children.Add(objectRect);
            }
        }

        private void CreateArrayRect(object? sender, PointerPressedEventArgs e)
        {
            if (sender is ArrayRect rect)
            {
                rect.PointerPressed -= CreateArrayRect;
                rect.IsStatic = false;
                
                var arrayRect = new ArrayRect();
                arrayRect.IsStatic = true;
                arrayRect.PointerPressed += OnPointerPressed;
                arrayRect.PointerPressed += CreateArrayRect;
                arrayRect.PointerMoved += OnPointerMoved;
                arrayRect.PointerReleased += OnPointerReleased;

                Canvas.SetLeft(arrayRect, rect.Bounds.Left);
                Canvas.SetTop(arrayRect, rect.Bounds.Top);

                _main?.Children.Add(arrayRect);
            }
        }
        
        private void CreateHeaderKeyValueRect(object? sender, PointerPressedEventArgs e)
        {
            if (sender is HeaderKeyValueBlock rect)
            {
                rect.PointerPressed -= CreateHeaderKeyValueRect;
                rect.IsStatic = false;
                
                var headerBlock = new HeaderKeyValueBlock();
                headerBlock.IsStatic = true;
                headerBlock.PointerPressed += OnPointerPressed;
                headerBlock.PointerPressed += CreateHeaderKeyValueRect;
                headerBlock.PointerMoved += OnPointerMoved;
                headerBlock.PointerReleased += OnPointerReleased;

                Canvas.SetLeft(headerBlock, rect.Bounds.Left);
                Canvas.SetTop(headerBlock, rect.Bounds.Top);

                _main?.Children.Add(headerBlock);
            }
        }

        private void LoadState()
        {
            var saveState = _saveManager.LoadState();

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
                    
                    _main?.Children.Add(y);
                    
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
                    //Console.WriteLine(rect.Id + ": " + rect.SaveUiElement.Next + " - " + rect.SaveUiElement.NextOf + " - " + rect.SaveUiElement.PartOf);
                    if (rect.SaveUiElement!.Next != null) rect.NextBlock = FindRectBasedOnId(rect.SaveUiElement.Next, rects);
                    if (rect.SaveUiElement!.NextOf != null) rect.NextBlockOf = FindRectBasedOnId(rect.SaveUiElement.NextOf, rects);
                    if (rect.SaveUiElement!.PartOf != null) rect.PartOf = FindBaseContainerById(rect.SaveUiElement.PartOf, rects);

                    switch (rect)
                    {
                        case KeyValueRect keyValueRect:
                            keyValueRect.TextBox1.Text = keyValueRect.SaveUiElement!.Value1;
                            keyValueRect.TextBox2.Text = keyValueRect.SaveUiElement!.Value2;
                            break;
                        case ObjectRect objectRect:
                            objectRect.TextBox.Text = objectRect.SaveUiElement!.Value1;
                            foreach (var guid in objectRect.SaveUiElement!.Contains)
                            {
                                var block = FindRectBasedOnId(guid, rects);
                                if (block != null) objectRect.Properties.Add(block);
                            }
                            break;
                        case ArrayRect arrayRect:
                            arrayRect.TextBox.Text = arrayRect.SaveUiElement!.Value1;
                            foreach (var guid in arrayRect.SaveUiElement!.Contains)
                            {
                                var block = FindRectBasedOnId(guid, rects);
                                if (block != null) arrayRect.Properties.Add(block);
                            }
                            break;
                        case HeaderKeyValueBlock headerBlock:
                            headerBlock.TextBox1.Text = headerBlock.SaveUiElement!.Value1;
                            headerBlock.TextBox2.Text = headerBlock.SaveUiElement!.Value2;
                            break;
                        default:
                            break;
                    }
                }

                var startNextBlock = FindRectBasedOnId(saveState.StartBlockNext, rects);
                _bodyStart!.NextBlock = startNextBlock;
                if (_bodyStart.NextBlock != null) _bodyStart.NextBlock.NextBlockOf = _bodyStart;

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
            if (!HttpServer.IsRunning)
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
            if (HttpServer.IsRunning)
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
            _saveManager.DiscardSaveStates();
            foreach (var rect in FindAllMoveableRectangles(this))
            {
                _main!.Children.Remove(rect);
            }
        }

        private void SaveConfig(object? sender, RoutedEventArgs e)
        {
            _saveManager.SaveState(FindAllMoveableRectangles(this), _bodyStart!, _headerStart!);
        }
    }
    
    
}