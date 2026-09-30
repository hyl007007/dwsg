using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

// Run in a disposable project copy. Server settings affect the built scenes only.
public sealed class AndroidClientBuild : IProcessSceneWithReport
{
    public int callbackOrder { get { return 0; } }
    private static bool building;
    private static string authUrl, gameUrl, worldId;

    public static void Build()
    {
        string output = Environment.GetEnvironmentVariable("DWSG_ANDROID_OUTPUT");
        if (string.IsNullOrEmpty(output) || !Path.IsPathRooted(output))
            throw new BuildFailedException("DWSG_ANDROID_OUTPUT must be an absolute APK path.");
        authUrl = Endpoint("DWSG_AUTH_URL");
        gameUrl = Endpoint("DWSG_GAME_URL");
        worldId = Environment.GetEnvironmentVariable("DWSG_WORLD_ID") ?? "main";
        if (string.IsNullOrWhiteSpace(worldId)) throw new BuildFailedException("World ID is required.");

        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "dd.sg");
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
#if UNITY_6000_0_OR_NEWER
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
#else
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
#endif
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.Android.forceInternetPermission = true;
        PlayerSettings.Android.renderOutsideSafeArea = false;
        PlayerSettings.Android.useCustomKeystore = false;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
#if UNITY_2023_2_OR_NEWER
        // The project uses the original Input Manager and uGUI input fields.
        PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;
#endif
        EditorUserBuildSettings.buildAppBundle = false;
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0) throw new BuildFailedException("No enabled build scenes.");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        building = true;
        try
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.Android,
                options = Environment.GetEnvironmentVariable("DWSG_ANDROID_DEVELOPMENT") == "1"
                    ? BuildOptions.Development : BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("Android build failed: " + report.summary.result);
            Debug.Log("DWSG_ANDROID_BUILD_OK " + output);
        }
        finally { building = false; }
    }

    private static string Endpoint(string key)
    {
        string value = Environment.GetEnvironmentVariable(key);
        Uri uri;
        if (!Uri.TryCreate(value, UriKind.Absolute, out uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https") || !string.IsNullOrEmpty(uri.UserInfo))
            throw new BuildFailedException(key + " must be an HTTP(S) endpoint without credentials.");
        return value;
    }

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        if (!building || report == null || report.summary.platform != BuildTarget.Android) return;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var login in root.GetComponentsInChildren<登录脚本>(true))
            {
                login.服务器地址 = authUrl;
                login.游戏服务器地址 = gameUrl;
                login.联机世界 = worldId;
            }
    }
}
