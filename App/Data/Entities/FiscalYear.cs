using System;
using System.Collections.Generic;

namespace App.Data.Entities;

public partial class FiscalYear
{
    public long Id { get; set; }

    public int FiscalYear1 { get; set; }

    public string Name { get; set; } = null!;

    public DateOnly StartsOn { get; set; }

    public DateOnly EndsOn { get; set; }

    public string Status { get; set; } = null!;
}
