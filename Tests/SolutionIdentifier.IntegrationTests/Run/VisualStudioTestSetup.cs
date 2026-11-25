namespace SolutionIdentifier.IntegrationTests.Run
{
    internal sealed class VisualStudioTestSetup
    {
        public VisualStudioTestSetup(int visualStudioVersion, string sampleSolutionToCopy)
        {
            VisualStudioVersion = visualStudioVersion;
            SampleSolutionToCopy = sampleSolutionToCopy;
        }

        /// <summary>The major version: 17 = Visual Studio 2022, 18 = Visual Studio 2026.</summary>
        public int VisualStudioVersion { get; }

        /// <summary>A solution folder in Tests\SampleSolutions, e.g. SampleSolutionCopy.SmallSolutionName.</summary>
        public string SampleSolutionToCopy { get; }
    }
}
