using System;
using System.Threading.Tasks;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Test {
    public class Wait {
        public static async Task UntilAfter60Seconds(Func<bool> condition) {
            await Until(condition, TimeSpan.FromSeconds(60));
        }

        public static async Task Until(Func<bool> condition, TimeSpan timeSpan) {
            var limit = DateTime.Now.Add(timeSpan);
            do {
                if (condition()) { return; }

                await Task.Delay((int)timeSpan.TotalMilliseconds / 10);
            } while (DateTime.Now <= limit);
        }
    }
}
