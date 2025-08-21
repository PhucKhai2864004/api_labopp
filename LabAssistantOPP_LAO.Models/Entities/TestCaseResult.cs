using System;
using System.Collections.Generic;

namespace LabAssistantOPP_LAO.Models.Entities;

public partial class TestCaseResult
{
    public int Id { get; set; }

    public int StudentLabAssignmentId { get; set; }

    public int TestCaseId { get; set; }

    public string? ActualOutput { get; set; }

    public bool? IsPassed { get; set; }

    public virtual StudentLabAssignment StudentLabAssignment { get; set; } = null!;

    public virtual TestCase TestCase { get; set; } = null!;
}
