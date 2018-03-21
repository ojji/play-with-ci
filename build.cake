#tool "nuget:?package=JetBrains.dotCover.CommandLineTools"
#addin "nuget:?package=Cake.Sonar"
#tool "nuget:?package=MSBuild.SonarQube.Runner.Tool"

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var framework = "netcoreapp2.0";
var outputDirectory = new DirectoryPath("./dist");
var testResultsDirectory = new DirectoryPath("./dist/test-results");
var coverage = new FilePath("./dist/coverage/coverage.html");
var branchName = EnvironmentVariable("TRAVIS_BRANCH") ?? EnvironmentVariable("APPVEYOR_REPO_BRANCH");
var sonarLogin = EnvironmentVariable("SONAR_TOKEN");

Task("Clean")
    .Does(() => {
        var cleanSettings = new DotNetCoreCleanSettings
        {
            Framework = framework,
            Configuration = configuration
        };

        DotNetCoreClean("./AppVeyor.sln", cleanSettings);

        if (DirectoryExists(outputDirectory))
        {
            DeleteDirectory(outputDirectory, new DeleteDirectorySettings {
                Recursive = true,
                Force = true
            });
        }
    });

Task("Restore")
    .Does(() => {
        DotNetCoreRestore();
    });

Task("Build")
    .Does(() => {
        var buildSettings = new DotNetCoreBuildSettings
        {
            Framework = framework,
            Configuration = configuration
        };
        DotNetCoreBuild("./AppVeyor.sln", buildSettings);
    });

Task("Test-linux")
    .WithCriteria(IsRunningOnUnix)
    .Does(() => {
        DotNetCoreTest("./TestLibrary.Test", new DotNetCoreTestSettings {
                        Configuration = configuration,
                        NoBuild = true,
                        NoRestore = true,
                        ResultsDirectory = testResultsDirectory,
                        Logger = "trx"
        });
    });

Task("TestAndCover-windows")
    .WithCriteria(IsRunningOnWindows)
    .Does(() => {
        DotCoverAnalyse(tool => {
            tool.DotNetCoreTest("./TestLibrary.Test", new DotNetCoreTestSettings {
                    Configuration = configuration,
                    NoBuild = true,
                    NoRestore = true,
                    ResultsDirectory = testResultsDirectory,
                    Logger = "trx"
                });
            },
            coverage,
            new DotCoverAnalyseSettings {
                ReportType = DotCoverReportType.HTML
            }
            .WithFilter("+:TestLibrary")
            .WithFilter("-:TestLibrary.Test"));
    });

Task("SonarBegin-windows")
    .WithCriteria(IsRunningOnWindows)
    .Does(() => {
        SonarBegin(new SonarBeginSettings {
            Login = sonarLogin,
            Key = "testlibrary",
            Branch = branchName,
            Url = "https://sonarcloud.io",
            Organization = "ojji-github",
            DotCoverReportsPath = coverage.FullPath,
            VsTestReportsPath = testResultsDirectory.FullPath + "/*.trx"
        });
    });

Task("SonarEnd-windows")
    .WithCriteria(IsRunningOnWindows)
    .Does(() => {
        SonarEnd(new SonarEndSettings {
            Login = sonarLogin
         });
    });

Task("Default")
    .IsDependentOn("Clean")
    .IsDependentOn("SonarBegin-windows")
    .IsDependentOn("Build")
    .IsDependentOn("TestAndCover-windows")
    .IsDependentOn("Test-linux")
    .IsDependentOn("SonarEnd-windows");

RunTarget(target);