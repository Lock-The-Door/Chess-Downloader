using System.Net;
using System.Net.Http.Json;

class ChessDotCom : Platform
{
    const int MAX_THREAD_COUNT = 5; // TODO: make this configurable

    public ChessDotCom() : base("Chess.com", "https://api.chess.com/pub/player/", null)
    {
    }

    public override async Task DownloadGames(FileInfo path, string username)
    {
        // fetch game months from chess.com
        Logger.Log("Fetching available months...", Logger.LogLevel.Info);
        string availableMonthsUrl = username + "/games/archives";
        HttpResponseMessage? availableMonthsResponse = null;
        try
        {
            availableMonthsResponse = await Client.GetAsync(availableMonthsUrl);
            if (!availableMonthsResponse.IsSuccessStatusCode)
            {
                throw new Exception();
            }
        }
        catch
        {
            var e = new GameRequestFailedException("Chess.com", "fetch available games", availableMonthsResponse?.ReasonPhrase);
            Logger.Error(e);
            throw e;
        }
        string[]? availableMonthsUrls = (await availableMonthsResponse.Content.ReadFromJsonAsync<Dictionary<string, string[]>>())?["archives"];
        if (availableMonthsUrls == null || availableMonthsUrls.Length == 0)
        {
            var e = new GamesNotFoundException("Chess.com");
            Logger.Error(e);
            throw e;
        }

        // fetch games from chess.com
        Guid downloadStatusId = Logger.CreateStatus("Downloading games", 0, availableMonthsUrls.Length);
        DirectoryInfo tempDir = Directory.CreateTempSubdirectory("Chess.com-GameDownload");
        try { path.Delete(); } catch { Logger.Log("No existing symlink to delete"); }
        path.CreateAsSymbolicLink(tempDir.FullName); // create symlink to temp dir for now
        List<Task> downloadTasks = new();
        foreach (string monthUrl in availableMonthsUrls)
        {
            if (downloadTasks.Count >= MAX_THREAD_COUNT)
            {
                await Task.WhenAny(downloadTasks);
                downloadTasks.RemoveAll(t => t.IsCompleted);
            }

            downloadTasks.Add(DownloadMonth(downloadStatusId, monthUrl, tempDir));
        }
        await Task.WhenAll(downloadTasks);
        path.Delete(); // delete symlink

        // merge games
        Guid mergeStatusId = Logger.CreateStatus("Merging games", 0, availableMonthsUrls.Length);
        string[] pgnFiles = Directory.GetFiles(tempDir.FullName, "*.pgn");
        Array.Sort(pgnFiles);
        Array.Reverse(pgnFiles);
        if (pgnFiles.Length == 0)
        {
            var e = new GamesNotFoundException("Chess.com");
            Logger.Error(e);
            throw e;
        }
        using StreamWriter mergedGames = new(path.FullName);
        foreach (string pgnFile in pgnFiles)
        {
            using StreamReader pgn = new(pgnFile);
            await pgn.BaseStream.CopyToAsync(mergedGames.BaseStream);
            mergedGames.Write('\n');
            await mergedGames.FlushAsync();
            Logger.UpdateStatus(mergeStatusId, Logger.LogLevel.Info);
        }

        // Cleanup
        tempDir.Delete(true);
    }

    private async Task DownloadMonth (Guid statusId, string monthUrl, DirectoryInfo tempDir)
    {
        string monthGamesUrl = monthUrl + "/pgn";
        HttpResponseMessage? monthGamesResponse = null;
        try 
        {
            while (monthGamesResponse == null || monthGamesResponse.StatusCode == HttpStatusCode.TooManyRequests)
            {
                if (monthGamesResponse != null)
                {
                    double retryAfter = monthGamesResponse.Headers.RetryAfter?.Delta?.TotalMilliseconds ?? Random.Shared.Next(5000, 30000);
                    Logger.Log($"Too many requests, waiting {retryAfter/1000} seconds...", Logger.LogLevel.Warning);
                    Thread.Sleep((int)retryAfter);
                }
                monthGamesResponse = await Client.GetAsync(monthGamesUrl);
            }

            monthGamesResponse.EnsureSuccessStatusCode();
        }
        catch
        {
            var e = new GameRequestFailedException("Chess.com", "download games", monthGamesResponse?.ReasonPhrase);
            Logger.Error(e);
            throw e;
        }

        using Stream monthGamesStream = await monthGamesResponse.Content.ReadAsStreamAsync();
        string fileName = string.Join('-', monthUrl.Split('/').TakeLast(2)) + ".pgn";
        using FileStream file = File.Create(tempDir.FullName + "/" + fileName);
        await monthGamesStream.CopyToAsync(file);

        Logger.UpdateStatus(statusId, Logger.LogLevel.Info);
    }
}