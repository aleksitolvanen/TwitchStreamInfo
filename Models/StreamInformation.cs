namespace TwitchStreamInfo.Models
{
    public class StreamInformation
    {
        public long _id { get; set; }
        public string game { get; set; }
        public int viewers { get; set; }
        public int video_height { get; set; }
        public float average_fps { get; set; }
        public int delay { get; set; }
        public string created_at { get; set; }
        public StreamInformationChannel channel { get; set; }

    }
}
