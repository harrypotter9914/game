using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class BableSixteenthArtImport {
 public static void Apply(){
  foreach(var family in new[]{"FirstPenitent","BuriedOne","SkyJudicator","Nero","MeleeGuard","ShieldGuard","GiantGuard","RangedGuard"}){
   var row=File.ReadAllLines("../reference/revision16/run-sprites.csv").First(r=>r.StartsWith(family+"|0|")).Split('|');
   float width=float.Parse(row[5],System.Globalization.CultureInfo.InvariantCulture),height=float.Parse(row[6],System.Globalization.CultureInfo.InvariantCulture),pivotX=float.Parse(row[7],System.Globalization.CultureInfo.InvariantCulture),pivotY=float.Parse(row[8],System.Globalization.CultureInfo.InvariantCulture),ppu=float.Parse(row[9],System.Globalization.CultureInfo.InvariantCulture);
   string path="Assets/Resources/Bable/NewArt/"+family+"AlternatingGait.png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Multiple;imp.spritePixelsPerUnit=ppu;imp.filterMode=FilterMode.Point;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.alphaIsTransparency=true;imp.maxTextureSize=8192;
   imp.spritesheet=Enumerable.Range(0,16).Select(i=>new SpriteMetaData{name=family+"Gait_"+i.ToString("D2"),rect=new Rect(i*width,0,width,height),alignment=9,pivot=new Vector2(pivotX/width,pivotY/height)}).ToArray();imp.SaveAndReimport();
   string clipPath="Assets/Art/NativeAnimations/"+family+"_run.anim";string backup="../reference/revision16/backup/"+family+"_run.anim";if(!File.Exists(backup))File.Copy(clipPath,backup);
   var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);var frames=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name).ToArray();AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding{path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"},frames.Select((s,i)=>new ObjectReferenceKeyframe{time=i/16f,value=s}).ToArray());clip.frameRate=16;var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;settings.stopTime=1;AnimationUtility.SetAnimationClipSettings(clip,settings);EditorUtility.SetDirty(clip);
  }
  foreach(string name in new[]{"RuneSelectionOrnament","CheckpointAltar"}){
   string path="Assets/Resources/Bable/NewArt/"+name+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.spritePixelsPerUnit=name=="CheckpointAltar"?395f:100f;if(name=="CheckpointAltar"){var settings=new TextureImporterSettings();imp.ReadTextureSettings(settings);settings.spriteAlignment=9;settings.spritePivot=new Vector2(.5f,40f/1254);imp.SetTextureSettings(settings);}imp.filterMode=name=="CheckpointAltar"?FilterMode.Point:FilterMode.Bilinear;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.alphaIsTransparency=true;imp.maxTextureSize=2048;imp.SaveAndReimport();
  }
  AssetDatabase.SaveAssets();
 }
}
