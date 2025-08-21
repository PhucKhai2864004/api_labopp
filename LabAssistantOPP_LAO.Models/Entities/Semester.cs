using System;
using System.Collections.Generic;

namespace LabAssistantOPP_LAO.Models.Entities;

public partial class Semester
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual ICollection<StudentLabAssignment> StudentLabAssignments { get; set; } = new List<StudentLabAssignment>();
}
