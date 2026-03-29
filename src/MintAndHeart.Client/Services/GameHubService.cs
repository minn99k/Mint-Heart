using Microsoft.AspNetCore.SignalR.Client;

namespace MintAndHeart.Client.Services;

// This service is used to connect to the server and send/receive messages
public class GameHubService{
    // This is the connection to the server
    private readonly HubConnection _connection;


    public GameHubService(string hubUrl){
        _connection = new HubConnectionBuilder() // Build the connection to the server
        .WithUrl(hubUrl) // Set the URL of the server
        .WithAutomaticReconnect() // Automatically reconnect if the connection is lost
        .Build(); // Build the connection
    }

    public async Task StartAsync(){
        await _connection.StartAsync();
        Console.WriteLine("Connection started");
    }

    public void OnPong(Action<string> handler){
    _connection.On<string>("Pong", handler);
    }

// 서버의 Ping() 메서드를 호출
    public async Task SendPingAsync(){
        await _connection.InvokeAsync("Ping");
    }   
}