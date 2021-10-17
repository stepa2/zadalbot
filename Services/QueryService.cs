using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Okolni.Source.Common;
using Okolni.Source.Query;

namespace ZadalBot.Services
{
    public class QueryResult
    {
        public string Name { get; init; }
        public string Map { get; init; }
        public List<Player> Players { get; init; }
        public int MaxPlayers { get; init; }

        public class Player
        {
            public string Name { get; init; }
            public TimeSpan Duration { get; init; }
        }
    }

    public class QueryService
    {
        private readonly QueryConnection _connection;

        public QueryService(ZadalBotConfig config)
        {
            _connection = new QueryConnection
            {
                Host = config.GameHostname,
                Port = config.GamePort
            };
        }

        public async Task Connect() =>
            await Task.Run(() =>
            {
                _connection.Connect();
            });

        public async Task<QueryResult> Query() =>
            await Task.Run(() =>
            {
                try
                {
                    var info = _connection.GetInfo();
                    var players = _connection.GetPlayers();

                    return new QueryResult
                    {
                        Name = info.Name,
                        Map = info.Map,
                        MaxPlayers = info.MaxPlayers,
                        Players = players.Players.Select(ply => new QueryResult.Player
                        {
                            Name = ply.Name,
                            //Duration = ply.Duration
                        }).ToList()
                    };
                }
                catch (SourceQueryException)
                {
                    return null;
                }

            });
    }
}