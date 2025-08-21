using System;
using System.Collections.Generic;

namespace LabAssistantOPP_LAO.Models.Entities;

public partial class FapSemester
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public virtual ICollection<FapClass> FapClasses { get; set; } = new List<FapClass>();

    public virtual ICollection<FapStudent> FapStudents { get; set; } = new List<FapStudent>();
}
