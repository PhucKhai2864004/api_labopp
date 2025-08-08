using System;
using System.Collections.Generic;

namespace LabAssistantOPP_LAO.Models.Entities;

public partial class UploadFile
{
    public string Id { get; set; } = null!;

    public string? OriginName { get; set; }

    public string? Name { get; set; }

    public string? Path { get; set; }

    public string? MimeType { get; set; }

    public int? Size { get; set; }

    public string? UploadedBy { get; set; }

    public DateTime? UploadedAt { get; set; }

    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();

    public virtual User? UploadedByNavigation { get; set; }

	public virtual ICollection<LabAssignment> LabAssignments { get; set; } = new List<LabAssignment>();
}
