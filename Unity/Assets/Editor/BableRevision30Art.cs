using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class BableRevision30Art {
 const string Root="Assets/Resources/Bable/NewArt/";
 static Sprite[] Import(string name,int count,int w,int h,float ppu,float floor){
  var path=Root+name+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);var im=(TextureImporter)AssetImporter.GetAtPath(path);im.textureType=TextureImporterType.Sprite;im.spriteImportMode=SpriteImportMode.Multiple;im.spritePixelsPerUnit=ppu;im.filterMode=FilterMode.Point;im.mipmapEnabled=false;im.textureCompression=TextureImporterCompression.Uncompressed;im.alphaIsTransparency=true;im.maxTextureSize=8192;
  im.spritesheet=Enumerable.Range(0,count).Select(i=>new SpriteMetaData{name=name+"_"+i.ToString("D2"),rect=new Rect(i*w,0,w,h),alignment=9,pivot=new Vector2(.5f,(h-floor)/h)}).ToArray();im.SaveAndReimport();return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name).ToArray();
 }
 static void Clip(string actor,string action,Sprite[] frames,bool loop,float fps=16){
  string path="Assets/Art/NativeAnimations/"+actor+"_"+action+".anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}clip.frameRate=fps;
  AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding{path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"},frames.Select((s,i)=>new ObjectReferenceKeyframe{time=i/fps,value=s}).ToArray());var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;settings.stopTime=frames.Length/fps;AnimationUtility.SetAnimationClipSettings(clip,settings);EditorUtility.SetDirty(clip);
  var ctl=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Art/NativeAnimations/"+actor+".controller");var sm=ctl.layers[0].stateMachine;var state=sm.states.Select(x=>x.state).FirstOrDefault(x=>x.name=="right"+action)??sm.AddState("right"+action);state.motion=clip;EditorUtility.SetDirty(ctl);
 }
 public static void Apply(){
  Clip("Pilgrim","run",Import("PilgrimRun30",16,320,320,96,284),true);
  Clip("Pilgrim","unarmedrun",Import("PilgrimUnarmedRun30",16,320,320,96,284),true);
  var fall=Import("PilgrimBridge30",12,320,320,100,284);Clip("Pilgrim","bridgestumble",fall.Take(4).ToArray(),false,8);Clip("Pilgrim","bridgefall",fall.Skip(4).Take(4).ToArray(),false,8);Clip("Pilgrim","bridgeimpact",fall.Skip(8).Take(2).ToArray(),false,6);Clip("Pilgrim","bridgerise",fall.Skip(9).Take(3).ToArray(),false,6);
  var u=Import("PilgrimUnarmed30",12,320,320,102,284);Clip("Pilgrim","unarmedidle",new[]{u[4]},true);Clip("Pilgrim","unarmedtakeoff",new[]{u[5],u[6]},false);Clip("Pilgrim","unarmedjump",new[]{u[6]},true);Clip("Pilgrim","unarmedfall",new[]{u[7]},true);Clip("Pilgrim","unarmedland",new[]{u[8],u[9],u[4]},false);Clip("Pilgrim","unarmedreceive",new[]{u[4],u[10],u[11]},false,4);
  foreach(var actor in new[]{"MeleeGuard","ShieldGuard","RangedGuard","GiantGuard"}){
   var original=(TextureImporter)AssetImporter.GetAtPath(Root+actor+"AlternatingGait.png");var meta=original.spritesheet[0];int w=(int)meta.rect.width,h=(int)meta.rect.height;var patrol=Import(actor+"Patrol30",16,w,h,original.spritePixelsPerUnit,h*(1-meta.pivot.y));Clip(actor,"walk",patrol,true);
   AssetDatabase.ImportAsset(Root+actor+"AlternatingGait.png",ImportAssetOptions.ForceUpdate);
  }
  AssetDatabase.SaveAssets();
 }
}
