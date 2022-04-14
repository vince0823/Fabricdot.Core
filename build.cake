// Load other scripts.
#load "./build/packages.cake"

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

var version = XmlPeek(File("./Directory.Build.props"), "/Project/PropertyGroup/Version/text()");
var rootDir = Context.Environment.WorkingDirectory.FullPath;
var packageDir = System.IO.Path.Combine(rootDir, "build", "packages");
var reportDir = System.IO.Path.Combine(rootDir, "build" ,"reports");

var solutions = new []
{
    "./Fabricdot.Core.sln",
    "./extensions/Fabricdot.Identity/Fabricdot.Identity.sln",
};

var packages = BuildPackages.GetPackages(
    packageDir,
    version,
    new [] 
    {
        "Fabricdot.Core",
        "Fabricdot.Domain",
        "Fabricdot.Infrastructure",
        "Fabricdot.Infrastructure.EntityFrameworkCore",
        "Fabricdot.MultiTenancy.Abstractions",
        "Fabricdot.MultiTenancy",
        "Fabricdot.MultiTenancy.AspNetCore",
        "Fabricdot.WebApi",

        "Fabricdot.Identity.Domain",
        "Fabricdot.Identity"
    });

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(ctx => 
{ 
    Information("Running tasks...");
    Information($"Version:{version}");
});

Teardown(context =>
{
    Information("Finished running tasks.");
});

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
    {   
        CleanDirectories("**/bin/" + configuration);
        CleanDirectories("**/obj"); 
        CleanDirectories(new []
        {
            packageDir,
            reportDir,
        });
    });

Task("Restore")
    .IsDependentOn("Clean")
    .DoesForEach(solutions, sln=>
    {
        var settings = new DotNetRestoreSettings()
        {
            Verbosity = DotNetVerbosity.Minimal,
        };
        DotNetRestore(sln, settings);
    });

Task("Build")
    .IsDependentOn("Restore")
    .DoesForEach(solutions, sln=>
    {
        var settings = new DotNetBuildSettings()
        {
            Configuration = configuration,
            NoRestore = true,
        };
        DotNetBuild(sln, settings);
    });

Task("Test")
    .IsDependentOn("Build")
    .DoesForEach(()=> GetFiles("**/*.Tests.csproj"), project=>
    {
        foreach(var framework in new [] { "net5.0" })
        {
            var testResultsPath = System.IO.Path.Combine(reportDir,$"{project.GetFilenameWithoutExtension()}_{framework}_TestResults.xml");
            var settings = new DotNetTestSettings
            {
                Framework = framework,
                Configuration = configuration,
                NoBuild = true,
                NoRestore = true,
                // Collectors = new []{ "XPlat Code Coverage" },
                ArgumentCustomization = args=>args.Append($"--logger trx;LogFileName=\"{testResultsPath}\"")
            };
            DotNetTest(project.FullPath, settings);
        }
    });

Task("Package")
    .IsDependentOn("Test")
    .DoesForEach(solutions, sln=>
    {
        var settings = new DotNetPackSettings {
            Configuration = configuration,
            OutputDirectory = packageDir,
            NoBuild = true,
            NoRestore = true,
        };
        DotNetPack(sln, settings);
    });

Task("Publish")
    .IsDependentOn("Package")
    .Does(()=>
    {
        // Resolve the API key.
        var apiKey = EnvironmentVariable("NUGET_API_KEY");
        if(string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("Could not resolve NuGet API key.");

        foreach(var package in packages.NuGet)
        {
            // Push the package.
            var settings = new DotNetNuGetPushSettings {
                ApiKey = apiKey,
                Source = "https://api.nuget.org/v3/index.json",
                SkipDuplicate = true
            };

            DotNetNuGetPush(package.PackagePath.FullPath, settings);
        }
    })
    .OnError((exception, parameters) =>
    {
        Information("Publish Task failed, but continuing with next Task...");
    });

Task("AppVeyor")
    .IsDependentOn("Package")
    .WithCriteria(() => AppVeyor.IsRunningOnAppVeyor)
    .Does(() =>
    {
        foreach(var package in GetFiles(packageDir + "/*"))
        {
            AppVeyor.UploadArtifact(package);
            Information(package);
        }
    });

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Package");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);