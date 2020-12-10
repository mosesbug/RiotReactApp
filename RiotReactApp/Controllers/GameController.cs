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
             * (1) Get {encryptedAccountId} information using 
             *      print(requests.get(
                    "https://{region}.api.riotgames.com//lol/summoner/v4/summoners/by-name/{summonerName}", 
                    headers={"Accept": "application/json","X-Riot-Token": config["api-key"]}
                    ).json())
             */
            Request searchReq = requests.First();
            string uri = "https://" + searchReq.Region.ToLower() + ".api.riotgames.com//lol/summoner/v4/summoners/by-name/" + searchReq.SummonerName;
            __summoner = DeserializeObject<SummonerDTO>(await GetAsync(uri, searchReq.ApiKey, new List<Filter>())); // TODO: Status code error handling (try/catch?)                                                                                                          // TODO: Coding style, some thing should be stored as properties

            /**
             * (2) Use that information to print out match-lists using this API:
             *      print(requests.get(
                    "https://{region}.api.riotgames.com/lol/match/v4/matchlists/by-account/{encryptedAccountId}", 
                    headers={"Accept": "application/json","X-Riot-Token": config["api-key"]}
                    ).json())
             */
            uri = "https://" + searchReq.Region.ToLower() + ".api.riotgames.com/lol/match/v4/matchlists/by-account/" + __summoner.AccountId;
            List<Filter> filters = new List<Filter>
            {
                new Filter
                {
                    PropertyName = "endIndex",
                    Value = "10"
                }
            };

            MatchlistDto matches = DeserializeObject<MatchlistDto>(await GetAsync(uri, searchReq.ApiKey, filters));
            List<Game> gamesToReturn = new List<Game>();

            foreach (MatchReferenceDto matchRef in matches.Matches)
            {
                /**
             * Player stats:
             *  - 1) Win/Loss = matchId -> MatchDto get request -> get index in partipiantIndentities -> get ParticipantDto from participants via the participantId + 1 ->
             *       get ParticipantStatsDto -> get win boolean value  
             *  2) Queue type = Queue from MatchRferenceDto -> (TODO: map to the queue json)
             *  3) Date - get timestamp long from MatchReferenceDto.cs and compare it to epoch in order to get date
             *  4) Champion - for now just return return champion int from MatchReferenceDto (TODO: Also return the image eventually)
             *  5) GameLength - natchId -> MatchDto get request -> gameDuration
             */
                Game currGame = new Game();
                uri = "https://" + searchReq.Region.ToLower() + ".api.riotgames.com/lol/match/v4/matches/" + matchRef.GameId;
                MatchDto match = DeserializeObject<MatchDto>(await GetAsync(uri, searchReq.ApiKey, new List<Filter>()));
                ParticipantIdentityDto participantId = match.ParticipantIdentities.Find(IsSummonerMatch);
                ParticipantDto participant = match.Participants[participantId.ParticipantId - 1];
                
                currGame.Result = participant.Stats.Win ? "Win" : "Loss"; // (1)
                
                currGame.QueueType = matchRef.Queue.ToString(); // (2) // TODO: Actually map this value
                long timeStamp = matchRef.Timestamp;
                currGame.Date = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddMilliseconds(timeStamp - 86400000).ToShortDateString(); // subtract a day since we start at day 1
                currGame.Champion = matchRef.Champion.ToString();
                currGame.GameLength = (int)(match.GameDuration / 60);

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

        /// <summary>
        /// Uses web request to perform an HTTP to a certain URI, optionally applying filters
        /// </summary>
        /// <param name="uri">The uri to query</param>
        /// <param name="apiKey">The Riot API key to use</param>
        /// <returns></returns>
        public async Task<string> GetAsync(string uri, string apiKey, List<Filter> filters)
        {
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
                "X-Riot-Token: " + apiKey
            };

            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                return await reader.ReadToEndAsync();
            }
        }

        private bool IsSummonerMatch(ParticipantIdentityDto p)
        {
            return p.Player.AccountId == __summoner.AccountId;
        }

        #endregion private methods
    }
}
