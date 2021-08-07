using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ZadalBot.Services;

namespace ZadalBot
{
    public sealed class ZadalBot
    {
        private readonly RconService _rcon;
        private readonly DiscordService _discord;
        private readonly HttpService _http;
        private readonly DataService _data;

        public ZadalBot(ZadalBotConfig config)
        {
            _data = new DataService();
            _rcon = new RconService(config);
            _discord = new DiscordService(config);
            _http = new HttpService(config);

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
                _discord.Connect(),
                _http.Startup()
                );

            await Task.Delay(-1); // Lock
        }
    }
}