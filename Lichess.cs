using System.Net.Http.Json;

class Lichess : Platform {
    public Lichess(string? token) : base("Lichess", "https://lichess.org/api/", token) {
    }

    public override async Task DownloadGames(FileInfo file, string username)
    {
        // fetch user info from lichess
        Logger.Log("Fetching user info...", Logger.LogLevel.Info);
        HttpResponseMessage? userProfileResponse = null;
        try
        {
            userProfileResponse = await Client.GetAsync("user/" + username);
            if (!userProfileResponse.IsSuccessStatusCode)
            {
                throw new Exception();
            }
        }
        catch
        {
            var e = new GameRequestFailedException("Lichess", "fetch user info", userProfileResponse?.ReasonPhrase);
            Logger.Error(e);
            throw e;
        }
        var userProfile = await userProfileResponse.Content.ReadFromJsonAsync<LichessUserProfile>();
        int totalGames = userProfile?.count?.GetValueOrDefault("all", 0) ?? 0;
        if (totalGames == 0)
        {
            var e = new GamesNotFoundException("Lichess");
            Logger.Error(e);
            throw e;
        }

        // fetch games from lichess
        Guid downloadStatusId = Logger.CreateStatus("Downloading games", 0, totalGames, Logger.LogLevel.Info);
        using StreamWriter? gameWriter = file.CreateText();
        Stream gameDownloadStream = Stream.Null;
        try
        {
            gameDownloadStream = await Client.GetStreamAsync("games/user/" + username);
        }
        catch (HttpRequestException)
        {
            var e = new GameRequestFailedException("Lichess", "stream games", null);
            Logger.Error(e);
            throw e;
        }
        using StreamReader reader = new(gameDownloadStream);
        string gameBlock = "";
        while (!reader.EndOfStream)
        {
            gameBlock += await reader.ReadLineAsync() + "\n";
            if (gameBlock.EndsWith("\n\n\n")) {
                // That's the end of a game
                var writeOp = gameWriter.WriteAsync(gameBlock);
                gameBlock = "";
                Logger.UpdateStatus(downloadStatusId, Logger.LogLevel.Info);
                await writeOp;
                await gameWriter.FlushAsync();
            }
            Logger.Log(gameBlock);
        }
        // Lichess ends with three new-lines, so we can safely ignore the last gameBlock
        reader.Close();
        gameWriter.Close();
    }
}