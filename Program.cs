using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Newtonsoft.Json;

namespace ZadalBot
{
    internal class MainHolder
    {
        public static async Task Main(string[] args)
        {
            var fileName = args.Length == 0 ? "zadalbot_config.json" : string.Join(' ', args);

            ZadalBotConfig cfg;

            using(var fileReader = File.OpenText(fileName))
            using (var jsonReader = new JsonTextReader(fileReader))
                cfg = new JsonSerializer().Deserialize<ZadalBotConfig>(jsonReader);

            var bot = new ZadalBot(cfg);

            await bot.RunAsync();
        }

    }
}
