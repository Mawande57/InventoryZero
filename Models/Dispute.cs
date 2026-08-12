using System;
using System.Collections.Generic;

namespace InventoryZeroAPI.Models;

public partial class Dispute
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int RaisedByUserId { get; set; }

    public string Reason { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string? EvidenceUrls { get; set; }

    public string Status { get; set; } = null!;

    public string? ResolutionNotes { get; set; }

    public int? ResolvedByUserId { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual User RaisedByUser { get; set; } = null!;

    public virtual User? ResolvedByUser { get; set; }
}
