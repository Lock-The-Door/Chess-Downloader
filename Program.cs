
Console.WriteLine("Welcome to the chess game downloader!");
int platform = 0;
while (platform < 1 || platform > 2)
{
    Console.WriteLine("Please select a platform:");
    Console.WriteLine("1. Chess.com");
    Console.WriteLine("2. Lichess.org");
    Console.Write("Platform: ");
    string? rawPlatform = Console.ReadLine();
    if (rawPlatform == null)
    {
        Console.WriteLine("End of input stream");
        return;
    }
    if (!int.TryParse(rawPlatform, out platform))
    {
        Console.WriteLine("Please select a platform by its index.");
    }
}

string username = "";
while (username == "")
{
    Console.Write("Username: ");
    string? rawUsername = Console.ReadLine();
    if (rawUsername == null)
    {
        Console.WriteLine("End of input stream");
        return;
    }
    username = rawUsername;
}

Console.Write("Output directory: ");
string outputFile = Path.Join(Directory.GetCurrentDirectory(), $"{username}-platform{platform}_{DateTime.Now:g}.pgn");
Console.WriteLine(outputFile);

Console.Write("API key (optional): ");
string? apiKey = Console.ReadLine();
if (apiKey == null)
{
    Console.WriteLine("End of input stream");
    return;
}
if (apiKey == "")
{
    apiKey = null;
}

Console.WriteLine("Performining the magic...");
Platform? platformObj = platform switch
{
    1 => new ChessDotCom(),
    2 => new Lichess(apiKey),
    _ => null
};
if (platformObj == null)
{
    Console.WriteLine("This platform is not supported yet.");
    return;
}

platformObj.DownloadGames(new FileInfo(outputFile), username).Wait();

Console.WriteLine("Saved to " + outputFile);
Console.WriteLine("Done! Press any key to exit...");
Console.ReadKey();