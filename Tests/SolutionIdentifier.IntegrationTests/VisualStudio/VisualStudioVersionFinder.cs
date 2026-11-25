using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SolutionIdentifier.IntegrationTests.VisualStudio
{
    /// <summary>Finds the installed Visual Studio versions with vswhere.exe (installed with the Visual Studio Installer).</summary>
    internal static class VisualStudioVersionFinder
    {
        private static readonly Lazy<IReadOnlyList<VisualStudioVersionInfo>> AllVersionsInstalled = new Lazy<IReadOnlyList<VisualStudioVersionInfo>>(FindAllInstallations);

        /// <summary>The newest installation with this major version (17 = Visual Studio 2022, 18 = Visual Studio 2026).</summary>
        public static VisualStudioVersionInfo FindInstallationForMajorVersion(int majorVersion)
        {
            VisualStudioVersionInfo versionInfo = AllVersionsInstalled.Value
                .Where(i => i.Version.Major == majorVersion)
                .OrderByDescending(i => i.Version)
                .FirstOrDefault();

            return versionInfo ?? throw new InvalidOperationException(
                $"Visual Studio {majorVersion}.x isn't installed. Found: {string.Join(", ", AllVersionsInstalled.Value.Select(i => i.Version))}.");
        }

        private static IReadOnlyList<VisualStudioVersionInfo> FindAllInstallations()
        {
            string vswherePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                @"Microsoft Visual Studio\Installer\vswhere.exe");

            if (File.Exists(vswherePath) == false)
            {
                throw new InvalidOperationException($"vswhere.exe not found at {vswherePath}.");
            }

            ProcessResult result = ProcessRunner.RunProcessAndCaptureOutput(vswherePath, arguments: "-format xml", TimeSpan.FromMinutes(1));

            /*
            Output in the form (one <instance> per installation):

            <instances>
              <instance>
                <instanceId>01234567</instanceId>
                ...
                <installationPath>C:\Program Files\Microsoft Visual Studio\18\Community</installationPath>
                <installationVersion>18.00.12345.678</installationVersion>
                ...
              </instance>
              ...
            </instances>
            */

            return XDocument.Parse(result.Output).Descendants("instance")
                .Select(instance => new VisualStudioVersionInfo(
                    installationId: (string)instance.Element("instanceId"),
                    installationPath: (string)instance.Element("installationPath"),
                    version: Version.Parse((string)instance.Element("installationVersion"))))
                .ToList();
        }
    }
}
