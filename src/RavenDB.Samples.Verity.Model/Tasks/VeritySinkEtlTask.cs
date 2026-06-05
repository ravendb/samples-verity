using Raven.Client.Documents.Operations.ETL;
using RavenDB.Samples.Verity.Model;          // żeby widzieć Company / Report

namespace RavenDB.Samples.Verity.Model.Tasks;

public static class VeritySinkEtlTask
{
    public const string ConnectionStringName = "Verity Sink Connection";
    public const string TaskName = "VeritySinkEtlTask";

    public static RavenEtlConfiguration Create() => new()
    {
        Name = TaskName,
        ConnectionStringName = ConnectionStringName,
        Disabled = false,
        Transforms =
        [
            new Transformation
            {
                Name        = "Companies",
                Collections = [Company.Collection],
                Script      = "loadToCompanies(this);"
            },
            new Transformation
            {
                Name        = "Reports",
                Collections = [Report.Collection],
                Script      = "if (this.Summary) loadToReports(this);"
            }
        ]

    };
}
