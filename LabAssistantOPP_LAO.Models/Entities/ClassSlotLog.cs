using System;
using System.Collections.Generic;

namespace LabAssistantOPP_LAO.Models.Entities;

public partial class ClassSlotLog
{
    public int Id { get; set; }

    public int ClassSlotId { get; set; }

    public int ActorId { get; set; }

    public string Action { get; set; } = null!;

    public DateTime ActedAt { get; set; }

    public virtual User Actor { get; set; } = null!;

    public virtual ClassSlot ClassSlot { get; set; } = null!;
}
