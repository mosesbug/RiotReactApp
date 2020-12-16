
namespace RiotReactApp.HttpHelpers
{
    public class SummonerCard
    {
        public string ProfileIconLink { get; set; }

        public long SummonerLevel { get; set; }

        public string SummonerName { get; set; }

        public double Winrate { get; set; } // Winrate out of 100

        public string KDA { get; set; }

        public string Rating { get; set; }  // S+, S, A, B, C, D, F
    }
}
