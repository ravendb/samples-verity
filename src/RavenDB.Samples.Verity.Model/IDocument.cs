namespace RavenDB.Samples.Verity.Model;

/// <summary>
/// A base interface for documents.
/// </summary>
public interface IDocument
{
    /// <summary>
    /// The identifier of the document.
    /// </summary>
    string Id { get;  }

    /// <summary>
    /// The collection name the document belongs to.
    /// </summary>
    static abstract string Collection { get; }
}
