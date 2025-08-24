using System;
using System.Collections.Generic;

namespace LabAssistantOPP_LAO.Models.Entities;

public partial class AssignmentIngest
{
    public int Id { get; set; }

    public int AssignmentId { get; set; }

    public int DocumentId { get; set; }

    public int VectorIndexId { get; set; }

    public int? ChunkSize { get; set; }

    public int? ChunkOverlap { get; set; }

    public int? ChunksIngested { get; set; }

    public DateTime? LastChunkedAt { get; set; }

    public string Status { get; set; } = null!;

    public string? Message { get; set; }

    // Comment out navigation properties to prevent shadow properties
    // public virtual LabAssignment Assignment { get; set; } = null!;
    // public virtual AssignmentDocument Document { get; set; } = null!;
    // public virtual VectorIndex VectorIndex { get; set; } = null!;
}
