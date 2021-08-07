using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RconSharp;

namespace ZadalBot.Services
{
    public class RconService
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

        private async Task SendFetchCommandImpl()
        {
            if (_client == null)
            {
                Console.WriteLine("RCON > not initialized yet");
                return;
            }

            try
            {
                var result = await _client.ExecuteCommandAsync(GmodCommandName);
                if (!string.IsNullOrEmpty(result))
                    Console.WriteLine("RCON > {0} response : {1}", GmodCommandName, result);
            }
            catch (SocketException e)
            {
                Console.WriteLine("RCON > got exception executing fetch command: {0}", e.Message);
                await Connect();
            }
        }

        public Task SendFetchCommand() =>
            Task.Run(async () =>
            {
                var task = SendFetchCommandImpl();

                if (await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(6))) != task)
                {
                    Console.WriteLine("RCON > command execution timeout");

                    if (OnDisconnected != null) await OnDisconnected();
                    await Connect();
                }
            });

        public event Func<Task> OnConnected;
        public event Func<Task> OnDisconnected;
    }
}