class Logger
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }
    public static LogLevel Level { get; set; } = LogLevel.Info;

    public string SourceName { get; private set; }

    private class Status {
        public string Message { get; set; }
        public int Progress { get; set; }
        public int Total { get; set; }

        public Status(string message, int progress, int total) {
            Message = message;
            Progress = progress;
            Total = total;
        }

        public override string ToString() {
            if (Total == 0) {
                return $"{Message}...";
            }
            if (Progress >= Total) {
                return $"{Message} [Done]";
            }

            return $"{Message}... [{Progress}/{Total}]";
        }
    }
    private readonly Dictionary<Guid, Status> _statuses = new();
    private Status GetStatus(Guid id) {
        if (_statuses.ContainsKey(id)) {
            return _statuses[id];
        }
        throw new Exception("No status with this id");
    }

    public Logger(string sourceName)
    {
        SourceName = sourceName;
    }

    public void Log(string message, LogLevel level = LogLevel.Debug)
    {
        if (level >= Level)
        {
            Console.WriteLine($"[{DateTime.Now:g}] [{SourceName}] [{level}]: {message}");
        }
    }

    public Guid CreateStatus (string message, int progress, int total, LogLevel level = LogLevel.Debug)
    {
        var status = new Status(message, progress, total);
        Log(status.ToString(), level);
        var id = Guid.NewGuid();
        _statuses.Add(id, status);
        return id;
    }

    public void UpdateStatus(Guid id, LogLevel level = LogLevel.Debug) {
        Status status = GetStatus(id);
        UpdateStatus(status, status.Progress + 1, status.Total, level);
    }
    public void UpdateStatus(Guid id, int progress, LogLevel level = LogLevel.Debug)
    {
        Status status = GetStatus(id);
        UpdateStatus(status, progress, status.Total, level);
    }
    public void UpdateStatus(Guid id, int progress, int total, LogLevel level = LogLevel.Debug)
    {
        Status status = GetStatus(id);
        UpdateStatus(status, progress, total, level);
    }
    private void UpdateStatus(Status status, int progress, int total, LogLevel level = LogLevel.Debug)
    {
        status.Progress = progress;
        status.Total = total;
        Log(status.ToString(), level);

        if (progress >= total && total != 0)
        {
            _statuses.Remove(_statuses.First(s => s.Value == status).Key);
        }
    }

    public void Error(Exception exception) {
        Log(exception.ToString(), LogLevel.Error);
        Environment.Exit(1);
    }
}