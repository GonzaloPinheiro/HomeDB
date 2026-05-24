using HomeDB.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace HomeDB.Infrastructure.Security
{
    public class CurrentUserService : ICurrentUserService
    {
        //Variables y objetos globales
        private readonly IHttpContextAccessor _httpContextAccessor;

        //Constructores
        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        //Obtener el user id del token JWT
        public int UserId =>
            int.Parse(_httpContextAccessor.HttpContext!.User.FindFirstValue("userId")!);

        //Obtener el username del token JWT
        public string Username =>
            _httpContextAccessor.HttpContext!.User.FindFirstValue("username")!;

        //Obtener la dirección IP del token JWT
        public string? IpAddress =>
            _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    }
}