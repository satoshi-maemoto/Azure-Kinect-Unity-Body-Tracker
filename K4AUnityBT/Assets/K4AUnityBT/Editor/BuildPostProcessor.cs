using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;

public class BuildPostProcessor
{
    private static readonly string[] FILES = new string[]{ "directml.dll", "dnn_model_2_0_op11.onnx", "onnxruntime.dll"};

    [PostProcessBuild]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        CopyFiles(pathToBuiltProject);
    }

    private static void CopyFiles(string pathToBuiltProject)
    {
        foreach (string fileName in FILES)
        {
            string destFilePath = Path.Combine(Path.GetDirectoryName(pathToBuiltProject), fileName);

            if (!File.Exists(destFilePath))
            {
                string projectFilePath = Path.Combine(Application.dataPath.Replace("/Assets", ""), fileName);

                if (File.Exists(projectFilePath))
                {
                    File.Copy(projectFilePath, destFilePath);
                }
            }
        }
    }
}
