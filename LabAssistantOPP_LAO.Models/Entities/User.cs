using System;
using System.Collections.Generic;

namespace LabAssistantOPP_LAO.Models.Entities;

public partial class User
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public int RoleId { get; set; }

    public bool IsActive { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<AssignmentApproval> AssignmentApprovals { get; set; } = new List<AssignmentApproval>();

    public virtual ICollection<AssignmentDocument> AssignmentDocuments { get; set; } = new List<AssignmentDocument>();

    public virtual ICollection<ClassSlotLog> ClassSlotLogs { get; set; } = new List<ClassSlotLog>();

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual ICollection<LabAssignment> LabAssignmentApprovedByNavigations { get; set; } = new List<LabAssignment>();

    public virtual ICollection<LabAssignment> LabAssignmentCreatedByNavigations { get; set; } = new List<LabAssignment>();

    public virtual ICollection<LabAssignment> LabAssignmentTeachers { get; set; } = new List<LabAssignment>();

    public virtual Role Role { get; set; } = null!;

    public virtual Student? Student { get; set; }

    public virtual ICollection<StudentInClass> StudentInClasses { get; set; } = new List<StudentInClass>();

    public virtual ICollection<StudentLabAssignment> StudentLabAssignments { get; set; } = new List<StudentLabAssignment>();

    public virtual Teacher? Teacher { get; set; }

    public virtual ICollection<TestCase> TestCases { get; set; } = new List<TestCase>();
}
