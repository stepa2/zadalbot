using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ZadalBot
{
    public static class Util
    {
        private static readonly Regex _ipAndPortStatusRegex =
            new(@"udp\/ip\s+:\s+(\d+\.\d+\.\d+\.\d+):(\d+)\s+\(public ip:\s+(\d+\.\d+\.\d+\.\d+)\)");

        public static async Task WaitWhile(Func<bool> condition, TimeSpan checkDelay)
        {
            while (condition()) await Task.Delay(checkDelay);
        }

        public static IPEndPoint GetIpAddressFromStatusOutput(string status)
        {
            var match = _ipAndPortStatusRegex.Match(status);

            if (!match.Success)
                return null;

            if (!IPEndPoint.TryParse($"{match.Groups[3].Value}:{match.Groups[2].Value}", out var endpoint))
                return null;

            return endpoint;
        }
    }
}