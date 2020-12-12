using Microsoft.AspNetCore.Http;
using System;

namespace RiotReactApp
{
    public class HttpException : Exception
    {
        public HttpResponse Response { get; set; }
    }
}
