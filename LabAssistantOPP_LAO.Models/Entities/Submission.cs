using System;
using System.Collections.Generic;

namespace LabAssistantOPP_LAO.Models.Entities;

public partial class Submission
{
    public string Id { get; set; } = null!;

    public string? StudentId { get; set; }

    public string? AssignmentId { get; set; }

    public string? ZipCode { get; set; }

    public string? Status { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public int? LocResult { get; set; }

    public bool? ManuallyEdited { get; set; }

    public string? ManualReason { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual LabAssignment? Assignment { get; set; }

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual User? Student { get; set; }

    public virtual ICollection<TestCaseResult> TestCaseResults { get; set; } = new List<TestCaseResult>();

    public virtual UploadFile? ZipCodeNavigation { get; set; }
}
