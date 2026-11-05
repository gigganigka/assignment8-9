using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameScenes
{
    public static bool Load(string scene)
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        try
        {
            if (Application.CanStreamedLevelBeLoaded(scene)) SceneManager.LoadScene(scene);
#if UNITY_EDITOR
            else
            {
                // Editor play can start before the user has prepared Build Profiles.
                string path = "Assets/" + scene + ".unity";
                if (!System.IO.File.Exists(path)) throw new InvalidOperationException("Missing scene: " + path);
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
            }
#else
            else throw new InvalidOperationException("Scene is missing from the build: " + scene);
#endif
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("Cannot open " + scene + ": " + e.Message);
            if (MobileGameUI.Instance != null) MobileGameUI.Instance.ShowMessage("Cannot open level. See Console / Build Profiles.");
            return false;
        }
    }
}
