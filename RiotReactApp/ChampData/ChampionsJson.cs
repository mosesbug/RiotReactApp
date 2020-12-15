
using System.Collections.Generic;

namespace RiotReactApp
{
    public class ChampionsJson
    {
        public string Type { get; set; }

        public string Format { get; set; }

        public string Version { get; set; }

        public Dictionary<string, Champion> Data { get; set; }

    }
}
