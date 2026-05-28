namespace HomeDB.Application.DTOs
{
    public class LogEntryDto
    {
        public int Id { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Exception { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public long DurationMs { get; set; }
    }

    public class GetLogsResponseDto
    {
        public IEnumerable<LogEntryDto> Items { get; set; } = [];
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class LogErrorSummaryItemDto
    {
        public string Operation { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class LogSlowOperationDto
    {
        public string Operation { get; set; } = string.Empty;
        public long DurationMs { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
    }

    public class GetLogsRequestDto
    {
        public string? Level { get; set; }
        public string? Operation { get; set; }
        public DateTimeOffset? From { get; set; }
        public DateTimeOffset? To { get; set; }
        public string? CorrelationId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class LogHealthResponseDto
    {
        public int ErrorsLastHour { get; set; }
        public int ErrorsLast24h { get; set; }
        public int WarningsLast24h { get; set; }
    }
}