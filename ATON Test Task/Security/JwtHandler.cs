using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ATON_Test_Task.Data;
using ATON_Test_Task.Models;
using Microsoft.IdentityModel.Tokens;

namespace ATON_Test_Task.Security;

public static class JwtHandler {
    
    private static string _secretKey = "s,VjI(x:]37q1;GZe<f34ChpNx:]37q1x:]37q1";
    
    public static readonly TokenValidationParameters JwtParameters = new() {
       ValidateIssuer = false,
       ValidateAudience = false,
       ValidateLifetime = true,
       ClockSkew = TimeSpan.Zero,
       IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey))
    };
    
    public static async Task<string> GenerateJwtToken(UserLoginRequest user) {
        var claims = new[] {
            new Claim(ClaimTypes.Name, user.Login),
            new Claim(ClaimTypes.Role, await UsersRepository.GetUserRightAsync(user.Login) ? "Admin" : "User"),
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    
        var token = new JwtSecurityToken(
            issuer: "ATON_Test_Task",
            audience: "user",
            claims: claims,
            expires: DateTime.Now.AddHours(24), 
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}