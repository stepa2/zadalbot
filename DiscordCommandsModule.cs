using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using ZadalBot.Services;

namespace ZadalBot
{
    public sealed class DiscordCommandsModule: ModuleBase<SocketCommandContext>
    {
        public QueryService QueryService { get; set; }
        public RconService RconService { get; set; }

        [Command("status")]
        [Summary("Выводит состояние сервера")]
        [Alias("статус")]
        public async Task StatusAsync()
        {
            var query = await QueryService.Query();

            if (query == null)
            {
                await ReplyAsync(message: null, isTTS: false, embed: new EmbedBuilder
                {
                    Color = Color.Red,
                    Description = "Сервер выключен"
                }.Build());
            }
            else
            {
                //var status = await RconService.SendStatusCommand();

                var players = query.Players.Count != 0 ?
                    string.Join('\n', query.Players.Select(ply => $"{ply.Name ?? "<Подключается>"}")) :
                    " ";

                await ReplyAsync(message: null, isTTS: false,
                    new EmbedBuilder()
                        .WithColor(Color.Green)
                        .WithCurrentTimestamp()
                        .WithDescription(query.Name)
                        .WithFields(
                            new EmbedFieldBuilder { IsInline = true, Name = "Карта:", Value = query.Map },
                            new EmbedFieldBuilder { IsInline = true, Name = "Онлайн:", Value = $"{query.Players.Count}/{query.MaxPlayers}" },
                            new EmbedFieldBuilder { IsInline = true, Name = "Адрес:", Value = "Не реализовано!" },
                            new EmbedFieldBuilder { IsInline = false, Name = "Игроки:", Value = $"```{players}```" }
                        ).Build());

            }
        }
    }
}