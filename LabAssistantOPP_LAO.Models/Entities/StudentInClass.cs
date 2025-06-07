using System;
using System.Collections.Generic;

namespace LabAssistantOPP_LAO.Models.Entities;

public partial class StudentInClass
{
    public string Id { get; set; } = null!;

    public string? ClassId { get; set; }

    public string? StudentId { get; set; }

    public virtual Class? Class { get; set; }

    public virtual User? Student { get; set; }
}
