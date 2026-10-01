using System.IO;
using UnityEditor;
using UnityEngine;
public static class BableRevision53 {
 [MenuItem("Bable/Install Responsive Menu Art")]
 public static void Install(){
  AssetDatabase.Refresh();
  foreach(var file in Directory.GetFiles("Assets/Resources/Bable/NewArt/Release53","*.png")){
   var importer=(TextureImporter)AssetImporter.GetAtPath(file.Replace('\\','/'));
   importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
   importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Bilinear;
   importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.CompressedHQ;
   importer.SaveAndReimport();
  }
  BablePlayerBuilds.Configure();AssetDatabase.SaveAssets();Debug.Log("REV53 ART INSTALLED");
 }
 public static void Build(){Install();BablePlayerBuilds.Both();}
}
