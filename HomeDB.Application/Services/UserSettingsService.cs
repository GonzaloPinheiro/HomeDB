using HomeDB.Application.DTOs;
using HomeDB.Domain.Entities;
using HomeDB.Domain.Exceptions;
using HomeDB.Domain.Interfaces;
using HomeDB.Domain.Interfaces.Repositories;

namespace HomeDB.Application.Services
{
    public class UserSettingsService
    {
        //Variables y objetos globales
        private readonly IUserSettingsRepository _settingsRepository;
        private readonly UserAdminSettingsService _userAdminSettingsService;
        private readonly ICurrentUserService _currentUserService;
        

        //Constructores
        public UserSettingsService(IUserSettingsRepository settingsRepository, UserAdminSettingsService userAdminSettingsService, ICurrentUserService currentUserService)
        {
            _settingsRepository = settingsRepository;
            _userAdminSettingsService = userAdminSettingsService;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Obtiene la configuración del usuario actual.
        /// </summary>
        public async Task<UserSettingsResponseDto> GetSettingsForCurrentUserAsync(CancellationToken cToken)
        {
            //Obtiene el Id del usuario de la solicitud actual
            int userId = _currentUserService.UserId;

            //Obtiene la configuración del usuario desde el repositorio
            UserSettings settings = await _settingsRepository.GetByUserIdAsync(userId, cToken)
                ?? throw new UserSettingsNotFoundException(userId);

            return MapToDto(settings);
        }

        /// <summary>
        /// Obtiene el perfil completo del usuario actual, incluyendo la configuración y los límites de administración.
        /// </summary>
        public async Task<UserProfileDto> GetProfileForCurrentUserAsync(CancellationToken cToken)
        {
            //Obtener la configuración del usuario y los límites de administración de manera concurrente
            Task<UserSettingsResponseDto> settingsTask = GetSettingsForCurrentUserAsync(cToken);
            Task<UserAdminSettingsResponseDto> limitsTask = _userAdminSettingsService.GetAdminSettingsForCurrentUserAsync(cToken);

            //Esperar a que ambas tareas se completen
            await Task.WhenAll(settingsTask, limitsTask);

            //Devolver el resultado combinado en un DTO de perfil de usuario
            return new UserProfileDto(
                Settings: settingsTask.Result,
                Limits: limitsTask.Result
            );
        }

        /// <summary>
        /// Actualiza la configuración del usuario actual con los valores proporcionados en el DTO.
        /// </summary>
        public async Task<UserSettingsResponseDto> UpdateSettingsForCurrentUserAsync(UpdateUserSettingsRequestDto dto, CancellationToken cToken)
        {
            //Obtiene el Id del usuario de la solicitud actual
            int userId = _currentUserService.UserId;

            //Obtiene la configuración del usuario desde el repositorio
            UserSettings settings = await _settingsRepository.GetByUserIdAsync(userId, cToken, false)
                ?? throw new UserSettingsNotFoundException(userId);

            //Actualiza los campos de configuración si se proporcionan en el DTO
            if (!string.IsNullOrWhiteSpace(dto.Language)) settings.Language = dto.Language;
            if (!string.IsNullOrWhiteSpace(dto.Timezone)) settings.Timezone = dto.Timezone;

            //Persistir los cambios en la base de datos
            await _settingsRepository.SaveChangesAsync(cToken);

            //Devuelve la configuración actualizada como un DTO
            return MapToDto(settings);
        }

        /// <summary>
        /// Mapea un objeto UserSettings a un DTO UpdateUserSettingsResponseDto.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private static UserSettingsResponseDto MapToDto(UserSettings s)
        {
            return new UserSettingsResponseDto(
                Language: s.Language,
                Timezone: s.Timezone
            );
        }
    }
}