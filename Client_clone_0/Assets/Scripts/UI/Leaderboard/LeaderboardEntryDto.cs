using System;

[Serializable]
public class LeaderboardEntryDto
{
    // PascalCase khop JSON server tra ve: {"Rank":...,"CharacterName":...,"Value":...,"Extra":...}
    public int    Rank;
    public string CharacterName;
    public long   Value;
    public string Extra;
}

[Serializable]
public class LeaderboardResponseWrapper
{
    public LeaderboardEntryDto[] items;
}
