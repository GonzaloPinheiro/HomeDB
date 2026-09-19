using System.ComponentModel.DataAnnotations;
using HomeDB.Domain.Common.Enums;
using HomeDB.Domain.Entities;

namespace HomeDB.Application.DTOs
{
    //Se usa para aplicar filtros y paginación al consultar el historial de backups
    public class GetBackupHistoryRequestDto
    {
        public BackupLevel? Level { get; set; }
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;
        [Range(1, 200)]
        public int PageSize { get; set; } = 50;
    }

    //Se usa para la respuesta paginada del historial de backups
    public record GetBackupHistoryResponseDto
    {
        public IEnumerable<BackupAuditEntry> Items { get; init; } = [];
        public int TotalCount { get; init; }
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalPages { get; init; }
    }
}
