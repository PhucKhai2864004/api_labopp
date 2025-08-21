using System;
using System.Collections.Generic;

namespace LabAssistantOPP_LAO.Models.Entities;

public partial class FapStudent
{
    public int Id { get; set; }

    public string StudentCode { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int SemesterId { get; set; }

    public int ClassId { get; set; }

    public virtual FapClass Class { get; set; } = null!;

    public virtual FapSemester Semester { get; set; } = null!;
}
