using System;

namespace RiotReactApp
{
    public class Game
    {
        public string Date { get; set; }

        public string Result { get; set; } // Win or Loss -> use https://developer.riotgames.com/apis#match-v4/GET_getMatchlist to get gameId and then find that match for this info

        public string Champion { get; set; } // Champion played

        public int GameLength { get; set; }
    }
}
