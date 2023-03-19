using System;
using System.Collections;

[NodeTint(0.2f, 0.2f, 0.6f)]
public abstract class AsyncExtendedNode : SyncExtendedNode
{
    private System.Threading.Thread mThread;
    private bool mIsDone;
    public bool isDone {
        get {
            var tmp = mIsDone;
            return tmp;
        }
        set => mIsDone = value;
    }
    private bool success;

    // return success, this is passed to the callback
    public abstract void CalculateOutputsAsync(Action<bool> callback);

    public sealed override IEnumerator CalculateOutputs(Action<bool> callback)
    {
        isDone = false;
        mThread = new System.Threading.Thread(ThreadJob);
        mThread.Priority = System.Threading.ThreadPriority.Highest;
        // this makes the thread not keep an application open. If the main thread stops this thread will be aborted
        mThread.IsBackground = true;
        mThread.Start();
        
        while(!isDone) {
            yield return null;
        }
        
        callback.Invoke(success);
    }

    private void ThreadJob()
    {
        CalculateOutputsAsync(s => {
            success = s;
            isDone = true;
        });
    }
}
