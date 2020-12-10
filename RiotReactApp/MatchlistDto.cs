using System.Collections.Generic;

namespace RiotReactApp
{
    public class MatchlistDto
    {
        public int StartIndex { get; set; }

        public int TotalGames { get; set; }

        public int EndIndex { get; set; }

        public List<MatchReferenceDto> Matches { get; set; }
    }
}
