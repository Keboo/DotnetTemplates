using BlazorApp.Core.Auth;
using BlazorApp.Core.Hubs;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace BlazorApp.Components.QA;

public sealed class RoomHubClient : IAsyncDisposable
{
    private readonly HubConnection _hubConnection;

    private Guid? _roomId;

    public event Func<QuestionDto, Task>? QuestionSubmitted;
    public event Func<QuestionDto, Task>? QuestionApproved;
    public event Func<QuestionDto, Task>? QuestionAnswered;
    public event Func<QuestionDto?, Task>? CurrentQuestionChanged;
    public event Func<Guid, Task>? QuestionDeleted;

    public RoomHubClient(NavigationManager navigationManager, string? accessToken)
    {        
        Uri hubUri = navigationManager.ToAbsoluteUri("/hubs/room");
        if (hubUri.Host.EndsWith(".dev.localhost"))
        {
            UriBuilder builder = new(hubUri);
            builder.Host = "localhost";
            hubUri = builder.Uri;
        }
        
        // Append access token to query string for JWT middleware
        if (!string.IsNullOrEmpty(accessToken))
        {
            var builder = new UriBuilder(hubUri);
            builder.Query = $"access_token={Uri.EscapeDataString(accessToken)}";
            hubUri = builder.Uri;
        }
        
        // Create connection
        var connectionBuilder = new HubConnectionBuilder()
            .WithUrl(hubUri)
            .WithAutomaticReconnect();

        _hubConnection = connectionBuilder.Build();

        _hubConnection.On<QuestionDto>("QuestionSubmitted", async question =>
        {
            if (QuestionSubmitted != null)
            {
                await QuestionSubmitted.Invoke(question);
            }
        });

        _hubConnection.On<QuestionDto>("QuestionApproved", async question =>
        {
            if (QuestionApproved != null)
            {
                await QuestionApproved.Invoke(question);
            }
        });

        _hubConnection.On<QuestionDto>("QuestionAnswered", async question =>
        {
            if (QuestionAnswered != null)
            {
                await QuestionAnswered.Invoke(question);
            }
        });

        _hubConnection.On<QuestionDto?>("CurrentQuestionChanged", async question =>
        {
            if (CurrentQuestionChanged != null)
            {
                await CurrentQuestionChanged.Invoke(question);
            }
        });

        _hubConnection.On<Guid>("QuestionDeleted", async questionId =>
        {
            if (QuestionDeleted != null)
            {
                await QuestionDeleted.Invoke(questionId);
            }
        });
    }

    public async Task JoinRoomAsOwnerAsync(Guid roomId)
    {
        if (_roomId.HasValue)
        {
            throw new InvalidOperationException("Already joined a room.");
        }
        _roomId = roomId;
        await EnsureConnection();
        await _hubConnection.InvokeAsync("JoinRoomAsOwner", roomId.ToString());
    }

    public async Task JoinRoomAsync(Guid roomId)
    {
        if (_roomId.HasValue)
        {
            throw new InvalidOperationException("Already joined a room.");
        }
        _roomId = roomId;
        await EnsureConnection();
        await _hubConnection.InvokeAsync("JoinRoom", roomId.ToString());
    }

    private async Task EnsureConnection()
    {
        if (_hubConnection.State  == HubConnectionState.Disconnected)
        {
            await _hubConnection.StartAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection.State == HubConnectionState.Connected && _roomId is { } roomId)
        {
            await _hubConnection.InvokeAsync("LeaveRoom", roomId.ToString());
        }
        await _hubConnection.DisposeAsync();
    }
}
