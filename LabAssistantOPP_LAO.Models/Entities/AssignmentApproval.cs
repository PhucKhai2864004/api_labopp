using System;
using System.Collections.Generic;

namespace LabAssistantOPP_LAO.Models.Entities;

public partial class AssignmentApproval
{
    public int Id { get; set; }

    public int AssignmentId { get; set; }

    public string Action { get; set; } = null!;

    public int ActorId { get; set; }

    public string? ActionNote { get; set; }

    public DateTime ActedAt { get; set; }

    public virtual User Actor { get; set; } = null!;

    public virtual LabAssignment Assignment { get; set; } = null!;
}
