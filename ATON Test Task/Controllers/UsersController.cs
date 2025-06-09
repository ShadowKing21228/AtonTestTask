using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using ATON_Test_Task.Data;
using ATON_Test_Task.Models;
using ATON_Test_Task.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATON_Test_Task.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public partial class UsersController : ControllerBase
{
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest user) {
        if (string.IsNullOrEmpty(user.Login) || string.IsNullOrEmpty(user.Password) || string.IsNullOrEmpty(user.Name))
            return new BadRequestObjectResult("All fields are required");
        
        if (!LoginPasswordRegex().IsMatch(user.Login) || !LoginPasswordRegex().IsMatch(user.Password) || !await UsersRepository.IsLoginUniqueAsync(user.Login)) 
            return new BadRequestObjectResult("User is not added");
        
        var value = User.FindFirst(ClaimTypes.Role)?.Value;
        var userRole = value != null && ClaimToRightConvert(value);
        var creatorLogin = User.FindFirst(ClaimTypes.Name)?.Value;
        
        if (user.Admin && !userRole) return new BadRequestObjectResult($"User is not added! You don`t have Admin rights");
        
        await UsersRepository.AddUserAsync(new User {
            Login = user.Login,
            Name = user.Name,
            Password = user.Password,
            Gender = user.Gender ?? 2,
            Birthday = user.Birthday,
            Admin = user.Admin,
            CreatedBy = creatorLogin
        });
        return new OkResult();
    }
    
    [HttpPut("update")]
    public async Task<IActionResult> ChangeCommonData([FromBody] ChangeUserRequest user) {
        var rights = User.FindFirst(ClaimTypes.Role)?.Value;
        var login = User.FindFirst(ClaimTypes.Name)?.Value;

        var var = await IsRightValidated(login, rights, user.Login);
        if (var is not OkResult) return var;
            
        if (user.Name != null && !NameRegex().IsMatch(user.Name)) // Проверка регулярного выражения
            return new BadRequestObjectResult("Entered new Name is not valid");
        
        await UsersRepository.ChangeUserData(user.Login, user.Name, user.Gender, user.Birthday);
        await UsersRepository.ChangeModifiedStatus(user.Login, login);
        return new OkObjectResult("Parameters is successfully changed!");
    }
    
    [HttpPut("updatePassword")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangeUserPasswordRequest user) {
        var rights = User.FindFirst(ClaimTypes.Role)?.Value;
        var login = User.FindFirst(ClaimTypes.Name)?.Value;

        var var = await IsRightValidated(login, rights, user.Login);
        if (var is not OkResult) return var;
        
        if (!LoginPasswordRegex().IsMatch(user.NewPassword)) // Проверка регулярного выражения
            return new BadRequestObjectResult("Entered new Name is not valid");
        
        await UsersRepository.ChangeUserPassword(user.Login, user.NewPassword);
        await UsersRepository.ChangeModifiedStatus(user.Login, login);
        return new OkObjectResult("Password is successfully changed!");
    }
    
    [HttpPut("changeLogin")]
    public async Task<IActionResult> ChangeLogin([FromBody] ChangeUserLogin user) {
        var rights = User.FindFirst(ClaimTypes.Role)?.Value;
        var login = User.FindFirst(ClaimTypes.Name)?.Value;

        var var = await IsRightValidated(login, rights, user.Login);
        if (var is not OkResult) return var;
        
        if (!LoginPasswordRegex().IsMatch(user.NewLogin))
            return new BadRequestObjectResult("Entered new Login is not valid");
        
        if (!await UsersRepository.IsLoginUniqueAsync(user.NewLogin))
            return new BadRequestObjectResult("Entered new Login is not unique");
        
        await UsersRepository.ChangeUserLogin(user.Login, user.NewLogin);
        await UsersRepository.ChangeModifiedStatus(user.Login, login);
        return new OkObjectResult("Login is successfully changed!");
    }
    
    [HttpGet("getActiveUsers")]
    public async Task<IActionResult> GetActiveUsers() {
        var adminCheck = IsUserAdmin(User);
        if (adminCheck is not OkResult)
            return adminCheck;
        
        return new OkObjectResult(JsonSerializer.Serialize(await UsersRepository.GetActiveUsersSortedAsync())); 
    }
    
    [HttpGet("getUserByLogin")]
    public async Task<IActionResult> GetUser([FromQuery] [Required] string login)
    {
        var adminCheck = IsUserAdmin(User);
        if (adminCheck is not OkResult)
            return adminCheck;
        
        if (!await UsersRepository.IsUserExistAsync(login))
           return new BadRequestObjectResult("User is not exist");
        
        var user = await UsersRepository.GetUserAsync(login);
        var jsonUser = JsonSerializer.Deserialize<UserRequest>(
            JsonSerializer.Serialize(await UsersRepository.GetUserAsync(login)));
        jsonUser = jsonUser with { IsActive = user.RevokedOn <= DateTime.MinValue };
        
        return new OkObjectResult(jsonUser);
    }
    
    [HttpGet("getMyDataToken")]
    public async Task<IActionResult> GetMyUserData() {
        var login = User.FindFirst(ClaimTypes.Name)?.Value;
        var user = await UsersRepository.GetUserAsync(login);
        
        if (user.RevokedOn > DateTime.MinValue)
            return new BadRequestObjectResult("Your account is revoked and can`t be received");
        
        return new OkObjectResult(user);
    }
    
    [HttpPost("getMyDataNoToken")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMyUserData([FromBody] UserLoginRequest user) {
        
        if (!await UsersRepository.ValidateCredentialsAsync(user.Login, user.Password))
            return new BadRequestObjectResult("Login or password is invalid");
        
        if (!await UsersRepository.IsUserExistAsync(user.Login))
            return new BadRequestObjectResult("Your account is revoked and can`t be received");
        
        return new OkObjectResult(UsersRepository.GetUserAsync(user.Login));
    }

    [HttpGet("getUser")]
    public async Task<IActionResult> GetUsersByDateBirthWhoOlder([FromQuery] [Required] DateTime dateBirth) {
        var adminCheck = IsUserAdmin(User);
        if (adminCheck is not OkResult)
            return adminCheck;
        
        return new OkObjectResult(await UsersRepository.GetUsersSortedAsync(dateBirth));
    }
    
    [HttpDelete("deleteUser/{login}")]
    public async Task<IActionResult> DeleteUser(string login, [FromQuery] [Required] bool isAbsolute) {
        var loginIsRevoking = User.FindFirst(ClaimTypes.Name)?.Value;
        var adminCheck = IsUserAdmin(User);
        
        if (adminCheck is not OkResult)
            return adminCheck;
        
        if (!await UsersRepository.IsUserExistAsync(login))
            return new BadRequestObjectResult("User is not exist");
        
        await UsersRepository.DeleteUserAsync(login, isAbsolute, loginIsRevoking);
        return new OkObjectResult("User is successfully deleted!");
    }
    
    [HttpPut("recoverUser")]
    public async Task<IActionResult> RecoverUser([FromBody] [Required] string recoveringLogin) {
        var adminCheck = IsUserAdmin(User);
        var loginIsModifying = User.FindFirst(ClaimTypes.Name)?.Value;
        
        if (adminCheck is not OkResult)
            return adminCheck;
        
        if (!await UsersRepository.IsUserExistedAsync(recoveringLogin))
            return new BadRequestObjectResult("User is not exist and not existed");
        
        if (await UsersRepository.IsUserExistAsync(recoveringLogin))
            return new BadRequestObjectResult("User is not revoked for recover");

        await UsersRepository.ChangeUserRevokedStatus(recoveringLogin);
        await UsersRepository.ChangeModifiedStatus(recoveringLogin, loginIsModifying);
        return new OkObjectResult("User is successfully recovered!");
    }
    private static bool ClaimToRightConvert(string name) {
        return name == "Admin";
    }

    private static async Task<IActionResult> IsRightValidated(string loginOfChanging, string? rightsOfChanging, string loginOfChangeable) {
        var userRole = rightsOfChanging != null && ClaimToRightConvert(rightsOfChanging);
        var isChangingUserIsChangeable = loginOfChangeable.Equals(loginOfChanging);
        
        if (!await UsersRepository.IsUserExistedAsync(loginOfChangeable)) // Если изменяемого пользователя не существует
            return new BadRequestObjectResult("User is not exist and not existed");
        
        if (!userRole && !isChangingUserIsChangeable) // Если прав адмимна нет или изменяющий не является самим пользователем
            return new BadRequestObjectResult($"You don`t have enough rights to change this user");
        
        if (isChangingUserIsChangeable && !await UsersRepository.IsUserExistAsync(loginOfChangeable)) // Существует ли пользователь сейчас (не удалён ли)
            return new BadRequestObjectResult($"Your account is revoked and can`t be changed");
        
        return new OkResult();
    }

    private static IActionResult IsUserAdmin(ClaimsPrincipal user) {
        var rights = user.FindFirst(ClaimTypes.Role)?.Value;
        var userRole = rights != null && ClaimToRightConvert(rights);
        if (!userRole)
            return new BadRequestObjectResult("You don`t have enough rights");
        
        return new OkResult();
    }
    [GeneratedRegex(@"^[a-zA-Z0-9]+$")]
    private static partial Regex LoginPasswordRegex();
    
    [GeneratedRegex(@"^[a-zA-Z]+$")]
    private static partial Regex NameRegex();
}