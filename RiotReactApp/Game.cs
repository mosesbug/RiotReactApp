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

        //TODO: maybe update these error props as interfaced props so its cleaner

        public int ErrorStatusCode { get; set; }  // Only set for failed requests

        public string ErrorMessage { get; set; } // Only set for failed requests
    }
}
