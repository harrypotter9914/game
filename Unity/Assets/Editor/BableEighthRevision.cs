using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Linq;
public static class BableEighthRevision
{
    public static void Apply(){
        foreach(var entry in EditorBuildSettings.scenes.Where(s=>s.enabled)){
            var scene=EditorSceneManager.OpenScene(entry.path);
            string backup="../reference/revision8/"+System.IO.Path.GetFileNameWithoutExtension(entry.path)+".before.unity";
            if(!System.IO.File.Exists(backup))System.IO.File.Copy(entry.path,backup);
            var panels=Object.FindObjectsByType<Bable.ForestAscentBackdrop>(FindObjectsSortMode.None).ToArray();
            for(int i=1;i<panels.Length;i++)Object.DestroyImmediate(panels[i].gameObject);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        EditorSceneManager.OpenScene("Assets/Scenes/Gameplay_Main.unity");
    }
}

