using System;
using System.Collections.Generic;

namespace LabAssistantOPP_LAO.Models.Entities;

public partial class ClassSlot
{
    public int Id { get; set; }

    public int ClassId { get; set; }

    public int SlotNo { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public bool IsEnabled { get; set; }

    public string? ServerEndpoint { get; set; }

    public string? Note { get; set; }

    public virtual Class Class { get; set; } = null!;

    public virtual ICollection<ClassSlotLog> ClassSlotLogs { get; set; } = new List<ClassSlotLog>();
}
