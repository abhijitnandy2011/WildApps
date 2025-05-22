using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RAppsAPI.Data;
using RAppsAPI.Models;
using RAppsAPI.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace RAppsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IAuthService _authService) : ControllerBase
    {
        
        // TODO: Hash the pwd in frontend?
        // TODO: Should jwts be in memory & be verified in each API call?
        [HttpPost("login")]
        public async Task<IActionResult> Login(string username, string password)
        {
            var response = await _authService.LoginAsync(username, password);
            return Ok(response);
        }
        

        [Authorize]
        [HttpGet("authep")]
        public IActionResult AuthenticatedEndpoint()
        {
            //var claims = User.Identity.
            return Ok("You are authenticated!");
        }


        [Authorize(Roles ="Admin")]
        [HttpGet("adminonly")]
        public IActionResult AdminOnlyEp()
        {
            return Ok("You are Admin!");
        }

    }
}
