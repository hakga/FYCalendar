using System;
using System.Collections.Generic;

namespace App.Data.Entities;

public partial class Member
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }
}
