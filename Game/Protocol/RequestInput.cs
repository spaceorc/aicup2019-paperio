using System.Collections.Generic;
using Game.Types;

namespace Game.Protocol
{
    public class RequestInput
    {
        public Dictionary<string, PlayerData> players;
        public BonusData[] bonuses;
        public int tick_num;

        public class BonusData
        {
            public BonusType type;
            public V position;
        }

        public class PlayerData
        {
            public int score;
            public Direction direction;
            public V[] territory;
            public V position;
            public V[] lines;
            public BonusState[] bonuses;
        }
        
        public class BonusState
        {
            public BonusType type;
            public int ticks;
        }
    }
}