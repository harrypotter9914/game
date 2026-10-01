using System.Linq;
using UnityEngine;
using UnityEditor;
public static class BableRunRigImport {
 public static void Apply(){
  string path="Assets/Resources/Bable/NewArt/PilgrimRunRig.png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
  var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Multiple;imp.spritePixelsPerUnit=132;imp.filterMode=FilterMode.Point;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.alphaIsTransparency=true;imp.maxTextureSize=4096;
  imp.spritesheet=Enumerable.Range(0,8).Select(i=>new SpriteMetaData{name="PilgrimRunRig_"+i,rect=new Rect(i*320,0,320,320),alignment=9,pivot=new Vector2(.5f,36f/320)}).ToArray();imp.SaveAndReimport();
  var frames=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name).ToArray();var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Art/NativeAnimations/Pilgrim_run.anim");
  AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding{path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"},frames.Select((s,i)=>new ObjectReferenceKeyframe{time=i/12f,value=s}).ToArray());var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;settings.stopTime=8f/12;AnimationUtility.SetAnimationClipSettings(clip,settings);EditorUtility.SetDirty(clip);AssetDatabase.SaveAssets();
 }
}
