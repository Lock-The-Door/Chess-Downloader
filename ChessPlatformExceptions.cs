class GamesNotFoundException : Exception
{
    public GamesNotFoundException(string platform) : base(platform + ": No games found for this player. (Have you typed the correct username?)")
    {
    }
}

class GameRequestFailedException : Exception
{
    public GameRequestFailedException(string platform, string action, string? reason) : base($"{platform}: Failed to {action} ({reason ?? "Unknown Reason"}). Try again later.")
    {
    }
}