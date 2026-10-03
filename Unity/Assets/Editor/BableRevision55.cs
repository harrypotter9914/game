using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class BableRevision55 {
 const string Art="Assets/Resources/Bable/NewArt/",Anim="Assets/Art/NativeAnimations/";
 public static void Install(){
  Import("PilgrimVerticalDash55",140,new[]{
   Row("Pilgrim","dashup",0,475,new[]{0,443,887,1330,1774},new[]{270,699,1121,1561},new[]{428,430,431,438}),
   Row("Pilgrim","dashdown",475,887,new[]{0,443,887,1330,1774},new[]{253,697,1118,1564},new[]{751,792,823,790})});
  Import("PilgrimChargeDash55",144,new[]{
   Row("Pilgrim","charge",0,500,new[]{0,443,887,1338,1774},new[]{232,681,1120,1570},new[]{464,464,464,464}),
   Row("Pilgrim","crystaldash",500,887,new[]{0,443,891,1339,1774},new[]{200,650,1100,1550},new[]{790,790,790,790})});
  Import("BossRising55",132,new[]{
   Row("FirstPenitent","antiair",0,476,new[]{0,480,900,1340,1774},new[]{331,709,1157,1563},new[]{475,475,475,475}),
   Row("Nero","antiair",476,887,new[]{0,480,900,1340,1774},new[]{331,709,1157,1563},new[]{865,865,865,865})});
  Import("FirstQuick55",160,new[]{Row("FirstPenitent","quick",0,724,new[]{0,535,1080,1705,2172},new[]{330,795,1332,1940},new[]{580,580,580,580})});
  Import("BossRepairs55",146,new[]{
   Row("FirstPenitent","dead",0,445,new[]{0,443,832,1262,1774},new[]{227,667,1072,1520},new[]{422,422,422,422}),
   Row("Nero","cast",445,887,new[]{0,443,830,1380,1774},new[]{226,640,1068,1588},new[]{820,820,820,820})});
  BablePlayerBuilds.Configure();AssetDatabase.SaveAssets();
 }
 class Strip {public string actor,action;public int top,bottom;public int[] edges,roots,feet;}
 static Strip Row(string actor,string action,int top,int bottom,int[] edges,int[] roots,int[] feet)=>new Strip{actor=actor,action=action,top=top,bottom=bottom,edges=edges,roots=roots,feet=feet};
 static void Import(string sheet,float ppu,Strip[] rows){
  string path=Art+sheet+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
  var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.GetSourceTextureWidthAndHeight(out int width,out int height);
  var meta=new List<SpriteMetaData>();
  foreach(var r in rows)for(int c=0;c<4;c++){
   int x=r.edges[c],w=r.edges[c+1]-x,h=r.bottom-r.top;
   meta.Add(new SpriteMetaData{name=sheet+"_"+r.actor+"_"+r.action+"_"+c,rect=new Rect(x,height-r.bottom,w,h),alignment=9,pivot=new Vector2((r.roots[c]-x)/(float)w,(r.bottom-r.feet[c])/(float)h)});
  }
  importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.spritePixelsPerUnit=ppu;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.spritesheet=meta.ToArray();importer.SaveAndReimport();
  var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
  foreach(var r in rows){
   string cp=Anim+r.actor+"_"+r.action+".anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(cp);
   if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,cp);}clip.frameRate=10;
   AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding{path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"},Enumerable.Range(0,5).Select(i=>new ObjectReferenceKeyframe{time=i*.1f,value=sprites.Single(s=>s.name==sheet+"_"+r.actor+"_"+r.action+"_"+Mathf.Min(i,3))}).ToArray());
   var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;AnimationUtility.SetAnimationClipSettings(clip,settings);EditorUtility.SetDirty(clip);
   var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Anim+r.actor+".controller");var machine=controller.layers[0].stateMachine;
   var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="right"+r.action)??machine.AddState("right"+r.action);state.motion=clip;EditorUtility.SetDirty(controller);
  }
 }
 public static void Build(){BablePlayerBuilds.Both();}
}
