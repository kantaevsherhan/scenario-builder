namespace ScenarioBuilder.UI
{
    /// <summary>A window that can be closed with Esc.</summary>
    public interface IClosablePanel
    {
        bool IsOpen { get; }

        void Hide();
    }
}
