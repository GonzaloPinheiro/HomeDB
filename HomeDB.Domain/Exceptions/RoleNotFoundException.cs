
namespace HomeDB.Domain.Exceptions
{
    public class RoleNotFoundException : Exception
    {
        public RoleNotFoundException(int id)
            : base($"Role with id {id} was not found.") { }
    }
}
