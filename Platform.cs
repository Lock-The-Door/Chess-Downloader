abstract class Platform
{
    protected HttpClient Client { get; private set; }

    protected Logger Logger { get; private set; }

    public Platform(string platform, string baseUrl, string? apiKey)
    {
        Logger = new Logger(platform);
        Client = new HttpClient()
        {
            BaseAddress = new Uri(baseUrl),
            DefaultRequestHeaders =
            {
                { "User-Agent", "ChessGameDownloader" },
            },
        };

        if (apiKey != null)
        {
            Client.DefaultRequestHeaders.Add("Authorization", "Bearer " + apiKey);
        }
    }

    abstract public Task DownloadGames(FileInfo path, string username);
}