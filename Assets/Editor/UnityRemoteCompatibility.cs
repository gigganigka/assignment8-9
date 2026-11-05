using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

// Editor-only compatibility shim for Input System versions that search for
// GenericRemote in UnityEditor instead of UnityEditor.GenericRemoteModule.
// Uses the installed package's original message parser, never invents touch events.
[InitializeOnLoad]
public static class UnityRemoteCompatibility
{
    static Func<IntPtr, bool> handler;
    static Type remoteType;

    static UnityRemoteCompatibility()
    {
        EditorApplication.delayCall += Install;
        AssemblyReloadEvents.beforeAssemblyReload += Remove;
        EditorApplication.quitting += Remove;
    }
    [MenuItem("Tools/Mobile Game/7. Check Unity Remote Compatibility")]
    public static void Install()
    {
        try
        {
            // Ensure Input System initializes its own editor support before probing it.
            var devices = InputSystem.devices;
            Type support = typeof(InputSystem).Assembly.GetType("UnityEngine.InputSystem.UnityRemoteSupport");
            if (support == null) { Debug.LogWarning("Remote compatibility: package support type not found."); return; }
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            MethodInfo probe = support.GetMethod("GetUnityRemoteAPIMethod", flags);
            if (probe != null && probe.Invoke(null, new object[] { "AddMessageHandler" }) != null)
            {
                Debug.Log("Remote compatibility: native package lookup works; no workaround needed.");
                return;
            }
            if (handler != null) return;
            remoteType = Type.GetType("UnityEditor.Remote.GenericRemote, UnityEditor.GenericRemoteModule")
                ?? typeof(EditorApplication).Assembly.GetType("UnityEditor.Remote.GenericRemote");
            MethodInfo add = remoteType?.GetMethod("AddMessageHandler", flags);
            MethodInfo process = support.GetMethod("ProcessMessageFromUnityRemote", flags);
            if (add == null || process == null)
            {
                Debug.LogWarning("Remote compatibility: required API not found. Send this Console message with your package/editor versions.");
                return;
            }
            handler = (Func<IntPtr, bool>)Delegate.CreateDelegate(typeof(Func<IntPtr, bool>), process);
            add.Invoke(null, new object[] { handler });
            Debug.Log("Unity Remote compatibility installed. Reconnect the iPhone / reopen Unity Remote, then press Play. Check for Touchscreen in Input Debugger.");
        }
        catch (Exception e)
        {
            handler = null;
            Debug.LogWarning("Unity Remote compatibility could not initialize: " + e.GetBaseException().Message);
        }
    }
    static void Remove()
    {
        if (handler == null) return;
        try
        {
            remoteType?.GetMethod("RemoveMessageHandler", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                ?.Invoke(null, new object[] { handler });
        }
        catch (Exception e) { Debug.LogWarning("Remote compatibility cleanup: " + e.GetBaseException().Message); }
        finally { handler = null; }
    }
}
