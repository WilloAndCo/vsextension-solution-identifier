using SolutionIdentifier.Appearance;
using Xunit;

namespace SolutionIdentifier.UnitTests
{
    [Trait("Category", "Unit")]
    public sealed class SolutionPathDisplayTextTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ShortenPathToLastFolders_NoFoldersWanted_ReturnsFileName(int foldersToShow)
        {
            Assert.Equal("Repo.sln", SolutionPathDisplayText.ShortenPathToLastFolders(@"C:\Dev\Repo\Repo.sln", foldersToShow));
        }

        [Theory]
        [InlineData(1, @"...\Repo\Repo.sln")]
        [InlineData(2, @"...\Dev\Repo\Repo.sln")]
        public void ShortenPathToLastFolders_PathDeeperThanFoldersWanted_ReturnsLastFoldersAfterEllipsis(int foldersToShow, string expected)
        {
            Assert.Equal(expected, SolutionPathDisplayText.ShortenPathToLastFolders(@"C:\Work\Dev\Repo\Repo.sln", foldersToShow));
        }

        [Fact]
        public void ShortenPathToLastFolders_RoomForFoldersButNotDrive_ReplacesDriveWithEllipsis()
        {
            Assert.Equal(@"...\Dev\Repo\Repo.sln", SolutionPathDisplayText.ShortenPathToLastFolders(@"C:\Dev\Repo\Repo.sln", foldersToShow: 2));
        }

        [Theory]
        [InlineData(@"C:\Repo\Repo.sln")]
        [InlineData(@"C:\Repo.sln")]
        public void ShortenPathToLastFolders_PathNoDeeperThanFoldersWanted_ReturnsFullPath(string solutionPath)
        {
            Assert.Equal(solutionPath, SolutionPathDisplayText.ShortenPathToLastFolders(solutionPath, foldersToShow: 2));
        }

        [Fact]
        public void ShortenPathToLastFolders_NetworkPath_CountsServerAndShareAsOneFolder()
        {
            const string NetworkPath = @"\\server\share\Team\Repo\Repo.sln";

            Assert.Equal(@"...\Repo\Repo.sln", SolutionPathDisplayText.ShortenPathToLastFolders(NetworkPath, foldersToShow: 1));
            Assert.Equal(@"...\Team\Repo\Repo.sln", SolutionPathDisplayText.ShortenPathToLastFolders(NetworkPath, foldersToShow: 2));
            Assert.Equal(NetworkPath, SolutionPathDisplayText.ShortenPathToLastFolders(NetworkPath, foldersToShow: 3));
        }

        [Fact]
        public void ShortenPathToLastFolders_SlnxFile_KeepsExtension()
        {
            Assert.Equal(@"...\Repo\Repo.slnx", SolutionPathDisplayText.ShortenPathToLastFolders(@"C:\Dev\Repo\Repo.slnx", foldersToShow: 1));
        }
    }
}
