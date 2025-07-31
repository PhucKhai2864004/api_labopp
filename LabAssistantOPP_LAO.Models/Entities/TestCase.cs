using System;
using System.Collections.Generic;

namespace LabAssistantOPP_LAO.Models.Entities;

public partial class TestCase
{
    public string Id { get; set; } = null!;

    public string? AssignmentId { get; set; }

    public string? ExpectedOutput { get; set; }

    public string? Input { get; set; }

	public int? Loc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual LabAssignment? Assignment { get; set; }

    public virtual ICollection<TestCaseResult> TestCaseResults { get; set; } = new List<TestCaseResult>();
}
