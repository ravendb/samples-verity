namespace RavenDB.Samples.Verity.Model.Agents;

public class VerityReply

{
    public string?      Answer    { get; set; }
    public List<string> Followups { get; set; } = [];
}