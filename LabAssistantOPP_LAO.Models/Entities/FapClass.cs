using System;
using System.Collections.Generic;

namespace LabAssistantOPP_LAO.Models.Entities;

public partial class FapClass
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int SemesterId { get; set; }

    public virtual ICollection<FapStudent> FapStudents { get; set; } = new List<FapStudent>();

    public virtual FapSemester Semester { get; set; } = null!;
}
