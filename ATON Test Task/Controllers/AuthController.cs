using ATON_Test_Task.Data;
using ATON_Test_Task.Models;
using ATON_Test_Task.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATON_Test_Task.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginRequest user) {
        if (!await UsersRepository.ValidateCredentialsAsync(user.Login, user.Password)) return new UnauthorizedResult();
        return new OkObjectResult(JwtHandler.GenerateJwtToken(user));
    }
}