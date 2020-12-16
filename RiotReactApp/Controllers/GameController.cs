using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RiotReactApp.HttpHelpers;

namespace RiotReactApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GameController : ControllerBase
    {
        #region fields

        private readonly ILogger<GameController> _logger;

        private SummonerDTO __summoner;

        private Request __req;

        private ChampionsJson __champsJson;

        private MatchlistDto __matches;

        private int __currentChampId;

        private string __version;

        private int __totalKills;

        private int __totalDeaths;

        private int __totalAssists;

        private int __totalWins;

        #endregion fields

        #region constructors

        public GameController(ILogger<GameController> logger)
        {
            _logger = logger;
        }

        #endregion constructors

        #region public methods

        [HttpPost]
        public async Task<GameResponse> Post()
        {
            List<string> bodyComponents = await GetHelper.GetListOfStringsFromBody(Request.Body);
            List<Request> requests = new List<Request>();
            List<Game> gamesToReturn = new List<Game>();
            GameResponse gameResponse = new GameResponse
            {
                StatusCode = HttpStatusCode.OK,
                ErrorMessage = "",
                Games = gamesToReturn
            };

            foreach (string str in bodyComponents)
            {
                Console.WriteLine(str);
                requests.Add(GetHelper.DeserializeObject<Request>(str));
            }

            __req = requests.First();
            __req.ApiKey = Environment.GetEnvironmentVariable("X-Riot-Token", EnvironmentVariableTarget.Machine); // Windows only (TODO: instructions for other OS?)
          

            if (__req.ApiKey == null)
            {
                // Handle non-existent API-Key
                gameResponse.StatusCode = HttpStatusCode.NotFound;
                gameResponse.ErrorMessage = "No API key provided";

                return gameResponse;
            }

            GetHelper getHelper = new GetHelper(__req.Region.ToLower(), __req.ApiKey);

            // Using https://developer.riotgames.com/apis
            /**
             * (1) Get {encryptedAccountId} 
             */
            //TODO: Move these gets into a helper class and have them return an HttpGetRespone
            HttpGetResponse summonerGet = await getHelper.GetAsyncFromRiotApi<SummonerDTO>(".api.riotgames.com/lol/summoner/v4/summoners/by-name/" + __req.SummonerName);
            if (summonerGet.Ex != null)
            {
                return GetHelper.HandleBadRequest(summonerGet.Ex, GetRequest.Summoner);
            }
            __summoner = summonerGet.Value as SummonerDTO;
            
            /**
             * (2) Use that information to get the match-lists
             *      
             */
            HttpGetResponse matchesGet = await getHelper.GetAsyncFromRiotApi<MatchlistDto>(".api.riotgames.com/lol/match/v4/matchlists/by-account/" + __summoner.AccountId,
                    new List<Filter>
                    {
                        new Filter
                        {
                            PropertyName = "endIndex",
                            Value = "10"
                        }
                    }
            );
            if (matchesGet.Ex != null)
            {
                return GetHelper.HandleBadRequest(matchesGet.Ex, GetRequest.Matches);
            }
            __matches = matchesGet.Value as MatchlistDto;

            // Get Queue.json data - TODO: Maybe only need to do this one time
            HttpGetResponse versionsGet = await getHelper.GetAsync<List<string>>("https://ddragon.leagueoflegends.com/api/versions.json");
            if (versionsGet.Ex != null)
            {
                return GetHelper.HandleBadRequest(versionsGet.Ex, GetRequest.Versions);
            }
            __version = (versionsGet.Value as List<string>).First();

            // Get Champs data - TODO: Maybe only need to do this one time
            HttpGetResponse championsGet = await getHelper.GetAsync<ChampionsJson>("http://ddragon.leagueoflegends.com/cdn/" + __version + "/data/en_US/champion.json");
            if (championsGet.Ex != null)
            {
                return GetHelper.HandleBadRequest(championsGet.Ex, GetRequest.Champions);
            }
            __champsJson = (championsGet.Value as ChampionsJson);

            foreach (MatchReferenceDto matchRef in __matches.Matches)
            {
                /**
             * Player stats:
             *  - 1) Win/Loss = matchId -> MatchDto get request -> get index in partipiantIndentities -> get ParticipantDto from participants via the participantId + 1 ->
             *       get ParticipantStatsDto -> get win boolean value  
             *  2) Queue type = Queue from MatchRferenceDto -> (TODO: map to the queue json)
             *  3) Date - get timestamp long from MatchReferenceDto.cs and compare it to epoch in order to get date
             *  4) ChampionName - for now just return return champion int from MatchReferenceDto (TODO: Also return the image eventually)
             *  5) ChampionImage - The href link
             *  6) GameLength - natchId -> MatchDto get request -> gameDuration
             *  7) Kills
             *  8) Deaths
             *  9) Assists
             *  
             *  TODO: 
             *  1) Items built (w/ icons)
             *  
             *  Player card:
             *  1) Overall KDA
             *  2) Summoner Level
             *  3) Win rate
             *  4) Profile icon
             *  5) Custom report card rating
             */
                Game currGame = new Game();
                MatchDto match = new MatchDto();
                HttpGetResponse matchGet = await getHelper.GetAsyncFromRiotApi<MatchDto>(".api.riotgames.com/lol/match/v4/matches/" + matchRef.GameId);
                if (matchGet.Ex != null)
                {
                    return GetHelper.HandleBadRequest(matchGet.Ex, GetRequest.Match);
                }
                match = matchGet.Value as MatchDto;

                ParticipantIdentityDto participantId = match.ParticipantIdentities.Find(IsSummonerMatch);
                ParticipantDto participant = match.Participants[participantId.ParticipantId - 1];
                
                if (participant.Stats.Win) // (1) Result
                {
                    currGame.Result = "Win";
                    __totalWins++;
                }
                else
                {
                    currGame.Result = "Loss";
                }

                // TODO: Maybe actually map to http://http://static.developer.riotgames.com/docs/lol/queues.json
                currGame.QueueType = GetQueueStringMapping(matchRef.Queue); // (2) QueueType
                
                long timeStamp = matchRef.Timestamp;
                currGame.Date = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddMilliseconds(timeStamp - 86400000).ToShortDateString(); // (3) Date

                __currentChampId = matchRef.Champion;
                Tuple<string, string> champTuple = GetChampMappings();
                currGame.ChampionName = champTuple.Item1; // (4) ChampionName
                currGame.ChampionImage = champTuple.Item2; // (5) ChampionImage

                currGame.GameLength = (int)(match.GameDuration / 60); // (6) GameLength

                currGame.Kills = participant.Stats.Kills; // (7) Kills
                __totalKills += currGame.Kills;
                currGame.Deaths = participant.Stats.Deaths; // (8) Deaths
                __totalDeaths += currGame.Deaths;
                currGame.Assists = participant.Stats.Assists; // (9) Assists
                __totalAssists += currGame.Assists;

                gamesToReturn.Add(currGame); // Woohoo
            }

            double kda = (__totalKills + __totalAssists) / (__totalDeaths * 1.0);
            double winRate = (double)__totalWins / gameResponse.Games.Count() * 100;
            gameResponse.CardStats = GetSummonerCardInfo(kda, winRate);

            return gameResponse;
        }

        #endregion public methods

        #region private methods

        private string GetQueueStringMapping(int queueId)
        {
            string returnStr;
            if (queueId == 0)
            {
                returnStr = "Custom";
            }
            else if (queueId == 400)
            {
                returnStr = "5v5 Draft Pick";
            }
            else if (queueId == 420)
            {
                returnStr = "Ranked Solo";
            }
            else if (queueId == 430)
            {
                returnStr = "5v5 Blind Pick";
            }
            else if (queueId == 440)
            {
                returnStr = "Ranked Flex";
            }
            else if (queueId == 450)
            {
                returnStr = "ARAM";
            }
            else if (queueId == 700)
            {
                returnStr = "Clash";
            }
            else if (queueId >= 820 && queueId <= 850)
            {
                returnStr = "Bot Game";
            }
            else if (queueId >= 1100 && queueId <= 1111)
            {
                returnStr = "TFT";
            }
            else
            {
                returnStr = "Special Mode";
            }

            return returnStr;
        }

        /// <summary>
        /// Get's champ name and image mappings
        /// </summary>
        /// <param name="champId">The champs Id</param>
        /// <returns>A tuple of the name and image mappings</returns>
        private Tuple<string, string> GetChampMappings()
        {
            string champName;
            string champImage;

            Champion champ = __champsJson.Data.Values.ToList().Find(IsChampionMatch);
            champName = champ.Id;
            champImage = "http://ddragon.leagueoflegends.com/cdn/" + __version + "/img/champion/" + champ.Image.Full;

            return new Tuple<string, string>(champName, champImage);
        }

        /// <summary>
        /// Match delegate for finding the right champ
        /// </summary>
        /// <param name="champ"></param>
        /// <returns></returns>
        private bool IsChampionMatch(Champion champ)
        {
            if (Int32.Parse(champ.Key) == __currentChampId) { return true; }
            else { return false;  }
        }

        // <summary>
        /// Match delegate between ParticipantDto and ParticipantIdentityDto
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private bool IsSummonerMatch(ParticipantIdentityDto p)
        {
            return p.Player.AccountId == __summoner.AccountId;
        }

        /// <summary>
        /// Adds summoner card info to our game response
        /// </summary>
        /// <param name="kda">The player's average kda</param>
        /// <param name="wr">The winrate out of 100</param>
        private SummonerCard GetSummonerCardInfo(double kda, double wr)
        {
            return new SummonerCard
            {
                ProfileIconLink = "http://ddragon.leagueoflegends.com/cdn/" + __version + "/img/profileicon/" + __summoner.ProfileIconId + ".png",
                SummonerLevel = __summoner.SummonerLevel,
                SummonerName = __summoner.Name,
                KDA = Math.Round(kda, 2) + "",
                Winrate = wr,
                Rating = DeterminePlayerRating(kda, wr),
            };
        }

        /// <summary>
        /// Determine a rating to give the player
        /// </summary>
        /// <param name="kda">The player's average kda</param>
        /// <param name="wr">The winrate out of 100</param>
        /// <returns>The string of the players rating</returns>
        private string DeterminePlayerRating(double kda, double wr)
        {
            double kdaCoeff = 0.8;
            double wrCoeff = 0.2;

            // kda < 1 = really bad
            // kda >1 and <2 = below average
            // kda >2 and <2.5 = average
            // kda >2.5 and <3.5 = good
            // kda >3.5 and <5 = really good
            // kda > 5 = your a god (make a cap here)

            kda *= 2;
            wr /= 10;

            double score = (kda * kdaCoeff) + (wr * wrCoeff);
            return ScoreToRating(score);
        }

        /// <summary>
        /// Calculates the player card rating
        /// </summary>
        /// <param name="score">The player's weighted score</param>
        /// <returns></returns>
        private string ScoreToRating(double score)
        {
            if (score >= 9)
            {
                return "S+";
            }
            else if (score >= 7.5)
            {
                return "S";
            }
            else if (score >= 6)
            {
                return "A";

            }
            else if (score >= 5)
            {
                return "B";
            }
            else if (score >= 4)
            {
                return "C";
            }
            else if (score >= 3)
            {
                return "D";
            }
            else
            {
                return "F";
            }

        }

        #endregion private methods
    }

    public enum GetRequest
    {
        Summoner,
        Matches,
        Versions,
        Champions,
        Match
    }
}
