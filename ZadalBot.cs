using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ZadalBot.Services;

namespace ZadalBot
{
    public sealed class ZadalBot : IDisposable, IServiceProvider
    {
        private readonly RconService _rcon;
        private readonly DiscordService _discord;
        private readonly HttpService _http;
        private readonly DataService _data;
        private readonly QueryService _query;


        public ZadalBot(ZadalBotConfig config)
        {
            _data = new DataService();
            _rcon = new RconService(config);
            _discord = new DiscordService(config, this);
            _http = new HttpService(config);
            _query = new QueryService(config);

            _http.DataProvider += async () => new JArray(await _data.GetDataFromChat());
            _http.OnDataReceived += async data => await _data.HandleDataFromGame(data);

            _discord.OnMessage += async msg =>
            {
                _data.StoreChatMessage(msg);
                await _rcon.SendFetchCommand();
            };

            _rcon.OnConnected += async () => await _discord.SendServerStatus(true);
            _rcon.OnDisconnected += async () => await _discord.SendServerStatus(false);

            _data.OnGotGameMessage += async msg => await _discord.SendMessage(msg);
            _data.OnGotPlayerConnectionStatus += async status => await _discord.SendPlayerConnectionStatus(status);
            _data.OnPlayerDisconnected += async data => await _discord.SendPlayerDisconnected(data);

        }



        public async Task RunAsync()
        {
            await Task.WhenAll(
                _rcon.Connect(),
                _discord.Start(),
                _http.Startup(),
                _query.Connect()
                );

            await Task.Delay(-1); // Lock
        }

        public void Dispose()
        {
            _discord?.Dispose();
            _http?.Dispose();
        }

        public object GetService(Type type)
        {
            if (type == typeof(DataService))
                return _data;
            if (type == typeof(DiscordService))
                return _discord;
            if (type == typeof(HttpService))
                return _http;
            if (type == typeof(QueryService))
                return _query;
            if (type == typeof(RconService))
                return _rcon;

            return null;
        }
    }
}