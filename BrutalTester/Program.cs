using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Game.Helpers;
using Newtonsoft.Json;

namespace BrutalTester
{
    internal class Program
    {
        private static string[] topPlayers =
        {
            "Игорь Волков",
            "Денис Уткин",
            "Sergei Fomin",
            "Иван Дашкевич",
            "Anton Kozlovsky",
            "Владимир Усачев",
            "Александр Желтов",
            "Ralph Pulletz",
            "Екатерина Федотова",
            "Boris Zaitsev",
            "Максим Полунин",
            "Алексей Голубь"
        };

        public static void Main(string[] args)
        {
            var file = Path.Combine(FileHelper.PatchDirectoryName("BrutalTester"), "out.json");
            var games = JsonConvert.DeserializeObject<List<Game>>(File.ReadAllText(file));
            var scores = new Dictionary<string, int>();
            var counts = new Dictionary<string, int>();
            foreach (var game in games)
            {
                if (game.Players.Count(p => topPlayers.Any(t => p.IndexOf(t) >= 0)) >= 2)
                {
                    Console.Out.WriteLine(game.Session);
                    Array.Sort(game.Scores, game.Players);
                    for (int i = 0; i < game.Players.Length; i++)
                    {
                        scores[game.Players[i]] = scores.GetOrAdd(game.Players[i]) + i;
                        counts[game.Players[i]] = counts.GetOrAdd(game.Players[i]) + 1;
                    }
                }
            }

            foreach (var kvp in scores.OrderByDescending(x => (double)x.Value / counts[x.Key]))
            {
                Console.Out.WriteLine($"{(double)kvp.Value / counts[kvp.Key]:F2}\t{kvp.Value}\t{counts[kvp.Key]}\t{kvp.Key}");
            }
        }

        public static async Task Main2(string[] args)
        {
            var games = new List<Game>();
            for (int i = 1; i < 100; i++)
            {
                Console.Out.WriteLine($"page {i}");
                var pageGames = (await ReadGamesRowsColsAsync(i)).Select(Game.Parse).ToList();
                if (games.Any(g => pageGames.Any(pg => pg.Session == g.Session)))
                    break;

                games.AddRange(pageGames.Where(x => x.Time.StartsWith("21.08.19")));
                if (pageGames.Any(x => !x.Time.StartsWith("21.08.19")))
                    break;
            }

            var file = Path.Combine(FileHelper.PatchDirectoryName("BrutalTester"), "out.json");
            File.WriteAllText(file, games.ToJson());
        }

        private static async Task<List<string[]>> ReadGamesRowsColsAsync(int page)
        {
            var rows = await ReadGamesRowsAsync(page);
            var re = new Regex(@"<td[^>]*>\s*(?<content>.*?)\s*</td>", RegexOptions.Singleline);
            return rows.Select(x => re.Matches(x).Select(m => m.Groups["content"].Value).ToArray()).ToList();
        }

        private static async Task<string[]> ReadGamesRowsAsync(int page)
        {
            var s = await ReadGamesStringAsync(page);
            var re = new Regex(@"<tr\sclass=""session-item[^>]*>(?<content>.*?)</tr>", RegexOptions.Singleline);
            return re.Matches(s).Select(m => m.Groups["content"].Value).ToArray();
        }

        private static async Task<string> ReadGamesStringAsync(int page)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://aicups.ru");
                using (var responseMessage = await client.GetAsync($"round/13/?rp={page}"))
                {
                    return await responseMessage.Content.ReadAsStringAsync();
                }
            }
        }

        public class Game
        {
            private static Regex re = new Regex(@"<a\s[^>]*>\s*(?<content>.*?)\s*</a>", RegexOptions.Singleline);

            public static Game Parse(string[] row)
            {
                var players = re.Matches(row[5]).Select(x => x.Groups["content"].Value).ToArray();
                return new Game
                {
                    Session = re.Match(row[0]).Groups["content"].Value,
                    Time = row[1],
                    Players = players.Where((x, i) => i % 2 == 0).ToArray(),
                    Scores = row[6].Split("<br />").Select(int.Parse).ToArray()
                };
            }

            public string Session { get; set; }
            public string Time { get; set; }
            public string[] Players { get; set; }
            public int[] Scores { get; set; }
        }
    }
}