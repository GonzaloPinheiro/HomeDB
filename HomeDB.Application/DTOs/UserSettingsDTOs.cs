
namespace HomeDB.Application.DTOs
{
    //Se usa para recibir la información de configuración del usuario desde el cliente
    public class UpdateUserSettingsRequestDto
    {
        public string? Language { get; set; } = null;
        public string? Timezone { get; set; } = null;
    };

    //Se usa para enviar la información de configuración del usuario al cliente
    public record UserSettingsResponseDto(
        string Language,
        string Timezone
    );

    //Se usa para recibir la información de configuración del usuario desde el cliente para los ajustes de administración
    public class UpdateUserAdminSettingsRequestDto
    {
        public long? StorageLimitBytes { get; set; } = null;
        public long? MaxFileSizeBytes { get; set; } = null;
    };

    //Se usa para enviar la información de configuración del usuario al cliente para los ajustes de administración
    public record UserAdminSettingsResponseDto(
        long? StorageLimitBytes,
        long? MaxFileSizeBytes
    );

    //Se usa para enviar la información de configuración del usuario al cliente para el perfil del usuario
    public record UserProfileDto(
        UserSettingsResponseDto Settings,
        UserAdminSettingsResponseDto Limits
    );
}