using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace ZadalBot.Services
{
    public class DiscordService
    {
        private readonly DiscordSocketClient _client;
        private readonly ZadalBotConfig _config;
        private SocketTextChannel _chatChannel;

        public DiscordService(ZadalBotConfig config)
        {
            _config = config;
            _client = new DiscordSocketClient(new DiscordSocketConfig());
            _client.Log += DiscordLogHandler;
            _client.MessageReceived += async msg => await MessageHandler(msg);
            _client.MessageUpdated += async (_, msgNew, _) => await MessageHandler(msgNew);
            _client.Ready += GetGuildAndChannel;
        }

        private Task DiscordLogHandler(LogMessage msg)
        {
            Console.WriteLine("Discord > {0} {1} > {2}", msg.Source, msg.Severity, msg.Message);
            if (msg.Exception != null) Console.WriteLine(msg.Exception);
            return Task.CompletedTask;
        }

        private async Task MessageHandler(SocketMessage msg)
        {
            if (msg.Author.IsBot)
                return;


            if (OnMessage != null) await OnMessage(new DataService.ChatMessage
            {
                Sender = msg.Author.Username,
                Contents = msg.Content,
                Attachments = msg.Attachments.Select(att => att.Url).ToList()
            });
        }

        public event Func<DataService.ChatMessage, Task> OnMessage;


        public async Task Connect() =>
            await Task.Run(async () =>
            {
                await _client.LoginAsync(TokenType.Bot, _config.DiscordToken);
                await _client.StartAsync();
            });

        private Task GetGuildAndChannel()
        {
            var guild = _client.GetGuild(_config.DiscordGameGuild);

            if (guild == null)
            {
                Console.WriteLine("FATAL ERROR > Discord > Guild id is invalid!");
                Environment.Exit(-1);
            }

            if (guild.GetChannel(_config.DiscordGameChannel) is SocketTextChannel channel)
                _chatChannel = channel;
            else
            {
                Console.WriteLine("FATAL ERROR > Discord > Channel is not a valid guild text channel!");
                Environment.Exit(-1);
            }

            return _chatChannel.SendMessageAsync(embed: new EmbedBuilder()
                .WithColor(Color.Green)
                .WithDescription("Бот запущен")
                .Build());
        }

        public Task SendMessage(DataService.GameMessage msg) =>
            Task.Run(async () =>
            {
                await Util.WaitWhile(() => _chatChannel == null, TimeSpan.FromMilliseconds(20));
                

                await _chatChannel.SendMessageAsync(embed: new EmbedBuilder()
                    .WithAuthor(msg.Sender)
                    .WithDescription(msg.Contents).Build());
            });

        public Task SendPlayerConnectionStatus(DataService.PlayerConnectionStatus status) =>
            Task.Run(async () =>
            {
                await Util.WaitWhile(() => _chatChannel == null, TimeSpan.FromMilliseconds(20));

                await _chatChannel.SendMessageAsync(embed: new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithAuthor(status.Player)
                    .WithDescription(status.Status switch
                    {
                        DataService.ConnectionStatusEnum.ConnectedToServer => "Игрок начал подключаться",
                        DataService.ConnectionStatusEnum.LoadedServerside => "Игрок почти подключился",
                        DataService.ConnectionStatusEnum.LoadedClientside => "Игрок подключился",
                        _ => $"<ошибка в данных ({status.Status})>"
                    }).Build());
            });

        public Task SendPlayerDisconnected(DataService.PlayerDisconnectedData data) =>
            Task.Run(async () =>
            {
                await Util.WaitWhile(() => _chatChannel == null, TimeSpan.FromMilliseconds(20));


                await _chatChannel.SendMessageAsync(embed: new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithAuthor(data.Player)
                    .WithTitle("Игрок отсоединился")
                    .WithDescription(data.Reason).Build());
            });

        public Task SendServerStatus(bool active) =>
            Task.Run(async () =>
            {
                await Util.WaitWhile(() => _chatChannel == null, TimeSpan.FromMilliseconds(20));

                await _chatChannel.SendMessageAsync(embed: new EmbedBuilder()
                    .WithTitle("Сервер " + (active ? "запущен" : "выключен"))
                    .WithColor(active ? Color.Green : Color.Red)
                    .Build());
            });
    }
}