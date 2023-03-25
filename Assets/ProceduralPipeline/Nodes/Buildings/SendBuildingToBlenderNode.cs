using System;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

[CreateNodeMenu("Testing/Building Blender")]
public class SendBuildingToBlenderNode : AsyncExtendedNode
{

    [Output] public float o;
    
    protected override void CalculateOutputsAsync(Action<bool> callback)
    {
        try 
        {
            var myProcess = new Process
            {
                StartInfo =
                {
                    FileName = "\"C:\\Users\\cv20549\\OneDrive - University of Bristol\\Documents\\Downloads\\BlenderProcessUnity-main\\BlenderScripts\\RunMeshProcess.bat\"",
                    Arguments = "\"C:\\Users\\cv20549\\OneDrive - University of Bristol\\Documents\\Downloads\\BlenderProcessUnity-main\\UnityProject\\Assets\\SampleAssets\\SampleMesh.fbx\""
                }
            };
            myProcess.Start();
            myProcess.WaitForExit();
            Debug.Log("Blender exit code: " + myProcess.ExitCode);
            myProcess.Close();
            
            /*
             
                // If we want to output the results to stream
                
                var proc = new Process 
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "program.exe",
                        Arguments = "command line arguments to your executable",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                
                // read from the stream
                
                proc.Start();
                while (!proc.StandardOutput.EndOfStream)
                {
                    string line = proc.StandardOutput.ReadLine();
                    // do something with line, start recording data after specific output?
                }
                proc.Close()
                
             */
        }
        catch (Exception exception)
        {
            Debug.LogWarning(exception);
            callback.Invoke(false);
        }

        o = 0;
        callback.Invoke(true);
    }

    protected override void ReleaseData()
    {
    }
}
