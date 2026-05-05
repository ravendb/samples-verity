using Raven.Client.Documents.Operations.ETL;
using Raven.Client.Documents.Operations.ETL.Queue;
using RavenDB.Samples.Verity.Model;

namespace RavenDB.Samples.Verity.Model.Tasks;

public static class AuditRevisionQueueEtlTask
{
    public const string ConnectionStringName = "Verit-Azure-Queue-Storage";
    public const string TaskName             = "AuditRevisionQueue";
    public const string QueueName            = "auditRevisions";

    public static QueueEtlConfiguration Create() => new()
    {
        Name = "AuditRevisionQueue",
        ConnectionStringName = ConnectionStringName,
        BrokerType = QueueBrokerType.AzureQueueStorage,
        Disabled = false,
        Queues = [new EtlQueue { Name = QueueName }],
        Transforms =
        [
            new Transformation
            {
                Name = "PublishAuditRevisions",
                Collections = [Audit.Collection],
                Script = @"
var report  = load(this.ReportId);
var company = load(report.CompanyId);

loadToAuditRevisions({
    AuditId:       id(this),
    ReportYear:    report.Year,
    ReportQuarter: report.Quarter,
    CompanyName:   company.Name
});"
            }
        ]
    };
}
