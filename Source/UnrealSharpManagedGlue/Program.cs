using System;
using System.Diagnostics;
using EpicGames.UHT.Tables;
using EpicGames.UHT.Utils;
using UnrealSharp.Shared;
using UnrealSharpManagedGlue;
using UnrealSharpManagedGlue.Utilities;

[UnrealHeaderTool]
public static class Program
{
    [UhtExporter(Name = "UnrealSharpCore", Description = "Exports C++ to C# code", Options = UhtExporterOptions.Default, ModuleName = "UnrealSharpCore")]
    private static void Main(IUhtExportFactory factory)
    {
        GeneratorStatics.Initialize(factory);
        USharpBuildToolUtilities.CompileUSharpBuildTool();
        
        ExportBindings();
        PostExport();
    }

    private static void ExportBindings()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        ConsoleUtilities.Log("Starting C# bindings export...");

        try
        {
            GlueGenerator.GenerateBindings();
            
            TaskManager.WaitForTasks();
            
            FileExporter.CleanOldExportedFiles();

            stopwatch.Stop();
            
            string timeString = stopwatch.Elapsed.TotalSeconds < 1 ? $"{stopwatch.Elapsed.TotalMilliseconds:F0}ms" : $"{stopwatch.Elapsed.TotalSeconds:F2}s";
            ConsoleUtilities.Log($"Finished exporting C# bindings in {timeString}");
        }
        catch (Exception ex)
        {
            ConsoleUtilities.Log("Critical failure during export process:");
            ConsoleUtilities.Log(ex.ToString());
        }
    }

    private static void PostExport()
    {
        try
        {
            GlueModuleFactory.CreateGlueProjects();
            BuildUtilities.BuildBindings();
        }
        catch (Exception ex)
        {
            ConsoleUtilities.Log("Critical failure during post-export process:");
            ConsoleUtilities.Log(ex.Message);
        }
    }
}