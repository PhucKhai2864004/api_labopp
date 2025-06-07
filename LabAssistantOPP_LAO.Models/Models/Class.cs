using System;
using System.Collections.Generic;

namespace LabAssistantOPP_LAO.DTO.Models;

public partial class Class
{
    public string Id { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Subject { get; set; } = null!;

    public bool IsActive { get; set; }

    public string? TeacherId { get; set; }

    public int LocToPass { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<ClassHasLabAssignment> ClassHasLabAssignments { get; set; } = new List<ClassHasLabAssignment>();

    public virtual ICollection<StudentInClass> StudentInClasses { get; set; } = new List<StudentInClass>();

    public virtual User? Teacher { get; set; }
}
