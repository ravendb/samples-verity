namespace RavenDB.Samples.Verity.Model;

public static class SecEdgar
{
    public const string EdgarBase = "https://www.sec.gov";
    public const string DataBase  = "https://data.sec.gov";

    public static string SubmissionsUrl(string paddedCik) =>
        $"{DataBase}/submissions/CIK{paddedCik}.json";

    public static string NormalizeCik(string cik) =>
        cik.Trim().PadLeft(10, '0');
}
