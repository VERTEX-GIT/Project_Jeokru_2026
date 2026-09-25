using System;

[Serializable]
public sealed class GameSaveData
{
    public int version = 1;
    public string savedAtUtc;
    public GameTimeSaveData gameTime = new();
}

[Serializable]
public sealed class GameTimeSaveData
{
    public int day = 1;
    public int time = 1;
}
