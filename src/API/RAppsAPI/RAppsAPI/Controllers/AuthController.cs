using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RAppsAPI.Data;
using RAppsAPI.Entities;
using RAppsAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace RAppsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IConfiguration configuration, RDBContext dbContext) : ControllerBase
    {
        [HttpPost("register")]
        // This endpoint will not store any pwd.
        // It will create an user entry with given username & email
        // and send an email to confirm the email. User login will be disabled
        // till email confirmation. On email confirm, login will be enabled.
        // On login auth will be done with ext Auth server.
        // If success, missing user fields will be filled in &
        // accessToken + refreshToken issued.
        public async Task<IActionResult> Register(RegisterUserDto request)
        {
            // TODO:  check if username already exists, validate username with auth server,
            // check if email present, email must be unique.
            // check if same email exists for another user & ONLY then add user with
            // unconfirmed email. Login must be disabled till email confirmed.
            // NOTE: Use a service
            try
            {
                var uaRole = dbContext.Roles.Where(role => role.Name == DBConstants.RoleName.Unassigned).FirstOrDefault();
                if (uaRole == null)
                {
                    // TODO: Log the detailed error: failed to set role
                    return BadRequest("Failed to register");
                }
                var emailToken = Guid.NewGuid().ToString();
                var user = new VUsers()
                {
                    UserName = request.Username,
                    Email = request.Email,
                    EmailConfirmed = false,
                    EmailToken = emailToken,
                    CreatedDate = DateTime.Now,
                    Role = uaRole,
                    RStatus = (int)DBConstants.RStatus.Inactive    // row disabled(check with data model convention, use constant)
                };
                var result = await dbContext.Users.AddAsync(user);
                await dbContext.SaveChangesAsync();
                if (result == null)
                {
                    // TODO: Log the detailed error
                    return BadRequest($"Failed to register user");
                }
                // TODO: Send email                
                var confirmationLink = Url.Action(nameof(ConfirmEmail), "Auth", new { token = emailToken, email = user.Email }, Request.Scheme);
                //var message = new Message(new string[] { user.Email }, "Confirmation email link", confirmationLink, null);
                //await _emailSender.SendEmailAsync(message);
                //await _userManager.AddToRoleAsync(user, "Visitor");
                return Ok($"Registered user '{request.Username}' successfully.\n{confirmationLink}");
            }
            catch (Exception ex)
            {
                // TODO: Log the detailed error
                return StatusCode(500, "Error");
            }
        }


        [HttpGet("confirmEmail")]
        public async Task<IActionResult> ConfirmEmail(string token, string email)
        {
            try
            {
                var user = dbContext.Users.Where(user => 
                user.Email == email && 
                !user.EmailConfirmed &&
                user.EmailToken == token).FirstOrDefault();
                if (user == null)
                {
                    // TODO: Log the detailed error: no user found with email/unconfirmed email
                    return BadRequest("Invalid");
                }
                user.EmailConfirmed = true;
                user.EmailToken = null;
                user.RStatus = (int)DBConstants.RStatus.Active;
                await dbContext.SaveChangesAsync();
                //var result = await _userManager.ConfirmEmailAsync(user, token);
                //return (result.Succeeded ? Ok("") : StatusCode(500, "Error"));
                return Ok("Email confirmed!");
            }
            catch (Exception ex)
            {
                // TODO: Log the detailed error
                return StatusCode(500, "Error");
            }
        }
        

        // TODO: should username/pwd be supplied here or use external entity?
        // TODO: Should jwts be in memory & be verified in each API call?
        [HttpPost("login")]
        public async Task<IActionResult> Login(string username, string password)
        {
            // TODO: User row has to be activated by Admin, authenticated API needed for Admin Role for this            
            // TODO: Authenticate username/pwd thru auth server            
            try
            {
                var success = AuthenticateUser(username, password);  // TODO: This must send back a profile DTO to fill missing user felds
                if (!success)
                {
                    // TODO: Log the detailed error: credentials failed in auth server
                    return BadRequest("Invalid Credentials");
                }
                // User creds are valid, check if user is regd with this app in Users
                var user = dbContext.Users.Include(user => user.Role)
                    .Where(user => user.UserName == username).FirstOrDefault();
                if (user == null)
                {
                    // TODO: Log the detailed error: no user with username
                    return BadRequest($"Invalid Credentials");
                }
                // User is present with username, but is row active?
                if (user.RStatus != (int)DBConstants.RStatus.Active)
                {
                    // TODO: Log the detailed error: user row is inactive/login blocked, was email confirmed?
                    return BadRequest($"Invalid Credentials");
                }
                // Sign in the user, role will be updated by Admin in backend
                user.LastLoginDate = DateTime.Now;   
                await dbContext.SaveChangesAsync();
                // Create & send back token
                var userRole = user.Role;
                string token = CreateToken(user);
                // Role is retrieved from DB & set in JWT.
                return Ok(token);
            }
            catch (Exception ex)
            { 
                // TODO: Log the detailed ex with trace
                return StatusCode(500, "Error");
            }
        }


        private string CreateToken(VUsers user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(ClaimTypes.Role, user.Role.Name)
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
