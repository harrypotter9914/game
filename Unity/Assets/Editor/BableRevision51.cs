using System.IO;
using UnityEditor;
using UnityEngine;
public static class BableRevision51 {
 public static void Install(){
  foreach(var file in Directory.GetFiles("Assets/Resources/Bable/NewArt/Release51","*.png")){
   var importer=(TextureImporter)AssetImporter.GetAtPath(file.Replace('\\','/'));
   importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
   importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Bilinear;
   importer.maxTextureSize=file.Contains("Rune")?512:2048;importer.textureCompression=TextureImporterCompression.CompressedHQ;
   importer.SaveAndReimport();
  }
  PlayerSettings.bundleVersion="0.51.0";
  PlayerSettings.resizableWindow=true;
  AssetDatabase.SaveAssets();
  Debug.Log("REVISION51 IMPORT COMPLETE");
 }
 public static void Build(){Install();Babel.EditorTools.BabelSliceScaffolder.BuildWindowsPlayer();}
}

