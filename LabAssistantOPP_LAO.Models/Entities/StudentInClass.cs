using System;
using System.Collections.Generic;

namespace LabAssistantOPP_LAO.Models.Entities;

public partial class StudentInClass
{
    public int Id { get; set; }

    public int ClassId { get; set; }

    public int StudentId { get; set; }

    public virtual Class Class { get; set; } = null!;

    public virtual User Student { get; set; } = null!;
}
