using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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

        private int __currentChampId;

        private string __version;

        #endregion fields

        #region constructors

        public GameController(ILogger<GameController> logger)
        {
            _logger = logger;
        }

        #endregion constructors

        #region public methods

        [HttpPost]
        public async Task<IEnumerable<Game>> Post()
        {
            List<string> bodyComponents = await GetListOfStringsFromBody(Request.Body);
            List<Request> requests = new List<Request>();

            foreach (string str in bodyComponents)
            {
                Console.WriteLine(str);
                requests.Add(DeserializeObject<Request>(str));
            }

            // Using https://developer.riotgames.com/apis
            /**
             * (1) Get {encryptedAccountId} 
             */
            __req = requests.First();
            
            // TODO: Status code error handling (try/catch?)
                // 1) Summoner not found
                // 2) 5xx errors
            // TODO: Coding style, some thing should be stored as properties
            __summoner = DeserializeObject<SummonerDTO>(await GetAsyncFromRiotApi(".api.riotgames.com/lol/summoner/v4/summoners/by-name/" + __req.SummonerName, new List<Filter>()));                                                                                                         // TODO: Coding style, some thing should be stored as properties

            /**
             * (2) Use that information to get the match-lists
             *      
             */
            List<Filter> filters = new List<Filter>
            {
                new Filter
                {
                    PropertyName = "endIndex",
                    Value = "10"
                }
            };

            MatchlistDto matches = DeserializeObject<MatchlistDto>(await GetAsyncFromRiotApi(".api.riotgames.com/lol/match/v4/matchlists/by-account/" + __summoner.AccountId, 
                new List<Filter>
                {
                    new Filter
                    {
                        PropertyName = "endIndex",
                        Value = "10"
                    }
                }
            ));

            // Get Queue.json data
            string test = await GetAsync("https://ddragon.leagueoflegends.com/api/versions.json");
            List<string> versions = DeserializeListObject<string>(test);
            __version = versions.First();

            // Get Champs data
            __champsJson = DeserializeObject<ChampionsJson>(await GetAsync("http://ddragon.leagueoflegends.com/cdn/" + __version +"/data/en_US/champion.json"));

            List<Game> gamesToReturn = new List<Game>();
            foreach (MatchReferenceDto matchRef in matches.Matches)
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
             */
                Game currGame = new Game();
                MatchDto match = DeserializeObject<MatchDto>(await GetAsyncFromRiotApi(".api.riotgames.com/lol/match/v4/matches/" + matchRef.GameId, new List<Filter>()));
                ParticipantIdentityDto participantId = match.ParticipantIdentities.Find(IsSummonerMatch);
                ParticipantDto participant = match.Participants[participantId.ParticipantId - 1];
                
                currGame.Result = participant.Stats.Win ? "Win" : "Loss"; // (1) Result

                // TODO: Maybe actually map to http://http://static.developer.riotgames.com/docs/lol/queues.json
                currGame.QueueType = GetQueueStringMapping(matchRef.Queue); // (2) QueueType
                
                long timeStamp = matchRef.Timestamp;
                currGame.Date = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddMilliseconds(timeStamp - 86400000).ToShortDateString(); // (3) Date

                __currentChampId = matchRef.Champion;
                Tuple<string, string> champTuple = GetChampMappings();
                currGame.ChampionName = champTuple.Item1; // (4) ChampionName
                currGame.ChampionImage = champTuple.Item2; // (5) ChampionImage

                currGame.GameLength = (int)(match.GameDuration / 60); // (7) GameLength

                gamesToReturn.Add(currGame); // Woohoo
            }

            return gamesToReturn;
        }

        #endregion public methods

        #region private methods

        /// <summary>
        /// Gets the body from an HTTP request
        /// </summary>
        /// <param name="requestBody">The Stream body from an HTTPRequest</param>
        /// <returns></returns>
        private async Task<List<string>> GetListOfStringsFromBody(Stream requestBody)
        {
            StringBuilder builder = new StringBuilder();
            byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);
            List<string> results = new List<string>();

            while (true)
            {
                var bytesRemaining = await requestBody.ReadAsync(buffer, offset: 0, buffer.Length);

                if (bytesRemaining == 0)
                {
                    results.Add(builder.ToString());
                    break;
                }

                // Instead of adding the entire buffer into the StringBuilder
                // only add the remainder after the last \n in the array.
                var prevIndex = 0;
                int index;
                while (true)
                {
                    index = Array.IndexOf(buffer, (byte)'\n', prevIndex);
                    if (index == -1)
                    {
                        break;
                    }

                    var encodedString = Encoding.UTF8.GetString(buffer, prevIndex, index - prevIndex);

                    if (builder.Length > 0)
                    {
                        // If there was a remainder in the string buffer, include it in the next string.
                        results.Add(builder.Append(encodedString).ToString());
                        builder.Clear();
                    }
                    else
                    {
                        results.Add(encodedString);
                    }

                    // Skip past last \n
                    prevIndex = index + 1;
                }


                // TODO: There's likely an edge case of none remaining that's occasionally causing a runtime error
                var remainingString = Encoding.UTF8.GetString(buffer, prevIndex, bytesRemaining - prevIndex);
                builder.Append(remainingString);
            }

            ArrayPool<byte>.Shared.Return(buffer);

            return results;
        }

        private T DeserializeObject<T>(string objString)
        {
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<T>(objString, options);
        }

        private List<T> DeserializeListObject<T>(string objString)
        {
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<List<T>>(objString, options);
        }

        /// <summary>
        /// Uses web request to perform an HTTP to a Riot URI, optionally applying filters
        /// </summary>
        /// <param name="uri">The uri to query (jsut the middle of it)</param>
        /// <param name="filters">The filters to use</param>
        /// <returns>string of JSON data</returns>
        private async Task<string> GetAsyncFromRiotApi(string uriQuery, List<Filter> filters)
        {
            string uri = "https://" + __req.Region.ToLower() + uriQuery;
            if (filters.Count > 0) //TODO: improve coding syntax (...list maybe?)
            {
                uri += "?";
                foreach (Filter filt in filters)
                {
                    uri += filt.PropertyName + "=" + filt.Value + "&";
                }
                uri = uri[0..^1]; // Trim last &
            }

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.Headers = new WebHeaderCollection
            {
                "Accept: application/json",
                "X-Riot-Token: " + __req.ApiKey
            };

            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                return await reader.ReadToEndAsync();
            }
        }

        /// <summary>
        /// Uses web request to perform an HTTP to a certain URI
        /// </summary>
        /// <param name="uri">The uri to query (jsut the middle of it)</param>
        /// <returns>String of JSON data</returns>
        private async Task<string> GetAsync(string uri)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.Headers = new WebHeaderCollection
            {
                "Accept: application/json",
            };

            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                return await reader.ReadToEndAsync();
            }
        }

        /// <summary>
        /// Match delegate between ParticipantDto and ParticipantIdentityDto
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private bool IsSummonerMatch(ParticipantIdentityDto p)
        {
            return p.Player.AccountId == __summoner.AccountId;
        }

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

        #endregion private methods
    }
}
