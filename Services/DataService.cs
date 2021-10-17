using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ZadalBot.Services
{
    public sealed class DataService
    {
        public sealed class ChatMessage
        {
            public string Sender { get; init; }
            public string Contents { get; init; }
            public IEnumerable<string> Attachments { get; init; }
        }

        public sealed class GameMessage
        {
            public string Sender { get; init; }
            public string Contents { get; init; }
        }

        public enum ConnectionStatusEnum
        {
            ConnectedToServer,
            LoadedServerside,
            LoadedClientside
        }

        public sealed class PlayerConnectionStatus
        {
            public string Player { get; init; }

            public ConnectionStatusEnum Status { get; init; }
        }

        public class PlayerDisconnectedData
        {
            public string Player { get; init; }
            public string Reason { get; init; }
        }

        private readonly ConcurrentStack<JObject> _dataFromChat = new();


        public event Func<GameMessage, Task> OnGotGameMessage;
        public event Func<PlayerConnectionStatus, Task> OnGotPlayerConnectionStatus;
        public event Func<PlayerDisconnectedData, Task> OnPlayerDisconnected;

        public void StoreChatMessage(ChatMessage msg)
        {
            var jobj = JObject.FromObject(msg);
            jobj.Add("Type", new JValue("ChatMessage"));
            _dataFromChat.Push(jobj);
        }
            

        public Task<JObject[]> GetDataFromChat()
        {
            return Task.Run(() =>
            {
                var count = _dataFromChat.Count;

                if (count <= 0)
                    return Array.Empty<JObject>();

                var result = new JObject[count];
                var poppedCount = _dataFromChat.TryPopRange(result);

                return result[..poppedCount];
            });
        }

        public async Task HandleDataFromGame(JObject data) =>
            await Task.Run(async () =>
            {
                var type = data.Value<string>("Type");
                data.Remove("Type");

                switch (type)
                {
                    case "GameMessage":
                        if (OnGotGameMessage == null)
                            Console.WriteLine("Data > OnGotGameMessage is null");
                        else
                            await OnGotGameMessage(data.ToObject<GameMessage>());
                        break;
                    case "PlayerConnection":
                        if (OnGotPlayerConnectionStatus == null)
                            Console.WriteLine("Data > OnGotPlayerConnectionStatus is null");
                        else
                            await OnGotPlayerConnectionStatus(data.ToObject<PlayerConnectionStatus>());
                        break;
                    case "PlayerDisconnected":
                        if (OnPlayerDisconnected == null)
                            Console.WriteLine("Data > OnPlayerDisconnected is null");
                        else
                            await OnPlayerDisconnected(data.ToObject<PlayerDisconnectedData>());
                        break;

                    case null:
                        Console.WriteLine("Data > JSON from game has no Type key or it is not a string");
                        break;

                    default:
                        Console.WriteLine("Data > JSON from game has invalid type {0}", type);
                        break;
                }
            });
    }
}