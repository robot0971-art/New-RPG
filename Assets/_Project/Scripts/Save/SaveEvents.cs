using System;

public static class SaveEvents
{
    public static event Action SaveRequested;

    public static void RequestSave()
    {
        SaveRequested?.Invoke();
    }
}
