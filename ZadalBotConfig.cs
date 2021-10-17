namespace ZadalBot
{
    public sealed class ZadalBotConfig
    {
        public string GameHostname { get; init; }

        public ushort GamePort { get; init; }
        
        public string GameRconPassword { get; init; }

        public string DiscordToken { get; init; }

        public string HttpUri { get; init; }

        public ulong DiscordGameChannel { get; init; }

        public ulong DiscordGameGuild { get; init; }

        public string DiscordGameChannelWebhook { get; init; }
    }
}