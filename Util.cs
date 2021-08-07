using System;
using System.Threading.Tasks;

namespace ZadalBot
{
    public static class Util
    {
        public static async Task WaitWhile(Func<bool> condition, TimeSpan checkDelay)
        {
            while (condition()) await Task.Delay(checkDelay);
        }
    }
}