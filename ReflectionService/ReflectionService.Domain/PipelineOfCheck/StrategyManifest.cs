using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReflectionService.Domain.PipelineOfCheck
{
    internal static class StrategyManifest
    {
        internal static string Manifest { get; } =
            @"{
          ""id"": ""check-interfaces-consumers"",
          ""pattern"": ""default"",
          ""version"": ""1.0"",
          ""target"": {
            ""assemblyName"": ""UserAssemblyName"",
            ""entrypointTypeRegex"": "".*"",
            ""excludeCompilerGenerated"": true
          },
          ""steps"": [
            {
              ""id"": ""step-findtypes"",
              ""operation"": ""FindTypes"",
              ""output"": ""AllClasses"",
              ""args"": {
                ""kind"": ""Any"",
                ""visibility"": ""Any""
              }
            },
            {
              ""id"": ""step-findinterfaces"",
              ""operation"": ""FindInterfaces"",
              ""output"": ""Interfaces"",
              ""args"": {
                ""visibility"": ""Any"",
                ""nameRegex"": "".*""
              }
            },
            {
              ""id"": ""step-countimpls"",
              ""operation"": ""CountTypes"",
              ""input"": ""Interfaces"",
              ""output"": ""Implementations"",
              ""args"": {
                ""kind"": ""Class"",
                ""visibility"": ""Any"",
                ""min"": 2
              }
            },
            {
              ""id"": ""step-findconsumers"",
              ""operation"": ""FindCtorConsumers"",
              ""input"": ""Interfaces"",
              ""output"": ""Consumers"",
              ""args"": {
                ""visibility"": ""Any""
              }
            }
          ]
        }";
    }
}
