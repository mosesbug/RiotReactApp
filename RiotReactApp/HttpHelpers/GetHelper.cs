using RiotReactApp.Controllers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RiotReactApp.HttpHelpers
{
    /// <summary>
    /// A helper class meant to assist in HTTP Get calls
    /// </summary>
    public class GetHelper
    {
        #region private fields

        private string __region;

        private string __apiKey;

        #endregion private fields

        #region constructor

        public GetHelper (string region, string apiKey)
        {
            __region = region;
            __apiKey = apiKey;
        }

        #endregion constructor

        #region public methods

        /// <summary>
        /// Uses web request to perform an HTTP to a Riot URI, optionally applying filters
        /// </summary>
        /// <param name="uri">The uri to query (jsut the middle of it)</param>
        /// <param name="filters">The filters to use</param>
        /// <returns>string of JSON data</returns>
        public async Task<HttpGetResponse> GetAsyncFromRiotApi<T>(string uriQuery, List<Filter> filters)
        {
            uriQuery += "?";
            foreach (Filter filt in filters)
            {
                uriQuery += filt.PropertyName + "=" + filt.Value + "&";
            }
            uriQuery = uriQuery[0..^1]; // Trim last &

            return await GetAsyncFromRiotApi<T>(uriQuery);
        }

        /// <summary>
        /// Uses web request to perform an HTTP to a Riot URI, optionally applying filters
        /// </summary>
        /// <param name="uri">The uri to query (just the middle of it)</param>
        /// <param name="filters">The filters to use</param>
        /// <returns>string of JSON data</returns>
        public async Task<HttpGetResponse> GetAsyncFromRiotApi<T>(string uriQuery)
        {
            string responseBody;
            string uri = "https://" + __region + uriQuery;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.Headers = new WebHeaderCollection
            {
                "Accept: application/json",
                "X-Riot-Token: " + __apiKey
            };

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    responseBody = await reader.ReadToEndAsync();
                }
            }
            catch (WebException e)
            {
                return new HttpGetResponse
                {
                    Ex = e
                };
            }

            T obj = DeserializeObject<T>(responseBody);

            return new HttpGetResponse
            {
                Value = obj
            };
        }

        /// <summary>
        /// Uses web request to perform an HTTP to a certain URI
        /// </summary>
        /// <param name="uri">The uri to query (jsut the middle of it)</param>
        /// <returns>String of JSON data</returns>
        public static async Task<HttpGetResponse> GetAsync<T>(string uri)
        {
            string responseBody;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.Headers = new WebHeaderCollection
            {
                "Accept: application/json",
            };

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    responseBody = await reader.ReadToEndAsync();
                }
            }
            catch (WebException e)
            {
                return new HttpGetResponse
                {
                    Ex = e
                };
            }

            T obj = DeserializeObject<T>(responseBody);

            return new HttpGetResponse
            {
                Value = obj
            };
        }

        public static GameResponse HandleBadRequest(WebException e, GetRequest reqType)
        {

            HttpWebResponse resp = e.Response as HttpWebResponse;

            if (resp == null)
            {
                return new GameResponse
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "Unknown error - please try again in a few seconds",
                    Games = new List<Game>()
                };
            }

            GameResponse gameResp = new GameResponse
            {
                StatusCode = resp.StatusCode,
                ErrorMessage = e.Message,
                Games = new List<Game>()
            };

            if (gameResp.StatusCode == HttpStatusCode.Forbidden || gameResp.StatusCode == HttpStatusCode.Unauthorized)
            {
                gameResp.ErrorMessage = "Incorrect or expired API key";
            }
            else if (gameResp.StatusCode == HttpStatusCode.NotFound)
            {
                if (reqType == GetRequest.Summoner)
                {
                    gameResp.ErrorMessage = "Summoner not found";
                }
                else if (reqType == GetRequest.Matches)
                {
                    gameResp.ErrorMessage = "No matches found for summoner";
                }
                else
                {
                    gameResp.ErrorMessage = "Unknown error - please try again in a few seconds";
                }
            }
            else if (gameResp.StatusCode == HttpStatusCode.TooManyRequests)
            {
                gameResp.ErrorMessage = "Slow your roll... too many requests too handle";
            }
            else // 5xx errors
            {
                gameResp.ErrorMessage = "Internal server error - please try again in a few seconds";
            }

            return gameResp;
        }

        /// <summary>
        /// Reads from the body of an HTTP response and serializes it to the passed in type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="body"></param>
        /// <returns></returns>
        public async static Task<T> ReadFromBody<T>(Stream body)
        {
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true,
            };
            return await JsonSerializer.DeserializeAsync<T>(body, options);
        }

        /// <summary>
        /// Deserialize a JSON string into a stringly-typed object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objString"></param>
        /// <returns></returns>
        public static T DeserializeObject<T>(string objString)
        {
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<T>(objString, options);
        }

        #endregion public methods
    }
}
