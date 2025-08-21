using System;
using System.Collections.Generic;

namespace LabAssistantOPP_LAO.Models.Entities;

public partial class TestCase
{
    public int Id { get; set; }

    public int AssignmentId { get; set; }

    public string? ExpectedOutput { get; set; }

    public string? Input { get; set; }

    public int? Loc { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual LabAssignment Assignment { get; set; } = null!;

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<TestCaseResult> TestCaseResults { get; set; } = new List<TestCaseResult>();
}
