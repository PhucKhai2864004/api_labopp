using System;
using System.Collections.Generic;

namespace LabAssistantOPP_LAO.Models.Entities;

public partial class StudentLabAssignment
{
    public int Id { get; set; }

    public int AssignmentId { get; set; }

    public int StudentId { get; set; }

    public int SemesterId { get; set; }

    public string? SubmissionZip { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? SubmittedAt { get; set; }

    public int? LocResult { get; set; }

    public bool? ManuallyEdited { get; set; }

    public string? ManualReason { get; set; }

    public virtual LabAssignment Assignment { get; set; } = null!;

    public virtual Semester Semester { get; set; } = null!;

    public virtual User Student { get; set; } = null!;

    public virtual ICollection<TestCaseResult> TestCaseResults { get; set; } = new List<TestCaseResult>();
}
