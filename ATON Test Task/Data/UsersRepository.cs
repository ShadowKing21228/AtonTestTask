using ATON_Test_Task.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ATON_Test_Task.Data;

public static class UsersRepository
{
    private static readonly DbContextOptions<DbConnectionFactory> Options = new DbContextOptionsBuilder<DbConnectionFactory>()
        .UseSqlite(DbConnectionFactory.DbPath)
        .Options;
    public static Task<bool> ValidateCredentialsAsync(string login, string password) {
        using var db = new DbConnectionFactory(Options);
        return db.Users.AnyAsync(user => user.Login == login && user.Password == password);
    }
    public static async Task AddUserAsync(User user) {
        await using var db =  new DbConnectionFactory(Options);
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }
    public static async Task ChangeUserData(string login, string? name, int? gender, DateTime? birthday) {
        await using var db = new DbConnectionFactory(Options);
        var userByLogin = await db.Users.FirstOrDefaultAsync(user1 => user1.Login == login);
        userByLogin.Name = name ?? userByLogin.Name;
        userByLogin.Gender = gender ?? userByLogin.Gender;
        userByLogin.Birthday = birthday ?? userByLogin.Birthday;
        await db.SaveChangesAsync();
    }

    public static async Task ChangeUserPassword(string login, string newPassword) {
        await using var db = new DbConnectionFactory(Options);
        var userByLogin = await db.Users.FirstOrDefaultAsync(user1 => user1.Login == login);
        userByLogin.Password = newPassword;
        await db.SaveChangesAsync();
    }

    public static async Task ChangeUserLogin(string login, string newLogin) {
        await using var db = new DbConnectionFactory(Options);
        var userByLogin = await db.Users.FirstOrDefaultAsync(user1 => user1.Login == login);
        userByLogin.Login = newLogin;
        await db.SaveChangesAsync();
    }

    public static async Task ChangeUserRevokedStatus(string login) {
        await using var db = new DbConnectionFactory(Options);
        var userByLogin = await db.Users.FirstOrDefaultAsync(user1 => user1.Login == login);
        userByLogin.RevokedOn = DateTime.MinValue;
        userByLogin.RevokedBy = "None";
        await db.SaveChangesAsync();
    }

    public static async Task ChangeModifiedStatus(string login, string loginModifiedBy) {
        await using var db = new DbConnectionFactory(Options);
        var userByLogin = await db.Users.FirstOrDefaultAsync(user1 => user1.Login == login);
        userByLogin.ModifiedOn = DateTime.Now;
        userByLogin.ModifiedBy = loginModifiedBy;
        await db.SaveChangesAsync();
    }
    public static async Task<bool> GetUserRightAsync(string login) {
        await using var db = new DbConnectionFactory(Options);
        return db.Users.FirstOrDefaultAsync(user1 => user1.Login == login).Result.Admin;
    }

    public static async Task<List<User>> GetActiveUsersSortedAsync() {
        await using var db = new DbConnectionFactory(Options);
        var resultList = db.Users.ToList();
        resultList.RemoveAll(a => a.RevokedOn > DateTime.MinValue);
        resultList.Sort((a, b) => a.CreatedOn.CompareTo(b.CreatedOn));
        return resultList;
    }
    public static async Task<List<User>> GetUsersSortedAsync(DateTime birthDate) {
        await using var db = new DbConnectionFactory(Options);
        var resultList = db.Users.ToList();
        resultList.RemoveAll(a => a.Birthday < birthDate);
        resultList.Sort((a, b) => a.Birthday > b.Birthday ? -1 : 1);
        return resultList;
    }

    public static async Task<User> GetUserAsync(string login)
    {
        await using var db = new DbConnectionFactory(Options);
        var userByLogin = await db.Users.FirstOrDefaultAsync(user1 => user1.Login == login);
        return userByLogin;
    }
    public static async Task<bool> IsLoginUniqueAsync(string name) {
        await using var db = new DbConnectionFactory(Options);
        return !await db.Users.AnyAsync(user1 => user1.Login == name);
    }

    public static async Task<bool> IsUserExistedAsync(string login) {
        await using var db = new DbConnectionFactory(Options);
        return await db.Users.AnyAsync(user1 => user1.Login == login);
    }

    public static async Task<bool> IsUserExistAsync(string login) {
        await using var db = new DbConnectionFactory(Options);
        return await db.Users.AnyAsync(user1 => user1.Login == login && user1.RevokedOn <= DateTime.MinValue);
    }

    public static async Task DeleteUserAsync(string login, bool isAbsolute, string RevokedBy) {
        await using var db = new DbConnectionFactory(Options);
        if (isAbsolute) {
            db.Users.Remove(await db.Users.FirstOrDefaultAsync(user1 => user1.Login == login));
        }
        else {
            var user = await db.Users.FirstOrDefaultAsync(user1 => user1.Login == login);
            user.RevokedBy = RevokedBy;
            user.RevokedOn = DateTime.Now;
        }
        await db.SaveChangesAsync();
    }
}