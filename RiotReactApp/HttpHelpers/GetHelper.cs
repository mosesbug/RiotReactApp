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
        public async Task<HttpGetResponse> GetAsync<T>(string uri)
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

        // TODO: Re-factor to only handle a single request body
        /// <summary>
        /// Gets the body from an HTTP request
        /// </summary>
        /// <param name="requestBody">The Stream body from an HTTPRequest</param>
        /// <returns></returns>
        public static async Task<List<string>> GetListOfStringsFromBody(Stream requestBody)
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
