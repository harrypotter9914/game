using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class BableGaitAnchors {
 [System.Serializable] class Entry {public string name;public float[] offsets;}
 [System.Serializable] class Catalog {public Entry[] items;}
 public static void Apply(){
  var catalog=JsonUtility.FromJson<Catalog>(File.ReadAllText("../reference/revision13/locomotion-anchors.json"));
  foreach(var entry in catalog.items){string path="Assets/Resources/Bable/NewArt/"+entry.name+"Locomotion.png";var imp=(TextureImporter)AssetImporter.GetAtPath(path);var frames=imp.spritesheet;for(int i=0;i<frames.Length;i++){frames[i].alignment=9;frames[i].pivot=new Vector2(.5f+entry.offsets[i],frames[i].pivot.y);}imp.spritesheet=frames;imp.SaveAndReimport();}
  AssetDatabase.SaveAssets();
 }
}
