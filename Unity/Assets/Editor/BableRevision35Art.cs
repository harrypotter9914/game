using System.Linq;
using UnityEngine;
using UnityEditor;
public static class BableRevision35Art {
 public static void Apply(){foreach(var actor in new[]{"Pilgrim"})foreach(var action in actor=="Pilgrim"?new[]{"run","unarmedrun"}:new[]{"run","walk"}){
 string path="Assets/Resources/Bable/NewArt/"+actor+"Gait35_"+action+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Multiple;imp.spritePixelsPerUnit=100;imp.filterMode=FilterMode.Point;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.alphaIsTransparency=true;imp.maxTextureSize=4096;imp.spritesheet=Enumerable.Range(0,24).Select(i=>new SpriteMetaData{name=actor+"_"+action+"_"+i.ToString("D2"),rect=new Rect(i%8*384,(2-i/8)*384,384,384),alignment=9,pivot=new Vector2(.5f,48f/384)}).ToArray();imp.SaveAndReimport();var frames=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name).ToArray();var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Art/NativeAnimations/"+actor+"_"+action+".anim");clip.frameRate=24;AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding{path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"},frames.Select((s,i)=>new ObjectReferenceKeyframe{time=i/24f,value=s}).ToArray());var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;settings.stopTime=1;AnimationUtility.SetAnimationClipSettings(clip,settings);EditorUtility.SetDirty(clip);
 }AssetDatabase.SaveAssets();}
}


