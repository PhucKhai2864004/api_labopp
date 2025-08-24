using System;
using System.Collections.Generic;

namespace LabAssistantOPP_LAO.Models.Entities;

public partial class LabAssignment
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public int TeacherId { get; set; }

    public int? LocTotal { get; set; }

    public string Status { get; set; } = null!;

    public int? CreatedBy { get; set; }

    public int? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public string? ReviewNote { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual ICollection<AssignmentApproval> AssignmentApprovals { get; set; } = new List<AssignmentApproval>();

    // Comment out navigation properties to prevent shadow properties
    // public virtual ICollection<AssignmentDocument> AssignmentDocuments { get; set; } = new List<AssignmentDocument>();
    // public virtual ICollection<AssignmentIngest> AssignmentIngests { get; set; } = new List<AssignmentIngest>();

    public virtual ICollection<ClassHasLabAssignment> ClassHasLabAssignments { get; set; } = new List<ClassHasLabAssignment>();

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<StudentLabAssignment> StudentLabAssignments { get; set; } = new List<StudentLabAssignment>();

    public virtual User Teacher { get; set; } = null!;

    public virtual ICollection<TestCase> TestCases { get; set; } = new List<TestCase>();
}
