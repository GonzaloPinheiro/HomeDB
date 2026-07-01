using HomeDB.Application.DTOs;
using HomeDB.Application.Options;
using HomeDB.Domain.Entities;
using HomeDB.Domain.Exceptions;
using HomeDB.Domain.Interfaces;
using HomeDB.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Options;

namespace HomeDB.Application.Services
{
    public class UserAdminSettingsService
    {
        //Variables y objetos globales
        private readonly IUserAdminSettingsRepository _adminSettingsRepository;
        private readonly IUserRepository _userRepository;
        private readonly StorageOptions _storageOptions;
        private readonly ICurrentUserService _currentUserService;

        //Constructores
        public UserAdminSettingsService(IUserAdminSettingsRepository adminSettingsRepository, IUserRepository userRepository, 
                                        IOptions<StorageOptions> storageOptions, ICurrentUserService currentUserService)
        {
            _adminSettingsRepository = adminSettingsRepository;
            _userRepository = userRepository;
            _storageOptions = storageOptions.Value;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Obtiene la configuración de administración de un usuario por su ID.
        /// </summary>
        public async Task<UserAdminSettingsResponseDto> GetAdminSettingsAsync(int userId, CancellationToken cToken)
        {
            //Comprobar si el usuario existe en la base de datos
            bool exists = await _userRepository.UserExistsAsync(userId, cToken);
            if (!exists)
                throw new UserNotFoundException(userId);

            //Obtener la configuración de administración del usuario
            UserAdminSettings settings = await _adminSettingsRepository.GetByUserIdAsync(userId, cToken)
                ?? throw new UserSettingsNotFoundException(userId);

            //Devolver la configuración de administración del usuario como DTO
            return MapToDto(settings);
        }

        public async Task<UserAdminSettingsResponseDto> GetAdminSettingsForCurrentUserAsync(CancellationToken cToken)
        {
            //Obtener el ID del usuario actual desde el servicio de usuario actual
            int userId = _currentUserService.UserId;

            //Obtener la configuración efectiva (con fallback a appsettings cuando el valor es null)
            UserAdminSettings effectiveSettings = await GetEffectiveSettingsAsync(userId, cToken);

            //Devolver la configuración efectiva como DTO
            return MapToDto(effectiveSettings);
        }

        /// <summary>
        /// Actualiza la configuración de administración de un usuario por su ID y el DTO de solicitud.
        /// </summary>
        public async Task<UserAdminSettingsResponseDto> UpdateAdminSettingsAsync(int userId, UpdateUserAdminSettingsRequestDto dto, CancellationToken cToken)
        {
            //Comprobar si el usuario existe en la base de datos
            bool exists = await _userRepository.UserExistsAsync(userId, cToken);
            if (!exists)
                throw new UserNotFoundException(userId);


            //Obtener la configuración de administración del usuario
            UserAdminSettings settings = await _adminSettingsRepository.GetByUserIdAsync(userId, cToken, asNoTracking: false)
                ?? throw new UserSettingsNotFoundException(userId);

            //Actualizar los valores de configuración de administración del usuario
            settings.StorageLimitBytes = dto.StorageLimitBytes;
            settings.MaxFileSizeBytes = dto.MaxFileSizeBytes;

            //Persistir los cambios en la base de datos
            await _adminSettingsRepository.SaveChangesAsync(cToken);

            //Devolver la configuración de administración del usuario actualizada como DTO
            return MapToDto(settings);
        }

        /// <summary>
        /// Obtiene la configuración de administración efectiva de un usuario por su ID, aplicando valores predeterminados desde appsettings cuando los valores son nulos.
        /// </summary>
        public async Task<UserAdminSettings> GetEffectiveSettingsAsync(int userId, CancellationToken cToken)
        {
            //Comprobar si el usuario existe en la base de datos
            bool exists = await _userRepository.UserExistsAsync(userId, cToken);
            if (!exists)
                throw new UserNotFoundException(userId);

            //Obtener la configuración de administración del usuario
            UserAdminSettings settings = await _adminSettingsRepository.GetByUserIdAsync(userId, cToken)
                ?? throw new UserSettingsNotFoundException(userId);

            //Fallback a appsettings cuando el valor es null
            return new UserAdminSettings
            {
                UserId = settings.UserId,
                StorageLimitBytes = settings.StorageLimitBytes ?? _storageOptions.StorageLimitBytes,
                MaxFileSizeBytes = settings.MaxFileSizeBytes ?? _storageOptions.MaxFileSizeBytes
            };
        }

        //Mapea la entidad UserAdminSettings a un DTO UserAdminSettingsResponseDtos
        private static UserAdminSettingsResponseDto MapToDto(UserAdminSettings s)
        {
            return new UserAdminSettingsResponseDto(
                StorageLimitBytes: s.StorageLimitBytes,
                MaxFileSizeBytes: s.MaxFileSizeBytes
            );
        }
    }
}