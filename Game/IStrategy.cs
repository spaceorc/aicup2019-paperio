using Game.Protocol;

namespace Game
{
	public interface IStrategy
	{
		TurnOutput OnTick(TurnInput turnInput, TimeManager timeManager);
	}
}