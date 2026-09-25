using System;
using System.Collections.Generic;

[Serializable]
public sealed class GameSaveData
{
    public int version = 1;
    public string savedAtUtc;
    public GameTimeSaveData gameTime = new();
    public List<ResourceAmountSaveData> resources = new();
}

[Serializable]
public sealed class GameTimeSaveData
{
    public int day = 1;
    public int time = 1;
}

[Serializable]
public sealed class ResourceAmountSaveData
{
    public ResourceType resourceType;
    public int amount;
}
