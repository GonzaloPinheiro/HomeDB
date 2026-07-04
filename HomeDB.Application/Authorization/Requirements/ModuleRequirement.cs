using HomeDB.Domain.Common.Enums;
using Microsoft.AspNetCore.Authorization;

namespace HomeDB.Application.Authorization.Requirements
{
    //Requirement para verificar si el usuario tiene acceso a un módulo específico
    public class ModuleRequirement : IAuthorizationRequirement
    {
        public AppModules Module { get; }

        public ModuleRequirement(AppModules module)
        {
            Module = module;
        }
    }
}