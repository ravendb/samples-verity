namespace RavenDB.Samples.Verity.App.Models.VerityAgent;

public class VerityReply
{
    public string? Answer { get; set; }
    public List<string> Followups { get; set; } = [];
}

public class VeritySaveInvoiceArgs
{
    public string InvoiceNumber { get; set; } = "";
    public string VendorName { get; set; } = "";
    public string InvoiceDate { get; set; } = "";
    public string DueDate { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public decimal VatAmount { get; set; }
    public string Currency { get; set; } = "PLN";
    public string Description { get; set; } = "";
    public List<VerityInvoiceLineItem> LineItems { get; set; } = [];
}

public class VerityInvoiceLineItem
{
    public string Description { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class VerityFlagInvoiceArgs
{
    public string InvoiceId { get; set; } = "";
    public string Reason { get; set; } = "";
    public string Note { get; set; } = "";
}