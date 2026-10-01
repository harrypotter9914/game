using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Bable;
public static class BablePoseAudit {
 [Serializable] public class Frame {public string actor,clip,sprite,path;public float x,y,w,h,px,py,ppu,visualX,visualY,scaleX,scaleY,foot;}
 [Serializable] public class Report {public Frame[] frames;public string[] actors;}
 public static string Export(){
  var rows=new List<Frame>();var names=new List<string>();
  var actors=UnityEngine.Object.FindObjectsByType<CharacterPresentation>(FindObjectsSortMode.None).GroupBy(a=>a.animator.runtimeAnimatorController.name).Select(g=>g.First()).ToArray();
  foreach(var a in actors){names.Add(a.name+" / "+a.animator.runtimeAnimatorController.name);var col=a.GetComponent<BoxCollider2D>();float foot=col!=null?(col.offset.y-col.size.y/2)*a.transform.lossyScale.y:0;var v=a.animator.transform;
   foreach(var clip in a.animator.runtimeAnimatorController.animationClips.Distinct())foreach(var bind in AnimationUtility.GetObjectReferenceCurveBindings(clip))foreach(var key in AnimationUtility.GetObjectReferenceCurve(clip,bind)){
    var s=key.value as Sprite;if(s==null)continue;rows.Add(new Frame{actor=a.animator.runtimeAnimatorController.name,clip=clip.name,sprite=s.name,path=AssetDatabase.GetAssetPath(s),x=s.rect.x,y=s.rect.y,w=s.rect.width,h=s.rect.height,px=s.pivot.x,py=s.pivot.y,ppu=s.pixelsPerUnit,visualX=v.position.x-a.transform.position.x,visualY=v.position.y-a.transform.position.y,scaleX=v.lossyScale.x,scaleY=v.lossyScale.y,foot=foot});
   }
  }
  File.WriteAllText("../reference/revision12/pose-frames.json",JsonUtility.ToJson(new Report{frames=rows.ToArray(),actors=names.ToArray()},true));return actors.Length+" character controllers / "+rows.Count+" sprite samples";
 }
}
