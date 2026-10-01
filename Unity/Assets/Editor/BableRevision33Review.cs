using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class BableRevision33Review {
 public static string Run(){
  var report=new List<string>();var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  var camObj=new GameObject("Motion review camera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camObj,scene);
  var cam=camObj.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=2.5f;cam.transform.position=new Vector3(0,0,-10);cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.12f,.14f,.17f);cam.cullingMask=1<<31;
  var rt=new RenderTexture(1200,600,24);cam.targetTexture=rt;var tex=new Texture2D(1200,600,TextureFormat.RGB24,false);
  try {foreach(var actor in new[]{"Pilgrim","MeleeGuard","ShieldGuard","RangedGuard","GiantGuard"}){
   var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Art/NativeAnimations/"+actor+".controller");
   string[] actions=actor=="Pilgrim"?new[]{"idle","run","unarmedrun"}:new[]{"idle","walk","run"};
   var objects=new List<GameObject>();var clips=new List<AnimationClip>();
   for(int j=0;j<3;j++){
    var go=new GameObject(actor+" "+actions[j]);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);go.layer=31;go.AddComponent<SpriteRenderer>();go.transform.position=new Vector3((j-1)*2.6f,-1.5f,0);objects.Add(go);
    var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Art/NativeAnimations/"+actor+"_"+actions[j]+".anim");clips.Add(clip);
    var state=controller.layers[0].stateMachine.states.FirstOrDefault(s=>s.state.name=="right"+actions[j]).state;
    report.Add((state!=null&&state.motion==clip?"PASS ":"FAIL ")+actor+" controller "+actions[j]);
    if(j>0){var binding=AnimationUtility.GetObjectReferenceCurveBindings(clip).First(b=>b.propertyName=="m_Sprite");var frames=AnimationUtility.GetObjectReferenceCurve(clip,binding);bool ok=frames.Length==24&&frames.All(f=>AssetDatabase.GetAssetPath(f.value).Contains("Gait33_"));report.Add((ok?"PASS ":"FAIL ")+actor+" "+actions[j]+" 24 current sprites");}
   }
   var folder="../reference/revision33/unity-"+actor;Directory.CreateDirectory(folder);
   for(int f=0;f<24;f++){for(int j=0;j<3;j++){var b=AnimationUtility.GetObjectReferenceCurveBindings(clips[j]).First(x=>x.propertyName=="m_Sprite");var keys=AnimationUtility.GetObjectReferenceCurve(clips[j],b);objects[j].GetComponent<SpriteRenderer>().sprite=(Sprite)keys[Math.Min(keys.Length-1,(int)(f/24f*keys.Length))].value;}cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1200,600),0,0);tex.Apply();File.WriteAllBytes(folder+"/"+f.ToString("D2")+".png",tex.EncodeToPNG());}
   foreach(var go in objects)UnityEngine.Object.DestroyImmediate(go);
  }}finally{RenderTexture.active=null;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(camObj);}
  File.WriteAllLines("../reference/revision33/unity-audit.txt",report);return string.Join("\n",report);
 }
}



