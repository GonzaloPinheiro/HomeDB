using System.ComponentModel.DataAnnotations;

namespace HomeDB.Application.DTOs
{
    public class GetUsersRequestDto
    {
        public int? userId { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public DateTimeOffset? From { get; set; }
        public DateTimeOffset? To { get; set; }
        public int? roleId { get; set; }
        public string? RoleName { get; set; }
        public int Page { get; set; } = 1;
        [Range(1, 200)]
        public int PageSize { get; set; } = 50;
    }

    public record DeleteUserResponseDto(int UserId, string Username);

    public record UserSummaryDto(
        int Id,
        string Username,
        string Email,
        DateTime CreatedAt,
        IEnumerable<string> Roles);

    public record GetUsersResponseDto
    {
        public IEnumerable<UserSummaryDto> Users { get; set; } = [];
        public int TotalCount { get; set; }
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalPages { get; init; }
    }
}