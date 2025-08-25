using System;
using System.Collections.Generic;

namespace LabAssistantOPP_LAO.Models.Entities;

public partial class FapClass
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int SemesterId { get; set; }

	public string? SubjectCode { get; set; }

	public string? AcademicYear { get; set; }

	public string? TeacherCode { get; set; }

	public int TeacherId { get; set; }

	public virtual ICollection<FapClassSlot> FapClassSlots { get; set; } = new List<FapClassSlot>();

	public virtual ICollection<FapStudent> FapStudents { get; set; } = new List<FapStudent>();

    public virtual FapSemester Semester { get; set; } = null!;

	public virtual Teacher Teacher { get; set; } = null!;
}
