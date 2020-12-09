using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                requests.Add(JsonSerializer.Deserialize<Request>(str));
            }

            // Using https://developer.riotgames.com/apis

            /**
             * (1) Get {encryptedAccountId} information using 
             *      print(requests.get(
                    "https://{region}.api.riotgames.com//lol/summoner/v4/summoners/by-name/{summonerName}", 
                    headers={"Accept": "application/json","X-Riot-Token": config["api-key"]}
                    ).json())
             * (2) Use that information to print out match-lists using this API:
             *      print(requests.get(
                    "https://{region}.api.riotgames.com/lol/match/v4/matchlists/by-account/{encryptedAccountId}", 
                    headers={"Accept": "application/json","X-Riot-Token": config["api-key"]}
                    ).json())
                (3) Use filtering to limit the returned results 
                    - TODOs: 
                        (a) maybe add fields to let the user control this
                        (b) Add a next button to enable pagination of results using beginIndex and endIndex
                (4) Return results as a list of games (IEnumerable<Game>)

             */
            var rng = new Random();
            return Enumerable.Range(1, 10).Select(index => new Game
            {
                Date = (DateTime.Now.AddDays(index)).ToShortDateString(),
                Result = (rng.Next(0, 11) > 5) ? "Win" : "Loss",
                Champion = "200 years",
                GameLength = rng.Next(3, 59),
            })
            .ToArray();
        }

        #endregion public methods

        #region private methods

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

        #endregion private methods
    }
}
