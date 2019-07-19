namespace Game.Protocol
{
    public class ReadResult
    {
        public string Type { get; set; }
        public Config Config { get; set; }
        public RequestInput Input { get; set; }
    }
}