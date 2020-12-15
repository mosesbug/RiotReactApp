using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RiotReactApp
{
    public class MatchDto
    {
        public long GameId { get; set; }

        public List<ParticipantIdentityDto> ParticipantIdentities { get; set; }

        public string GameType { get; set; }

        public long GameDuration { get; set; }

        public List<ParticipantDto> Participants { get; set; }
    }
}
