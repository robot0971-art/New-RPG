public interface IGameplayWindow
{
    bool IsOpen { get; }
    void Close();
}

public static class GameplayWindowManager
{
    private static readonly System.Collections.Generic.List<IGameplayWindow> windows = new();

    public static void Register(IGameplayWindow window)
    {
        if (window != null && !windows.Contains(window))
        {
            windows.Add(window);
        }
    }

    public static void Unregister(IGameplayWindow window)
    {
        windows.Remove(window);
    }

    public static void OpenExclusive(IGameplayWindow windowToOpen)
    {
        for (int i = windows.Count - 1; i >= 0; i--)
        {
            IGameplayWindow window = windows[i];
            if (window == null)
            {
                windows.RemoveAt(i);
                continue;
            }

            if (window != windowToOpen && window.IsOpen)
            {
                window.Close();
            }
        }
    }

    public static void CloseAll()
    {
        OpenExclusive(null);
    }
}
