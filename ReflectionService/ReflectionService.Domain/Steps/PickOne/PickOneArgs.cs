using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReflectionService.Domain.Steps.PickOne;

public sealed record PickOneArgs(
    string Strategy = "only", 
    bool FailIfAmbiguous = true
);
