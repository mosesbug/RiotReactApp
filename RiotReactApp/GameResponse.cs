using System;
using System.Collections.Generic;
using System.Net;

namespace RiotReactApp
{
    public class GameResponse
    {
        public HttpStatusCode StatusCode { get; set; }

        public string ErrorMessage { get; set; } // Only set for failed requests

        public IEnumerable<Game> Games { get; set; }
    }
}
