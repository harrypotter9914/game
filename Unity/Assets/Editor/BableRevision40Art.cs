using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class BableRevision40Art {
    public static void Apply(){
        string dir="Assets/Art/NativeAnimations/";
        var idle=AssetDatabase.LoadAssetAtPath<AnimationClip>(dir+"RangedGuard_idle.anim");
        var binding=AnimationUtility.GetObjectReferenceCurveBindings(idle).First(b=>b.propertyName=="m_Sprite");
        var rest=AnimationUtility.GetObjectReferenceCurve(idle,binding)[0].value;
        foreach(string name in new[]{"attack","upattack"}){
            string path=dir+"RangedGuard_"+name+".anim";
            string backup="../reference/revision40/backup/"+Path.GetFileName(path);
            if(!File.Exists(backup))File.Copy(path,backup);
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            var keys=AnimationUtility.GetObjectReferenceCurve(clip,binding);
            if(keys[3].value==rest)continue;
            // The old recovery cell contains a second, baked flying arrow.
            // Preserve the actor's poses; only the real projectile travels.
            if(name=="upattack")keys[2].value=AnimationUtility.GetObjectReferenceCurve(AssetDatabase.LoadAssetAtPath<AnimationClip>(dir+"RangedGuard_upattack.anim"),binding)[3].value;
            keys[3].value=rest;
            AnimationUtility.SetObjectReferenceCurve(clip,binding,keys);EditorUtility.SetDirty(clip);
        }
        AssetDatabase.SaveAssets();
    }
}
