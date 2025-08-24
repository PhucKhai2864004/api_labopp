using System;
using System.Collections.Generic;

namespace LabAssistantOPP_LAO.Models.Entities;

public partial class VectorIndex
{
    public int Id { get; set; }

    public string Provider { get; set; } = null!;

    public string IndexName { get; set; } = null!;

    public string? ExternalId { get; set; }

    public DateTime CreatedAt { get; set; }

    // Comment out navigation properties to prevent shadow properties
    // public virtual ICollection<AssignmentIngest> AssignmentIngests { get; set; } = new List<AssignmentIngest>();
}
