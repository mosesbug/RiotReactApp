﻿
namespace RiotReactApp
{
    public class SummonerDTO
    {
        public string AccountId { get; set; }

        public int ProfileIconId { get; set; } // TODO: Get icon and show it

        public long RevisionDate { get; set; }

        public string Name { get; set; }

        public string Id { get; set; }

        public string Puuid { get; set; }

        public long SummonerLevel { get; set; }
    }
}