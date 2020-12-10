using System;

namespace RiotReactApp
{
    public class Game
    {
        public string Date { get; set; }

        public string Result { get; set; } // Win/Loss

        public string Champion { get; set; } // Champion played

        public int GameLength { get; set; }

        public string QueueType { get; set; }
    }
}
