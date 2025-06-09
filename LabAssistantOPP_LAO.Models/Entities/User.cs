using System;
using System.Collections.Generic;

namespace LabAssistantOPP_LAO.Models.Entities;

public partial class User
{
    public string Id { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

	public string RoleId { get; set; } = null!;

    public bool IsActive { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<LabAssignment> LabAssignments { get; set; } = new List<LabAssignment>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<StudentInClass> StudentInClasses { get; set; } = new List<StudentInClass>();

    public virtual ICollection<StudentLabAssignment> StudentLabAssignments { get; set; } = new List<StudentLabAssignment>();

    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
