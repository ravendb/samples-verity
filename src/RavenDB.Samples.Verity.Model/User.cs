namespace RavenDB.Samples.Verity.Model;

public class User
{
    public string   Id           { get; set; } = null!; // users/{subjectId}
    public string   SubjectId    { get; set; } = null!; // IS subject (sub claim)
    public string   Username     { get; set; } = null!; // login name, lowercase
    public string   PasswordHash { get; set; } = string.Empty;
    public string?  CompanyId    { get; set; }           // set for Employee role
    public string   Name         { get; set; } = null!;
    public string   Surname      { get; set; } = string.Empty;
    public string   Email        { get; set; } = null!;
    public UserRole Role         { get; set; } = UserRole.User;
}
