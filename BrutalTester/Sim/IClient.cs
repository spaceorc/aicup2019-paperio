using Game.Protocol;

namespace BrutalTester.Sim
{
    public interface IClient
    {
        void SendConfig(Config config);
        void SendGameEnd();
        RequestOutput SendRequestInput(RequestInput requestInput);
    }
}