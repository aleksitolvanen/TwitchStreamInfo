using System.Collections.Generic;

namespace TwitchStreamInfo.Models
{
    public class StreamUserWrapper
    {
        public int _total { get; set; }
        public List<StreamUser> users { get; set; }
    }
}
