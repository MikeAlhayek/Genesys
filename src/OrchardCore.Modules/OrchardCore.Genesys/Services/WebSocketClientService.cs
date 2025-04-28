using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CloudSolutions.Genesys.BackgroundTasks;

public class WebSocketClientService
{
    private ClientWebSocket _clientWebSocket;
    private readonly Uri _uri;
    private readonly int _reconnectDelaySeconds;
    private readonly int _maxReconnectAttempts;
    private readonly ILogger _logger;
    private readonly CancellationToken _cancellationToken;

    private int _reconnectAttempts;

    public WebSocketClientService(
        Uri uri,
        ILogger logger,
        CancellationToken cancellationToken,
        int reconnectDelaySeconds = 5,
        int maxReconnectAttempts = 10)
    {
        _uri = uri;
        _logger = logger;
        _cancellationToken = cancellationToken;
        _reconnectDelaySeconds = reconnectDelaySeconds;
        _maxReconnectAttempts = maxReconnectAttempts;
        _reconnectAttempts = 0;
    }

    public async Task StartAsync()
    {
        _clientWebSocket = new ClientWebSocket();
        await ConnectAsync();
    }

    public async Task StopAsync()
    {
        if (_clientWebSocket.State == WebSocketState.Open)
        {
            await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        }
    }

    private async Task ConnectAsync()
    {
        while (!_cancellationToken.IsCancellationRequested && _reconnectAttempts < _maxReconnectAttempts)
        {
            try
            {
                _logger.LogInformation("Attempting to connect... (Attempt {CurrentAttempt} out of {MaxReconnectAttempts})", _reconnectAttempts + 1, _maxReconnectAttempts);
                _clientWebSocket = new ClientWebSocket();
                await _clientWebSocket.ConnectAsync(_uri, _cancellationToken);

                _logger.LogInformation("Connected to WebSocket server.");
                _reconnectAttempts = 0; // Reset attempts on successful connection
                await ReceiveAsync();
            }
            catch (Exception ex)
            {
                _reconnectAttempts++;
                Console.WriteLine($"Connection error: {ex.Message}. Retrying in {_reconnectDelaySeconds} seconds... ({_reconnectAttempts}/{_maxReconnectAttempts})");

                if (_reconnectAttempts >= _maxReconnectAttempts)
                {
                    _logger.LogError("Max reconnect attempts reached. Stopping reconnection attempts.");
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(_reconnectDelaySeconds), _cancellationToken);
            }
        }
    }

    private async Task ReceiveAsync()
    {
        var buffer = new byte[1024];

        while (!_cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await _clientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogDebug("WebSocket closed by server. Reconnecting...");
                    await ConnectAsync();
                    break;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                _logger.LogDebug("Received message from Genesys: {Message}", message);

                var eventData = JsonSerializer.Deserialize<GenesysEvent>(message);

                if (eventData == null)
                {
                    return;
                }

                switch (eventData.EventType)
                {
                    case "conversation.start":
                        // A new conversation has started
                        _logger.LogInformation("A new conversation has started.");
                        // Here you can store the conversation ID or trigger further actions
                        break;

                    case "conversation.transcript":
                        // Real-time transcript event
                        _logger.LogInformation("Transcript update: {EventData}", eventData.EventData);
                        // Handle the transcript data (e.g., store it, send it to a processing service)
                        break;

                    default:
                        _logger.LogInformation("Unhandled event: {EventData}", eventData.EventType);
                        break;
                }


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving message. Reconnecting...");
                await ConnectAsync();
                break;
            }
        }
    }

    public async Task SendMessageAsync(string message)
    {
        if (_clientWebSocket.State == WebSocketState.Open)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            await _clientWebSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, _cancellationToken);
        }
        else
        {
            _logger.LogDebug("WebSocket is not connected.");
        }
    }

    public sealed class GenesysEvent
    {
        public string EventType { get; set; }
        public string EventData { get; set; }
    }
}
