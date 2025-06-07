using System;
using System.Collections.Generic;

namespace LabAssistantOPP_LAO.Models.Entities;

public partial class StudentLabAssignment
{
    public string Id { get; set; } = null!;

    public string? AssignmentId { get; set; }

    public string? StudentId { get; set; }

    public virtual LabAssignment? Assignment { get; set; }

    public virtual User? Student { get; set; }
}
