
namespace HomeDB.Domain.Exceptions
{
    public class ModuleAccessDeniedException : Exception
    {
        public ModuleAccessDeniedException(string moduleName)
       : base($"Access to module '{moduleName}' is not enabled for this user.") { }
    }
}