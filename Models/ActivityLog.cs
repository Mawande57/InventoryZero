using System;
using System.Collections.Generic;

namespace InventoryZeroAPI.Models;

public partial class ActivityLog
{
    public int Id { get; set; }

    public int AdminUserId { get; set; }

    public string Action { get; set; } = null!;

    public string EntityType { get; set; } = null!;

    public int EntityId { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User AdminUser { get; set; } = null!;
}
