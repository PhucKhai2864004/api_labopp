using System;
using System.Collections.Generic;

namespace LabAssistantOPP_LAO.Models.Entities;

public partial class Feedback
{
    public string Id { get; set; } = null!;

    public string? SubmissionId { get; set; }

    public string? TeacherId { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Submission? Submission { get; set; }

    public virtual User? Teacher { get; set; }
}
