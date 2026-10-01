using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;
public static class BableMenuSceneAuthor
{
    [MenuItem("Bable/Open Startup Menu Scene")]
    public static void Open()=>EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
    public static string Create()
    {
        const string path="Assets/Scenes/MainMenu.unity";
        if(System.IO.File.Exists(path))return "MainMenu scene already exists; left unchanged.";
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("Menu Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.cullingMask=~0;camera.gameObject.AddComponent<AudioListener>();
        var root=new GameObject("Main Menu");root.AddComponent<Bable.BableGameUI>();root.AddComponent<Bable.BableAudio>();
        EditorSceneManager.SaveScene(scene,path);
        var scenes=EditorBuildSettings.scenes.Where(s=>s.path!=path).ToList();scenes.Insert(0,new EditorBuildSettingsScene(path,true));EditorBuildSettings.scenes=scenes.ToArray();
        AssetDatabase.SaveAssets();return "Independent menu saved as first build scene.";
    }
}
