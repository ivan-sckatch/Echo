namespace EchoGame
{
    public interface IResearchLevel
    {
        bool IsResearch { get; }
        void EnterResearch();
        void StartCycle();
        void ResearchTick(float dt);
    }
}
