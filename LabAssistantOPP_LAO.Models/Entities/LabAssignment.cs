using System;
using System.Collections.Generic;

namespace LabAssistantOPP_LAO.Models.Entities;

public partial class LabAssignment
{
    public string Id { get; set; } = null!;

    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? TeacherId { get; set; }

    public int? LocTotal { get; set; }

    public string? Status { get; set; }

	public string? FileId { get; set; }

	public string? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }


	public virtual ICollection<ClassHasLabAssignment> ClassHasLabAssignments { get; set; } = new List<ClassHasLabAssignment>();

    public virtual ICollection<StudentLabAssignment> StudentLabAssignments { get; set; } = new List<StudentLabAssignment>();

    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();

    public virtual User? Teacher { get; set; }

    public virtual ICollection<TestCase> TestCases { get; set; } = new List<TestCase>();

	public virtual UploadFile? File { get; set; }
}
