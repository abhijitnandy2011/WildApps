using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RAppsAPI.Entities;
using RAppsAPI.Models;

namespace RAppsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        public static RAppUser user = new();
        
        [HttpPost("register")]
        // This endpoint will not store any pwd. Its will authenticate u/pw and send out
        // an email to confirm registration.
        public ActionResult<RAppUser> Register(UserDto request)
        {
            var hashedPassword = new PasswordHasher<RAppUser>()
                .HashPassword(user, request.Password);
            user.UserName = request.Username;
            return Ok(user);
        }


    }
}
