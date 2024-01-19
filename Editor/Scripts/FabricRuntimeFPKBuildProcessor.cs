using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Fabric.Player;

public class FabricRuntimeFPKPreBuildProcessor : IPreprocessBuildWithReport
{
    public int callbackOrder { get { return 0; } }
    public void OnPreprocessBuild(BuildReport report)
    {
        FPKExporter.LoadCustomExporterSettings();

        if (FPKExporter.customExporterSettings.exportOnBuild)
        {
            FPKExporter.Export();
        }

        string finalOutputPath = Path.Combine(Application.streamingAssetsPath, "Output.xml");
        finalOutputPath = finalOutputPath.Replace("\\", "/");
        Debug.Log("finalOutputPath: " + finalOutputPath);

        Fabric.Player.API.Save(Fabric.Player.FabricPlayer.Instance.fabricPlayerInstanceId, finalOutputPath);
    }
}

public class FabricRuntimeFPKPostBuildProcessor : IPostprocessBuildWithReport
{
    public int callbackOrder { get { return 0; } }
    public void OnPostprocessBuild(BuildReport report)
    {
        string finalOutputPath = Path.Combine(Application.streamingAssetsPath, "Output.xml");
        finalOutputPath = finalOutputPath.Replace("\\", "/");
        Debug.Log("finalOutputPath: " + finalOutputPath);

        File.Delete(finalOutputPath);
    }
}
