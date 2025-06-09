using ATON_Test_Task.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ATON_Test_Task.Data;

public class DbConnectionFactory: DbContext
{
    public DbSet<User> Users { get; set; }
    public static string DbPath { get; set; } = "Data Source=app.db";
    public DbConnectionFactory(DbContextOptions<DbConnectionFactory> options) : base(options) { }
}