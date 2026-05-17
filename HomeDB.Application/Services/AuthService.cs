using HomeDB.Application.DTOs.Auth;
using HomeDB.Domain.Common;
using HomeDB.Domain.Entities;
using HomeDB.Domain.Exceptions;
using HomeDB.Domain.Interfaces;
using HomeDB.Domain.Interfaces.Repositories;
using HomeDB.Domain.Interfaces.Services;

namespace HomeDB.Application.Services
{
    public class AuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IPasswordHelper _passwordHelper;
        private readonly IJwtService _jwtService;

        //TODO Quitar hardcodeado en v2
        private const int AccessTokenExpirationMinutes = 30;
        private const int RefreshTokenExpirationDays = 7;

        //TODO Crear interface de logger para poder usarlo
        public AuthService(IUserRepository userRepository, IRefreshTokenRepository refreshTokenRepository,
                           IPasswordHelper passwordHelper, IJwtService jwtService)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _passwordHelper = passwordHelper;
            _jwtService = jwtService;
        }


        /// <summary>
        /// Registra el usuario recibido
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="cToken"></param>
        /// <returns></returns>
        /// <exception cref="UserAlreadyExistsException"></exception>
        public async Task<UserDto> RegisterAsync(RegisterDto dto, CancellationToken cToken)
        {
            //Comprobar si el username ya existe
            if (await _userRepository.UsernameExistsAsync(dto.Username, cToken))
                throw new UserAlreadyExistsException(dto.Username);

            //Hashear la contraseña
            string hashPassword = _passwordHelper.HashPassword(dto.Password);

            //Crear el objeto User
            User user = new User
            {
                Username = dto.Username,
                PasswordHash = hashPassword
            };

            //Asignarle el rol User por defecto creando un UserRole
            user.UserRoles = new List<UserRole>
            {
                new UserRole
                {
                    RoleId = (int)RolesList.User
                }
            };

            //Insertar el usuario
            await _userRepository.AddUserAsync(user, cToken);

            //Guardar cambios
            await _userRepository.SaveChangesAsync(cToken);

            //Loguear que el registro fue exitoso
            //TODO Implementar logger y loguear que el registro fue exitoso

            //Devolver un UserDto
            return new UserDto(user.Id, user.Username, user.CreatedAt);
        }

        /// <summary>
        /// Realiza el login del usuario recibido, devolviendo un TokenResponseDto con el access token y el refresh token
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="cToken"></param>
        /// <returns></returns>
        /// <exception cref="InvalidCredentialsException"></exception>
        public async Task<TokenResponseDto> LoginAsync(LoginDto dto, CancellationToken cToken)
        {
            //Obtener el usuario por su username
            User? user = await _userRepository.GetByUsernameWithRolesAsync(dto.Username, cToken);

            //Asegurarse de que el usuario no es null
            if (user == null)
                throw new InvalidCredentialsException();

            //Comprobar que la contraseña es correcta
            if (!_passwordHelper.VerifyPassword(dto.Password, user.PasswordHash))
                throw new InvalidCredentialsException();

            //Generar el access token
            string accesTokenString = _jwtService.GenerateAccessToken(user);

            //Generar el refresh token
            string refreshTokenString = _jwtService.GenerateRefreshToken();

            //Crear objeto a insertar
            RefreshToken refreshToken = new RefreshToken
            {
                Token = refreshTokenString,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpirationDays)
            };

            //Insertar tokens
            await _refreshTokenRepository.AddRefreshTokenAsync(refreshToken, cToken);

            //Guardar cambios en la DB
            await _refreshTokenRepository.SaveChangesAsync(cToken);

            //Devolver el Token y el refresh token
            return new TokenResponseDto(accesTokenString, DateTime.UtcNow.AddMinutes(AccessTokenExpirationMinutes), 
                refreshTokenString, DateTime.UtcNow.AddDays(RefreshTokenExpirationDays));
        }

        /// <summary>
        /// Realiza el refresh del token recibido
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="cToken"></param>
        /// <returns></returns>
        /// <exception cref="InvalidProgramException"></exception>
        public async Task<TokenResponseDto> RefreshAsync(RefreshRequestDto dto, CancellationToken cToken)
        {
            //Buscar si existe el refreshToken
            RefreshToken? refreshToken =  await _refreshTokenRepository.GetByTokenAsync(dto.RefreshToken, cToken);

            //Si no existe devolver 403 y marcarlo con un log
            if (refreshToken == null || refreshToken.IsRevoked || refreshToken.ExpiresAt <= DateTime.UtcNow)
                // TODO: log de seguridad al administrador cuando esté implementado el logger
                throw new InvalidRefreshTokenException();

            //Crear el nuevo refresh token
            string newRefreshTokenString = _jwtService.GenerateRefreshToken();

            //Si existe marcarlo como revoked
            refreshToken.IsRevoked = true;
            refreshToken.ReplacedByToken = newRefreshTokenString;

            //Crear el objeto del refreshToken
            RefreshToken newRefreshToken = new RefreshToken
            {
                Token = newRefreshTokenString,
                UserId = refreshToken.UserId,
                ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpirationDays)
            };

            //Crear el nuevo acces token
            string newAccesTokenRefresh = _jwtService.GenerateAccessToken(refreshToken.User!);

            //Guardar el nuevo refresh token
            await _refreshTokenRepository.AddRefreshTokenAsync(newRefreshToken, cToken);

            //Persistir los cambios en la DB
            await _refreshTokenRepository.SaveChangesAsync(cToken);

            //Devolver los dos tokens
            return new TokenResponseDto(newAccesTokenRefresh, DateTime.UtcNow.AddMinutes(AccessTokenExpirationMinutes), newRefreshTokenString, DateTime.UtcNow.AddDays(RefreshTokenExpirationDays));
        }

        /// <summary>
        /// Realiza el logout del refresh token recibido
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="cToken"></param>
        /// <returns></returns>
        public async Task LogoutAsync(RefreshRequestDto dto, CancellationToken cToken)
        {
            //Buscar si existe el refreshToken
            RefreshToken? refreshToken =  await _refreshTokenRepository.GetByTokenAsync(dto.RefreshToken, cToken);

            //Si no existe ignorar de manera silenciosa
            if(refreshToken == null || refreshToken.IsRevoked)
                return;

            //Si existe marcarlo como revoked
            refreshToken.IsRevoked = true;

            //Persistir los cambios en la DB
            await _refreshTokenRepository.SaveChangesAsync(cToken);
        }
    }
}