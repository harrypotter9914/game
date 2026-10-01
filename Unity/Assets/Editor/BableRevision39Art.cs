using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class BableRevision39Art {
    public static void Apply(){
        Directory.CreateDirectory("../reference/revision39/backup");
        // Calibrate against the upright recovery body, not the raised weapon's
        // bounding box. Weapons and magic can extend far above the character.
        ScaleSheet("GiantGuardExtras",104f);
        ScaleSheet("RangedGuardExtras",115f);
        string path="Assets/Art/NativeAnimations/GiantGuard_windup.anim";
        Backup(path);
        var attack=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Art/NativeAnimations/GiantGuard_attack.anim");
        var windup=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        var binding=AnimationUtility.GetObjectReferenceCurveBindings(attack).First(b=>b.propertyName=="m_Sprite");
        var frames=AnimationUtility.GetObjectReferenceCurve(attack,binding);
        // Same-sized anticipation poses as the following heavy slam.
        AnimationUtility.SetObjectReferenceCurve(windup,binding,new[]{
            new ObjectReferenceKeyframe{time=0,value=frames[0].value},
            new ObjectReferenceKeyframe{time=.28f,value=frames[1].value},
            new ObjectReferenceKeyframe{time=.5f,value=frames[1].value}});
        EditorUtility.SetDirty(windup);
        // Remove a constant seven-pixel baseline offset while retaining flight
        // phases of the stride. Do not glue every running frame to the floor.
        FootPivot("ShieldGuardGait34_walk",55);
        FootPivot("ShieldGuardGait34_run",55);
        FootPivot("RangedGuardGait34_walk",47);
        FootPivot("RangedGuardGait34_run",47);
        FootPivot("GiantGuardGait34_run",45);
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Gameplay_Main.unity");
        var gate=GameObject.Find("Dawn exterior - western gate");
        if(gate!=null&&gate.GetComponent<Bable.DawnRescueBackdrop>()==null)gate.AddComponent<Bable.DawnRescueBackdrop>();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }
    static void Backup(string path){string dest="../reference/revision39/backup/"+Path.GetFileName(path);if(!File.Exists(dest))File.Copy(path,dest);}
    static void ScaleSheet(string name,float ppu){string path="Assets/Resources/Bable/NewArt/"+name+".png";Backup(path+".meta");var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.spritePixelsPerUnit=ppu;imp.SaveAndReimport();}
    static void FootPivot(string name,float pixels){string path="Assets/Resources/Bable/NewArt/"+name+".png";Backup(path+".meta");var imp=(TextureImporter)AssetImporter.GetAtPath(path);var sprites=imp.spritesheet;for(int i=0;i<sprites.Length;i++)sprites[i].pivot=new Vector2(.5f,pixels/384f);imp.spritesheet=sprites;imp.SaveAndReimport();}
}
