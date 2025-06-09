using System.ComponentModel.DataAnnotations;

namespace ATON_Test_Task.Repositories;

public class User {
    [Key] public Guid Guid { get; set; } =  Guid.NewGuid();
    public string Login { get; set; }
    public string Password { get; set; }
    public string Name { get; set; }
    public int Gender { get; set; } = 2; // 0 = Woman, 1 = Man, 2 = 
    public DateTime? Birthday { get; set; }
    public bool Admin { get; set; } = false;
    public DateTime CreatedOn { get; private set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "System";
    public DateTime ModifiedOn { get; set; } = DateTime.MinValue;
    public string ModifiedBy { get; set; } = "None";
    public DateTime RevokedOn { get; set; } = DateTime.MinValue;
    public string RevokedBy { get; set; } =  "None";
}