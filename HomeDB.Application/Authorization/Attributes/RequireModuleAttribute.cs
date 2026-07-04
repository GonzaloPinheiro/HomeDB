using HomeDB.Domain.Common.Enums;
using Microsoft.AspNetCore.Authorization;

namespace HomeDB.Application.Authorization.Attributes
{
    //Attribute para requerir un módulo específico en un endpoint
    public class RequireModuleAttribute : AuthorizeAttribute
    {
        public RequireModuleAttribute(AppModules module)
            : base(policy: $"Module.{module}")
        {
        }
    }
}