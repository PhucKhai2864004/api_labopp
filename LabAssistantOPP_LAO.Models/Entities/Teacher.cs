using System;
using System.Collections.Generic;

namespace LabAssistantOPP_LAO.Models.Entities;

public partial class Teacher
{
    public int Id { get; set; }

    public string TeacherCode { get; set; } = null!;

    public string? AcademicDegree { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Phone { get; set; }

    public bool? Gender { get; set; }

    public string? Address { get; set; }

    public virtual User IdNavigation { get; set; } = null!;
}
