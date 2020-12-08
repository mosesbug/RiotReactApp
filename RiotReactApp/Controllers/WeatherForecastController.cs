using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace RiotReactApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }

        //[HttpGet] TODO move this into the one HttpGet method
        public IEnumerable<Game> GetGame()
        {
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

            return Enumerable.Range(1, 5).Select(index => new Game
            {
                Date = DateTime.Now.AddDays(index),
                Result = "Win",
                GameLength = rng.Next(3, 59),
            })
            .ToArray();
        }
    }
}
