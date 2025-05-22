using Azure.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RAppsAPI.Data;
using RAppsAPI.Models;
using RAppsAPI.Models.MPM;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RAppsAPI.Services
{
    public class AuthService(
        IConfiguration configuration,
        RDBContext context) : IAuthService
    {
        
        // Checks credentials and registers user if logging in the first time.
        // Returns: 0 user logged in successfully
        //     1 user has logged in first time with valid creds, registered, awaiting activation
        //     2 user registered but is not yet activated by Admin
        //    -1 Invalid creds
        //    -2 Internal error/exception
        //    -3 Internal error while registering, check logs
        public async Task<LoginResponse> LoginAsync(string username, string password)
        {
            // TODO: User row has to be activated by Admin, authenticated API needed for Admin Role for this            
            // TODO: Authenticate username/pwd thru auth server            
            try
            {
                var success = AuthenticateUser(username, password);  // TODO: This must send back a profile DTO to fill missing user felds
                if (!success)
                {
                    return new LoginResponse
                    {
                        Code = -1,
                        Message = "Invalid",
                    };
                }
                // User creds are valid, check if user is regd with this app in Users
                var user = context.VUsers.Include(user => user.Role)
                    .Where(user => user.UserName == username).FirstOrDefault();
                if (user == null)
                {
                    // The user attempted to login first time
                    if (await RegisterUser(username) == 0)
                    {
                        return new LoginResponse
                        {
                            Code = 1,    // Registration succeeded
                            Message = "User registered",
                        };
                    }
                    else
                    {
                        return new LoginResponse
                        {
                            Code = -3,    // error while registering
                            Message = "Registration failed, internal error",
                        };
                    }                    
                }
                // User is present with username, but is row active?
                if (user.RStatus != (int)DBConstants.RStatus.Active)
                {
                    return new LoginResponse
                    {
                        Code = 2,    // Registered but inactive user
                        Message = "User inactive",
                    };
                }
                // Sign in the user, role will be updated by Admin in backend
                user.LastLoginDate = DateTime.Now;   // save login time
                await context.SaveChangesAsync();
                // Create & send back token
                //var userRole = user.Role;
                string token = CreateToken(user);
                // Role is retrieved from DB & set in JWT.
                return new LoginResponse
                {
                    Code = 0,    // successful login
                    Message = "",
                    IdToken = token,
                };
            }
            catch (Exception ex)
            {
                // TODO: Log the detailed ex with trace
                string exMsg = ex.Message;
                if (ex.InnerException != null)
                {
                    exMsg += "; InnerException:" + ex.InnerException.Message;
                }
                return new LoginResponse
                {
                    Code = (int)Constants.ResponseReturnCode.InternalError,
                    Message = $"Failed to login user:{username}, {exMsg}",
                };
            }

            
        }

        // Register the user by adding to DB
        // Returns: 0 success
        //      -1 Failed to fetch default role
        //      -2 Failed to add user
        //      -3 Failed to save user
        //      -4 Internal error/exception
        private async Task<int> RegisterUser(string username)
        {
            try
            {
                // Get default role
                var uaRole = context.VRoles.Where(role => role.Name == DBConstants.RoleName.Unassigned).FirstOrDefault();
                if (uaRole == null)
                {
                    // TODO: Log the detailed error: failed to set role
                    return -1;
                }
                // Get the max ID used so far
                var maxID = context.VUsers.Max(u => u.Id);
                maxID = Math.Max(DBConstants.MIN_NON_ADMIN_ID, maxID) + 1;                
                var user = new VUser()
                {
                    Id = maxID,
                    UserName = username,
                    FirstName = username,    // TODO: will be filled from auth profile later                    
                    FullName = username,
                    Email = username,
                    EmailConfirmed = true,
                    Role = uaRole,
                    Location = "Blr",        // TODO: will be filled from auth profile later
                    CreatedBy = DBConstants.ADMIN_USER_ID,
                    CreatedDate = DateTime.Now,                    
                    RStatus = (int)DBConstants.RStatus.Inactive    // row disabled(check with data model convention, use constant)
                };
                var result = await context.VUsers.AddAsync(user);
                if (result == null)
                {
                    // TODO: Log the detailed error
                    return -2;
                }
                var numWritten = await context.SaveChangesAsync();
                if (numWritten == 0)
                {
                    // TODO: Log the detailed error
                    return -3;
                }
                return 0;
            }
            catch (Exception ex)
            {
                // TODO: Log the detailed ex with trace
                string exMsg = ex.Message;
                if (ex.InnerException != null)
                {
                    exMsg += "; InnerException:" + ex.InnerException.Message;
                }
                return -4;
            }            
        }

        private string CreateToken(VUser user)
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
            if ( (username == "test" && password == "pwd") ||
                (username == "vis" && password == "pwd") ||
                (username == "Admin" && password == "pwd") )
                return true;
            return false;
        }

    }
}
