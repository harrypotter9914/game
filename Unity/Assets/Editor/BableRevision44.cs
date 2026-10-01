using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using Bable;
using Object=UnityEngine.Object;
public static class BableRevision44 {
 const string Art="Assets/Resources/Bable/NewArt/";
 public static void Apply(){
  if(EditorApplication.isPlaying)throw new Exception("Stop play first");
  ImportStone();
  var tm=GameObject.Find("GroundTilemap").GetComponent<Tilemap>();
  // This is the upper mouth, above the irreversible drop, not the pit bottom.
  typeof(BableRevision41).GetMethod("Gate",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{tm,"Return shaft upper seal",new RectInt(87,-53,6,2),"BlueCatacomb"});
  typeof(BableRevision41).GetMethod("Gate",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{tm,"Vine shaft upper seal",new RectInt(301,-39,4,2),"MossLabyrinth"});
  typeof(BableRevision41).GetMethod("Gate",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{tm,"Azazel upper entry seal",new RectInt(315,-27,2,6),"MossLabyrinth"});
  var walls=Object.FindObjectsByType<BreakableWall>(FindObjectsInactive.Include,FindObjectsSortMode.None);
  foreach(var wall in walls){wall.RefreshFractureArt();EditorUtility.SetDirty(wall);EditorUtility.SetDirty(wall.GetComponent<SpriteRenderer>());}
  CalibrateShield("run",92);CalibrateShield("walk",92);CalibrateShield("attack",107);
  EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
  File.WriteAllLines("../reference/revision44/wall-audit.txt",walls.GroupBy(w=>w.transform.parent==null?w.name:w.transform.parent.name).Select(g=>g.Key+" | "+g.Count()+" blocks | bounds "+new Vector2(g.Min(w=>w.GetComponent<Collider2D>().bounds.min.x),g.Min(w=>w.GetComponent<Collider2D>().bounds.min.y))+" to "+new Vector2(g.Max(w=>w.GetComponent<Collider2D>().bounds.max.x),g.Max(w=>w.GetComponent<Collider2D>().bounds.max.y))));
 }
 static void ImportStone(){
  string path=Art+"FracturedStone44.png";var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.spritePixelsPerUnit=627;imp.filterMode=FilterMode.Point;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.maxTextureSize=2048;imp.SaveAndReimport();
  var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
  for(int i=0;i<5;i++){string asset=Art+"FracturedStone44_"+i+".asset";if(AssetDatabase.LoadAssetAtPath<Sprite>(asset)!=null)continue;var rect=i==4?new Rect(0,0,tex.width,tex.height):new Rect(i%2*tex.width/2,i/2*tex.height/2,tex.width/2,tex.height/2);var s=Sprite.Create(tex,rect,new Vector2(.5f,.5f),tex.width/2f,0,SpriteMeshType.FullRect);s.name="FracturedStone44_"+i;AssetDatabase.CreateAsset(s,asset);}
 }
 static void CalibrateShield(string state,float ppu){
  var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Art/NativeAnimations/ShieldGuard_"+state+".anim");var bind=AnimationUtility.GetObjectReferenceCurveBindings(clip).First();var frames=AnimationUtility.GetObjectReferenceCurve(clip,bind);
  foreach(var old in frames.Select(k=>(Sprite)k.value).Distinct().ToArray()){
   if(old.name.StartsWith("Shield44_"))continue;
   string path="Assets/Art/NativeAnimations/Shield44_"+state+"_"+old.name+".asset";var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
   if(sprite==null){sprite=Sprite.Create(old.texture,old.rect,new Vector2(old.pivot.x/old.rect.width,old.pivot.y/old.rect.height),ppu,0,SpriteMeshType.FullRect);sprite.name="Shield44_"+state+"_"+old.name;AssetDatabase.CreateAsset(sprite,path);}
   for(int i=0;i<frames.Length;i++)if(frames[i].value==old)frames[i].value=sprite;
  }
  AnimationUtility.SetObjectReferenceCurve(clip,bind,frames);EditorUtility.SetDirty(clip);
 }
}
