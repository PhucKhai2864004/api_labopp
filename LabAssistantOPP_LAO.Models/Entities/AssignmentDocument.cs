using System;
using System.Collections.Generic;

namespace LabAssistantOPP_LAO.Models.Entities;

public partial class AssignmentDocument
{
    public int Id { get; set; }

    public int AssignmentId { get; set; }

    public string FileName { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public string? MimeType { get; set; }

    public int? UploadedBy { get; set; }

    public DateTime UploadedAt { get; set; }

    public virtual LabAssignment Assignment { get; set; } = null!;

    public virtual ICollection<AssignmentIngest> AssignmentIngests { get; set; } = new List<AssignmentIngest>();

    public virtual User? UploadedByNavigation { get; set; }
}
