namespace RavenDB.Samples.Verity.Model;

public class User : IDocument
{
    public static string Collection => "Users";

    public static string BuildId(string companyName, string firstName, string lastName)
        => $"{Collection}/{companyName}/{firstName} {lastName}";

    public static string BuildId(Company company, string firstName, string lastName)
        => BuildId(company.Name, firstName, lastName);

    public string Id        { get; set; } = null!;
    public string CompanyId { get; set; } = null!;
    public string Name      { get; set; } = null!;
    public string Surname   { get; set; } = null!;
    public string Email     { get; set; } = null!;
    public string SubjectId { get; set; } = null!; // IS subject (sub claim)
    public string Username  { get; set; } = null!; // login name, lowercase
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role    { get; set; } = UserRole.User;
}
