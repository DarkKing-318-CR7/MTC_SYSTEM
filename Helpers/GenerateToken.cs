using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Helpers
{
    public class GenerateToken
    {
        public static string GenerateAccessToken(LoginRequest loginRequest, int expiredInMinutes)
        {
            Claim[] claims = new Claim[3]
            {
                new Claim("sub", loginRequest.Subject),
                new Claim(ClaimTypes.Role, loginRequest.UserRole),
                new Claim("UserEmail", loginRequest.UserEmail)
            };
            SigningCredentials signingCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(loginRequest.SeretKey)), "HS256");
            string issuer = loginRequest.Issuer;
            string audience = loginRequest.Audience;
            DateTime? expires = DateTime.UtcNow.AddMinutes(expiredInMinutes);
            SigningCredentials signingCredentials2 = signingCredentials;
            JwtSecurityToken token = new JwtSecurityToken(issuer, audience, claims, null, expires, signingCredentials2);
            string item = new JwtSecurityTokenHandler().WriteToken(token);
            
            return item;
        }
    }
    public class LoginRequest
    {
        public string Subject { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string SeretKey { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
