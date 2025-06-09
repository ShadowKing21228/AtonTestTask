using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

namespace ATON_Test_Task.Models;

/// /// <param name="Gender">
/// 0 - Male, 1 - Female (optional, default: not specified)
/// </param>
public record CreateUserRequest(
    [Required] string Login,
    [Required] string Password,
    [Required] string Name,
    DateTime? Birthday,
    bool Admin = false,
    [Range(0, 1)] int? Gender = 2);
    
public record ChangeUserRequest(
    [Required] string Login,
    string? Name,
    [Range(0, 1)] int? Gender,
    DateTime? Birthday);

public record UserLoginRequest(
    [Required] string Login,
    [Required] string Password);
        
public record ChangeUserPasswordRequest(
    [Required] string Login,
    [Required] string NewPassword);
    
public record ChangeUserLogin(
    [Required] string Login,
    [Required] string NewLogin);
    
public record UserRequest(
    string Name,
    int Gender,
    DateTime? Birthday,
    bool IsActive);