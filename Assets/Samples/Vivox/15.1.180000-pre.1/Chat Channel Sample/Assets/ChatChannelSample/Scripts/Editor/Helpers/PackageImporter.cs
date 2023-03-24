using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using Unity.Services.Vivox.Editor;

/// <summary>
/// Used to force add any package dependencies our ChatChannelSample requires.
/// </summary>
static class PackageImporter
{
    const string k_authPackageDependency = "com.unity.services.authentication@1.0.0-pre.4";
        
    /// <summary>
    /// Adds required packages to the project that are not defined/found during any domain reload.
    /// </summary>
    [InitializeOnLoadMethod]
    static void InitializeOnLoadMethod()
    {
#if !AUTH_PACKAGE_PRESENT
    if(!VivoxSettings.Instance.IsEnvironmentCustom)
    {
        ImportAuthenticationPackage();
    }
#endif
    }

    /// <summary>
    /// Locates a specific version of the com.unity.services.authentication package and adds it to the project.
    /// </summary>
    static void ImportAuthenticationPackage()
    {
        Debug.Log($"[Vivox] Because the Chat Channel Sample requires {k_authPackageDependency}, it has been added to your project.");
        Client.Add(k_authPackageDependency);
    }
}