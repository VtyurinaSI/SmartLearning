using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReflectionService.Domain.Steps.AssertMemberSignature;

public sealed record AssertMemberSignatureArgs(
    string? NameRegex = null,
    TypeVisibility Visibility = TypeVisibility.Any,
    bool? Static = null,
    string? ReturnTypeRegex = null,
    string[]? ParamTypeRegexes = null,
    int? MinMatches = null 
);
