using System.Collections.Generic;
using System.Reflection;
using Xunit.Sdk;

// Each test starts Visual Studio's Experimental Instance; only one Visual Studio Exp is available so cannot run in parallel.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]

namespace SolutionIdentifier.IntegrationTests.VisualStudio
{
    /// <summary>Runs a [Theory] once for each Visual Studio major version, passed as its int visualStudioVersion parameter.</summary>
    internal sealed class VisualStudioVersionsAttribute : DataAttribute
    {
        // Each test runs once for each major version below, in this order
        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            yield return new object[] { 17 }; // Visual Studio 2022
            yield return new object[] { 18 }; // Visual Studio 2026
        }
    }
}
