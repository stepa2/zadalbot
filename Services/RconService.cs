using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RconSharp;

namespace ZadalBot.Services
{
    public sealed class RconService
    {
        private const string GmodCommandName = "zadalbot_fetch";

        private int _isConnecting;
        private RconClient _client;
        private readonly ZadalBotConfig _config;

        public RconService(ZadalBotConfig config)
        {
            _config = config;
        }

        private async Task<bool> TryConnect()
        {
            // Due to some bug ConnectAsync throws SocketException only once, so we recreate RCON client
            _client = RconClient.Create(new SocketChannel(_config.GameHostname, _config.GamePort));
            _client.ConnectionClosed += DisconnectHandler;

            try
            {
                await _client.ConnectAsync();
            }
            catch (SocketException e)
            {
                Console.WriteLine("RCON > connection failed with exception: {0}", e.Message);
                return false;
            }

            try
            {
                if (!await _client.AuthenticateAsync(_config.GameRconPassword))
                {
                    Console.WriteLine("RCON > authentication failed");
                    return false;
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("RCON > authentication failed with exception: {0}", e.Message);
                return false;
            }

            Console.WriteLine("RCON > connected");
            _isConnecting = 0;

            if (OnConnected != null) await OnConnected();

            return true;
        }

        public async Task Connect()
        {
            if (Interlocked.Exchange(ref _isConnecting, 1) == 1)
            {
                Console.WriteLine("RCON > double connection would occur");
                return;
            }

            await Task.Run(async () =>
            {
                while (!await TryConnect())
                {
                    await Task.Delay(TimeSpan.FromSeconds(4));
                    Console.WriteLine("RCON > attempt to reconnect");
                }
            });

        }

        private async void DisconnectHandler()
        {
            Console.WriteLine("RCON > disconnected");
            if (OnDisconnected != null) await OnDisconnected();
            await Task.Delay(TimeSpan.FromSeconds(4));
            await Connect();
        }

        private async Task<string> SendCommand(string command)
        {
            if (_client == null)
            {
                Console.WriteLine("RCON > not initialized yet");
                return null;
            }

            try
            {
                return await _client.ExecuteCommandAsync(command);
            }
            catch (SocketException e)
            {
                Console.WriteLine("RCON > got exception executing fetch command: {0}", e.Message);
                await Connect();
            }

            return null;
        }

        private Task<string> RunSendCommand(string command) =>
            Task.Run(async () =>
            {
                var sendCommand = SendCommand(command);
                var anyTask = await Task.WhenAny(
                    sendCommand, 
                    Task.Delay(TimeSpan.FromSeconds(6)).ContinueWith<string>(task => null));

                if (anyTask == sendCommand)
                    return await anyTask;

                Console.WriteLine("RCON > command execution timeout");
                if (OnDisconnected != null) await OnDisconnected();
                await Connect();
                return null;
            });

        public Task SendFetchCommand() => RunSendCommand(GmodCommandName);

        public Task<string> SendStatusCommand() => RunSendCommand("status");

        public event Func<Task> OnConnected;
        public event Func<Task> OnDisconnected;
    }
}