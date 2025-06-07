using System;
using System.Collections.Generic;

namespace LabAssistantOPP_LAO.Models.Entities;

public partial class ClassHasLabAssignment
{
    public string Id { get; set; } = null!;

    public string? ClassId { get; set; }

    public string? AssignmentId { get; set; }

    public virtual LabAssignment? Assignment { get; set; }

    public virtual Class? Class { get; set; }
}
