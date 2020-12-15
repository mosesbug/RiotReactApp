
using System.Net;

namespace RiotReactApp
{
    public class HttpGetResponse
    {

        public WebException? Ex { get; set; }
        
        public object Value { get; set; }
    }
}
