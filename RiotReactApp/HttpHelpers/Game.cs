using System;

namespace RiotReactApp
{
    public class Game
    {
        public string Date { get; set; }

        public string Result { get; set; } // Win/Loss

        public string ChampionName { get; set; }

        public string ChampionImage { get; set; }  // Used for an href on the client

        public int GameLength { get; set; }

        public string QueueType { get; set; }

        public int Kills { get; set; }

        public int Deaths { get; set; }

        public int Assists { get; set; }
    }
}
