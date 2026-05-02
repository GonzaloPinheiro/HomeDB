using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeDB.Application.DTOs.Auth
{
    //DTO para el registro de el usaurio
    public record RegisterDto(string Username, string Password);

    //DTO para el login del usuario
    public record LoginDto(string Username, string Password);

    //DTO para la respuesta del usuario después de un login o registro exitoso
    public record UserDto(int Id, string Username, DateTime CreatedAt);

    //DTO para la respuesta del token después de un login o refresh exitoso
    public record TokenResponseDto(
        string AccessToken,
        DateTime AccessTokenExpiresAt,
        string RefreshToken,
        DateTime RefreshTokenExpiresAt
    );

    //DTO para la solicitud de refresh token
    public record RefreshRequestDto(string RefreshToken);
}
