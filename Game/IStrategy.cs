using Game.Helpers;
using Game.Protocol;

namespace Game
{
	public interface IStrategy
	{
		RequestOutput OnTick(RequestInput requestInput, TimeManager timeManager);
	}
}