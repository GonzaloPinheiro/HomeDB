using HomeDB.Common;
using HomeDB.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HomeDB.Controllers
{
    [Authorize(Roles = nameof(RolesList.Admin))]
    [EnableRateLimiting(nameof(RateLimiterNames.Global))]
    [Route("api/admin")]
    public class UsersController : ApiControllerBase
    {

    }
}