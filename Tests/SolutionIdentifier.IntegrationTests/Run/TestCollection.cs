using System;
using SolutionIdentifier.IntegrationTests.VisualStudio;
using Xunit;

namespace SolutionIdentifier.IntegrationTests.Run
{
    /// <summary>xUnit creates this on the first test under this collection, and disposes after the last</summary>
    public sealed class ResetExperimentalInstancesOnDispose : IDisposable
    {
        public void Dispose()
        {
            ExperimentalInstanceConfiguration.ResetAllPreparedInstances();
        }
    }

    /// <summary>Every test class that uses Visual Studio's Exp instance is in this collection</summary>
    [CollectionDefinition(Name)]
    public sealed class ExperimentalInstanceTestCollection : ICollectionFixture<ResetExperimentalInstancesOnDispose>
    {
        public const string Name = "Experimental Instance Test Collection";
    }
}
