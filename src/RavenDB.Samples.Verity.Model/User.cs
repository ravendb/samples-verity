namespace RavenDB.Samples.Verity.Model;

public class User
{
    public string Id        { get; set; } = null!; // Id Users/{companyName}/{name}_{surname}
    public string CompanyId { get; set; } = null!;
    public string Name      { get; set; } = null!;
    public string Surname   { get; set; } = null!;
    public string Email     { get; set; } = null!; // Email nameSurname@companyDomain
}
