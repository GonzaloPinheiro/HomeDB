using HomeDB.Application.DTOs.Auth;
using HomeDB.Domain.Common;
using HomeDB.Domain.Entities;
using HomeDB.Domain.Exceptions;
using HomeDB.Domain.Interfaces;

namespace HomeDB.Application.Services
{
    public class AuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IPasswordHelper _passwordHelper;
        private readonly IJwtService _jwtService;

        //TODO Crear interface de logger para poder usarlo
        public AuthService(IUserRepository userRepository, IRefreshTokenRepository refreshTokenRepository,
                           IPasswordHelper passwordHelper, IJwtService jwtService)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _passwordHelper = passwordHelper;
            _jwtService = jwtService;
        }


        //Registra el usuario recibido
        public async Task<UserDto> RegisterAsync(RegisterDto dto, CancellationToken cToken)
        {
            //Comprobar si el username ya existe → si sí, lanzar UserAlreadyExistsException
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

        //Realiza el login del usuario recibido, devolviendo un TokenResponseDto con el access token y el refresh token
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
            

            //Generar el refresh token

            //Guardar cambios en la DB

            //Devolver el Token y el refresh token

        }

        //Realiza el refresh del token recibido
        public async Task<TokenResponseDto> RefreshAsync(RefreshRequestDto dto, CancellationToken cToken)
        {

        }

        //Realiza el logout del refresh token recibido
        public async Task LogoutAsync(RefreshRequestDto dto, CancellationToken cToken)
        {

        }
    }
}