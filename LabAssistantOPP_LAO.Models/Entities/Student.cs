using System;
using System.Collections.Generic;

namespace LabAssistantOPP_LAO.Models.Entities;

public partial class Student
{
    public string Id { get; set; } = null!;

    public string StudentCode { get; set; } = null!;

    public string? Major { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Phone { get; set; }

    public string? Gender { get; set; }

    public string? Address { get; set; }

    public virtual User IdNavigation { get; set; } = null!;
}
