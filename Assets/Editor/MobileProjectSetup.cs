using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// These tools belong in Editor: no UnityEditor references enter the Android player.
public static class MobileProjectSetup
{
    static readonly string[] ScenePaths = {
        "Assets/mainmenu.unity", "Assets/level1.unity", "Assets/level2.unity",
        "Assets/bosslevel.unity", "Assets/win.unity"
    };

    [MenuItem("Tools/Mobile Game/1. Prepare Project and Bake UI")]
    public static void PrepareAndBake()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Configure();
        foreach (string path in ScenePaths)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            MobileGameUI.Install(scene);
            PlayerController hero = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            if (hero != null)
            {
                SpriteRenderer renderer = hero.GetComponent<SpriteRenderer>();
                Sprite skin = AssetDatabase.LoadAllAssetsAtPath("Assets/Resources/Mobile/ExplorerIdle.png").OfType<Sprite>().FirstOrDefault();
                if (renderer != null && skin != null) { renderer.sprite = skin; renderer.color = Color.white; }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(ScenePaths[0]);
        Debug.Log("COINBOUND: project configured and all five interfaces baked. Open mainmenu and press Play.");
    }
    public static void Configure()
    {
        EditorSettings.serializationMode = SerializationMode.ForceText;
        UnityEditor.VersionControlSettings.mode = "Visible Meta Files";
        PlayerSettings.companyName = "StudentProject";
        PlayerSettings.productName = MobileGameUI.GameTitle;
        PlayerSettings.bundleVersion = "0.1.0";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        // 1 = Input System only. Uses serialization because no public API exists in this Unity release.
        SerializedObject settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        SerializedProperty input = settings.FindProperty("activeInputHandler");
        if (input != null) { input.intValue = 1; settings.ApplyModifiedPropertiesWithoutUndo(); }
        EditorBuildSettings.scenes = ScenePaths.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
        RenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/UniversalRP.asset");
        if (pipeline != null)
        {
            GraphicsSettings.defaultRenderPipeline = pipeline;
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.vSyncCount = 0;
        }
        EnsureGroundLayer();
        UnityEngine.Object graphicsAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0];
        SerializedObject graphics = new SerializedObject(graphicsAsset);
        SerializedProperty shaders = graphics.FindProperty("m_AlwaysIncludedShaders");
        Shader skinShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shaders != null && skinShader != null)
        {
            bool found = false;
            for (int i = 0; i < shaders.arraySize; i++)
                if (shaders.GetArrayElementAtIndex(i).objectReferenceValue == skinShader) found = true;
            if (!found) { int i = shaders.arraySize++; shaders.GetArrayElementAtIndex(i).objectReferenceValue = skinShader; }
            graphics.ApplyModifiedPropertiesWithoutUndo();
        }
        AssetDatabase.SaveAssets();
    }
    static void EnsureGroundLayer()
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        SerializedObject tags = new SerializedObject(assets[0]);
        SerializedProperty layers = tags.FindProperty("layers");
        if (layers != null && layers.arraySize > 6)
        {
            layers.GetArrayElementAtIndex(6).stringValue = "Ground";
            tags.ApplyModifiedPropertiesWithoutUndo();
        }
    }
    [MenuItem("Tools/Mobile Game/2. Build Android APK")]
    public static void BuildAndroid()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            throw new InvalidOperationException("Install Android Build Support, SDK/NDK Tools and OpenJDK for this Unity version in Unity Hub.");
        Configure();
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.student.coinbound");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.Android.bundleVersionCode = 1;
        PlayerSettings.Android.renderOutsideSafeArea = true;
        PlayerSettings.Android.optimizedFramePacing = true;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
        EditorUserBuildSettings.buildAppBundle = false;
        if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            throw new InvalidOperationException("Could not switch to Android. Switch in Build Profiles, wait for import, then run Build Android APK again.");
        Directory.CreateDirectory("Builds/Android");
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = ScenePaths,
            locationPathName = "Builds/Android/Coinbound.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Android build failed. See the first error in the Unity Console.");
        Debug.Log("APK created: " + Path.GetFullPath("Builds/Android/Coinbound.apk"));
    }
    [MenuItem("Tools/Mobile Game/6. Build Windows EXE")]
    public static void BuildWindows()
    {
        RequireTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64, "Windows Build Support");
        Configure();
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        BuildFor(BuildTarget.StandaloneWindows64, "Builds/Windows/Coinbound.exe");
    }
    [MenuItem("Tools/Mobile Game/4. Export iOS Xcode Project")]
    public static void ExportIOS()
    {
        RequireTarget(BuildTargetGroup.iOS, BuildTarget.iOS, "iOS Build Support");
        Configure();
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.student.coinbound");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
        PlayerSettings.iOS.targetOSVersionString = "15.0";
        PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
        // Do not invent a signing team or overwrite credentials. Set them in Xcode/cloud.
        PlayerSettings.iOS.appleEnableAutomaticSigning = true;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.iOS, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS, new[] { GraphicsDeviceType.Metal });
        BuildFor(BuildTarget.iOS, "Builds/iOS");
        Debug.Log("Xcode project exported. This is NOT a signed IPA. Xcode/signing are still required.");
    }
    [MenuItem("Tools/Mobile Game/5. Build Web (Safari)")]
    public static void BuildWeb()
    {
        RequireTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL, "Web Build Support");
        Configure();
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
        PlayerSettings.WebGL.template = "PROJECT:Coinbound";
        // Uncompressed output simplifies static hosting: no gzip/Brotli headers needed.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = false;
        BuildFor(BuildTarget.WebGL, "Builds/Web");
        Debug.Log("Web build created. Serve Builds/Web over HTTP(S); do not open index.html as a local file.");
    }
    static void RequireTarget(BuildTargetGroup group, BuildTarget target, string module)
    {
        if (!BuildPipeline.IsBuildTargetSupported(group, target))
            throw new InvalidOperationException("Install " + module + " for this editor in Unity Hub.");
        if (EditorUserBuildSettings.activeBuildTarget != target)
            throw new InvalidOperationException("Select " + target + " in File > Build Profiles, Switch Platform, wait for import, then run this command again.");
    }
    static void BuildFor(BuildTarget target, string output)
    {
        Directory.CreateDirectory(Path.HasExtension(output) ? Path.GetDirectoryName(output) : output);
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = ScenePaths, locationPathName = output, target = target, options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException(target + " build failed. Read the first Console error.");
    }
    [MenuItem("Tools/Mobile Game/3. Capture Screenshot (Play Mode)")]
    static void Capture()
    {
        if (!Application.isPlaying) { Debug.LogWarning("Enter Play Mode in Device Simulator first."); return; }
        Directory.CreateDirectory("Submission/Screenshots");
        string file = "Submission/Screenshots/" + SceneManager.GetActiveScene().name + "_" +
            Screen.width + "x" + Screen.height + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
        ScreenCapture.CaptureScreenshot(file);
        Debug.Log("Screenshot requested: " + file + ". Check the saved image before submission.");
    }
    // Headless validation entry point for a workstation with Unity installed:
    // Unity -batchmode -projectPath <project> -executeMethod MobileProjectSetup.Validate -quit -logFile validation.log
    public static void Validate()
    {
        foreach (string path in ScenePaths)
        {
            if (!File.Exists(path)) throw new FileNotFoundException(path);
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            MobileGameUI ui = MobileGameUI.Install(scene);
            if (ui.GetComponentsInChildren<SafeAreaPanel>(true).Length == 0)
                throw new InvalidOperationException(path + ": missing SafeArea");
            foreach (UnityEngine.UI.CanvasScaler scaler in ui.GetComponentsInChildren<UnityEngine.UI.CanvasScaler>(true))
                if (scaler.matchWidthOrHeight != 1f)
                    throw new InvalidOperationException(path + ": incorrect CanvasScaler");
            if (path.Contains("level") && ui.GetComponentsInChildren<TouchStick>(true).Length != 1)
                throw new InvalidOperationException(path + ": missing touch controls");
        }
        Debug.Log("COINBOUND scene validation passed. Physical device testing is still required.");
    }
}
