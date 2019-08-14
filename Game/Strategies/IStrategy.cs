using Game.Helpers;
using Game.Protocol;

namespace Game.Strategies
{
	public interface IStrategy
	{
		RequestOutput OnTick(RequestInput requestInput, TimeManager timeManager);
	}
}