using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using RAppsAPI.Entities;
using RAppsAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace RAppsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IConfiguration configuration) : ControllerBase
    {        
        /*[HttpPost("register")]
        // This endpoint will not store any pwd.
        // It will create an user entry with given username & email
        // and send an email to confirm the email. User login will be disabled
        // till email confirmation. On email confirm, login will be enabled.
        // On login auth will be done with ext Auth server.
        // If success, missing user fields will be filled in &
        // accessToken + refreshToken issued.
        public async Task<IActionResult> Register(RegisterUserDto request)
        {
            // TODO: Validate username, check if email present, check if username already exists,
            // check if same email exists for another user & ONLY then add user with
            // unconfirmed email, so that login is kept disabled.
            // NOTE: Use a service
            var user = new RAppUser()
            {
                UserName = request.Username,
                Email = request.Email,               
                CreatedOn = DateTime.Now,
                RStatus = 1     // row disabled(check with data model convention, use constant)
            };
            var result = await _userManager.CreateAsync(user);
            if (result.Succeeded)
            {
                // TODO: Send email
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationLink = Url.Action(nameof(ConfirmEmail), "Auth", new { token, email = user.Email }, Request.Scheme);
                //var message = new Message(new string[] { user.Email }, "Confirmation email link", confirmationLink, null);
                //await _emailSender.SendEmailAsync(message);
                //await _userManager.AddToRoleAsync(user, "Visitor");

                return Ok($"Registered user '{request.Username}' successfully.\n{confirmationLink}");
            }
            return BadRequest($"Failed to register user '{request.Username}'");
        }

        [HttpGet("confirmEmail")]
        public async Task<IActionResult> ConfirmEmail(string token, string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return BadRequest("Invalid");
            }
            var result = await _userManager.ConfirmEmailAsync(user, token);
            return (result.Succeeded ? Ok("") : StatusCode(500, "Error"));
        }
        */

        [HttpPost("login")]
        public async Task<IActionResult> Login(string username, string password)
        {
            // TODO: User row has to be activated by Admin, authenticated API needed for Admin Role for this            
            // TODO: Authenticate username/pwd
            // TODO: Get user from app db first & check if user active, can sign-in first? Or first do username/pwd authentication?
            var success = AuthenticateUser(username, password);  // TODO: This must send back a profile DTO to fill missing user felds
            if(!success)
            {
                return BadRequest("Invalid Credentials");
            }
            /* var user = await _userManager.FindByNameAsync(username);
             if (user == null)
             {
                 return BadRequest($"Failed to login user: {username}");
             }
             // TODO: Check if user row is active before signing user in
             bool isUserActive = true;
             if(!isUserActive)
             {
                 return BadRequest($"Failed to login user: {username}");
             }
             // TODO: Check if user can sign-in with Identity. If email not verified, no signIn allowed.
             // Sign in the user
             //await _signInManager.SignInAsync(user, false);    // TODO: Set Role, capture login time
             bool isAuthenticated = User.Identity!.IsAuthenticated;  
             /*if (!isAuthenticated)
             {
                 return BadRequest($"Failed to authenticate user: {username}");
             }*/

            // Create & send back token
            //var roles = await _userManager.GetRolesAsync(user);
            var roles = new List<string> { "Admin" };
            var user = new User { UserName = "qw" };
            string token = CreateToken(user, roles);
            // Role is retrieved from DB & set in JWT. 
            return Ok(token);
        }


        private string CreateToken(User user, IList<string> roles)
        {
            

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(ClaimTypes.Role, roles[0])
            };
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration.GetValue<string>("AppSettings:Token")!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);
            var tokenDescriptor = new JwtSecurityToken(
                issuer: configuration.GetValue<string>("AppSettings:Issuer"),
                audience: configuration.GetValue<string>("AppSettings:Audience"),
                claims: claims,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }


        private bool AuthenticateUser(string username, string password)
        {
            if(username == "qw" && password=="qwe")
                return true;
            return false;
        }


        [Authorize]
        [HttpGet("authep")]
        public IActionResult AuthenticatedEndpoint()
        {
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
