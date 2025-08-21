using System;
using System.Collections.Generic;

namespace LabAssistantOPP_LAO.Models.Entities;

public partial class Class
{
    public int Id { get; set; }

    public string ClassCode { get; set; } = null!;

    public string? SubjectCode { get; set; }

    public int SemesterId { get; set; }

    public string? AcademicYear { get; set; }

    public bool IsActive { get; set; }

    public int TeacherId { get; set; }

    public int? LocToPass { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<ClassHasLabAssignment> ClassHasLabAssignments { get; set; } = new List<ClassHasLabAssignment>();

    public virtual ICollection<ClassSlot> ClassSlots { get; set; } = new List<ClassSlot>();

    public virtual Semester Semester { get; set; } = null!;

    public virtual ICollection<StudentInClass> StudentInClasses { get; set; } = new List<StudentInClass>();

    public virtual User Teacher { get; set; } = null!;
}
