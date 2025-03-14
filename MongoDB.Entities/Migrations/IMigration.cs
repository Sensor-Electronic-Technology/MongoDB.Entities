using System.Collections.Generic;
using System.Threading.Tasks;

namespace MongoDB.Entities;

/// <summary>
/// The contract for writing user data migration classes
/// </summary>
public interface IMigration
{
    Task UpgradeAsync();
}

public interface IDocumentMigration {
    public DocumentVersion Version { get; set; }
    public List<FieldOperation> UpOperations { get; set; }
    public List<FieldOperation> DownOperations { get; set; }
    void Build(MigrationBuilder builder);
}