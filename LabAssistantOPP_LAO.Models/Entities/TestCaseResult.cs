using System;
using System.Collections.Generic;

namespace LabAssistantOPP_LAO.Models.Entities;

public partial class TestCaseResult
{
    public string Id { get; set; } = null!;

    public string? SubmissionId { get; set; }

    public string? TestCaseId { get; set; }

    public string? ActualOutput { get; set; }

    public bool IsPassed { get; set; }

    public virtual Submission? Submission { get; set; }

    public virtual TestCase? TestCase { get; set; }
}
