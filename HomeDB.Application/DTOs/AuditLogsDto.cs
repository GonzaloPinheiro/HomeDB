using HomeDB.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace HomeDB.Application.DTOs
{
    public class GetAuditLogsRequestDto
    {
        public int? UserId { get; set; }
        public string? userName { get; set; }
        public string? Action { get; set; }
        public string? ResourceType { get; set; }
        public DateTimeOffset? From { get; set; }
        public DateTimeOffset? To { get; set; }
        public int Page { get; set; } = 1;
        [Range(1, 200)]
        public int PageSize { get; set; } = 50;
    }

    public record GetAuditLogsResponseDto
    {
        public IEnumerable<AuditLogEntry> Items { get; init; } = [];
        public int TotalCount { get; init; }
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalPages { get; init; }
    }
}