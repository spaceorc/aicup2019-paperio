using Game.Types;

namespace Game.Protocol
{
	public class TurnOutput
    {
        public string Debug { get; set; }
        public string Error { get; set; }
        public Direction Command { get; set; }
    }
}