using System;
using System.Collections.Generic;

namespace LabAssistantOPP_LAO.Models.Entities;

public partial class ClassHasLabAssignment
{
    public int Id { get; set; }

    public int ClassId { get; set; }

    public int AssignmentId { get; set; }

    public DateTime? OpenAt { get; set; }

    public DateTime? CloseAt { get; set; }

    public virtual LabAssignment Assignment { get; set; } = null!;

    public virtual Class Class { get; set; } = null!;
}
