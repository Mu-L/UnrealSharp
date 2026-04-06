using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text;
using CommandLine;

namespace UnrealSharpBuildTool.Actions;

[Verb("PackageProjectParameters", aliases: ["PackageProject"], HelpText = "Packages the project. This will create a self-contained package in a archived directory.")]
public struct PackageProjectParameters
{
    [Option("ArchiveDirectory", Required = true, HelpText = "The directory base directory where the packaged game is located")]
    public string ArchiveDirectory { get; set; }
        
    [Option("TargetPlatform", Required = true, HelpText = "The target platform for the package")]
    public TargetPlatform TargetPlatform { get; set; }

    [Option("TargetArchitecture", Required = false, HelpText = "The target architecture for the package")]
    public TargetArchitecture TargetArchitecture { get; set; }
         
    [Option("UEBuildConfig", Required = false, HelpText = "The build configuration for the package (Debug, Release, etc.)")]
    public TargetConfiguration UEBuildConfig { get; set; }
        
    [Option("NativeAOT", Required = false, HelpText = "Enable Native AOT compilation. Will overwrite the BuildConfig to Release if set to true.")]
    public bool NativeAOT { get; set; }
        
    [Option("UETargetType", Required = false, HelpText = "The type of Unreal Engine target (Editor, Game, etc.). Required for the Publish action.")]
    public string UETargetType { get; set; }
}

public static class PackageProjectAction
{
    public static void PackageProject(PackageProjectParameters parameters)
    {
        if (!Directory.Exists(parameters.ArchiveDirectory))
        {
            throw new DirectoryNotFoundException(parameters.ArchiveDirectory);
        }
        
        Program.CopyGlobalJson();

        string UETargetType = parameters.UETargetType;
        if (string.IsNullOrEmpty(UETargetType))
        {
            throw new Exception("UETargetType argument is required for the Publish action.");
        }
        
        string bindingsPath = Path.Combine(BuildToolOptions.Instance.PluginDirectory, "Managed", "UnrealSharp");
        string buildOutput = Program.GetIntermediateBuildPathForPlatform(
            parameters.TargetArchitecture,
            parameters.TargetPlatform,
            parameters.UEBuildConfig);
        
        Collection<string> extraArguments =
        [
            "--self-contained",
            
            "--runtime",
            "win-x64",
            
			"-p:DisableWithEditor=true",
            "-p:GenerateDocumentationFile=false",
            
            $"-p:UETargetType={UETargetType}",
            $"-p:UEBuildConfig={parameters.UEBuildConfig}",
            
            $"-p:PublishDir=\"{parameters.ArchiveDirectory}\"",
            $"-p:OutputPath=\"{buildOutput}\"",
        ];
        
        if (!parameters.NativeAOT)
        {
            extraArguments.Add("--self-contained");
        }
        
        BuildSolutionParameters buildParameters = new BuildSolutionParameters
        {
            ExtraArguments = extraArguments,
            BuildConfig = parameters.UEBuildConfig,
            Publish = true,
            Folders = [bindingsPath, Program.GetScriptFolder()],
        };
        
        BuildSolutionAction.BuildSolution(buildParameters);
        
        if (parameters.NativeAOT)
        {
            // In progress
        }
    }
}
