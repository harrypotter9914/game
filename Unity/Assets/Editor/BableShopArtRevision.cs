using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
namespace Babel.EditorTools
{
    public static class BableShopArtRevision
    {
        public static void Apply(){
            const string path="Assets/Resources/Bable/NewArt/ShopIcons.png";
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);ImageConversion.LoadImage(texture,File.ReadAllBytes(path));var pixels=texture.GetPixels32();for(int i=0;i<pixels.Length;i++)if(pixels[i].g>120&&pixels[i].g-Math.Max(pixels[i].r,pixels[i].b)>35)pixels[i]=new Color32(0,0,0,0);texture.SetPixels32(pixels);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.Refresh();
            var sheet=JsonUtility.FromJson<BableArtPolish.Catalog>(File.ReadAllText("../reference/revision4/shop-slices.json")).items[0];var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Multiple;imp.spritePixelsPerUnit=100;imp.spritesheet=sheet.frames.Select((f,i)=>new SpriteMetaData{name="ShopIcon_"+i,rect=new Rect(f.x,f.y,f.width,f.height),alignment=9,pivot=new Vector2(.5f,.5f)}).ToArray();imp.filterMode=FilterMode.Point;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.alphaIsTransparency=true;imp.maxTextureSize=4096;imp.SaveAndReimport();var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name).ToArray();
            string[] items={"HealingProvision","ManaProvision","PilgrimBread","LanternOil","PenitentNeedle","QuickSilver","LongReach"};for(int i=0;i<items.Length;i++){var item=AssetDatabase.LoadAssetAtPath<Babel.Runtime.Shop.ShopItemDefinition>("Assets/Data/Shop/"+items[i]+".asset");var so=new SerializedObject(item);so.FindProperty("icon").objectReferenceValue=sprites[i];so.ApplyModifiedPropertiesWithoutUndo();}
            string coin="Assets/Prefabs/World/CoinPickup.prefab";var prefab=PrefabUtility.LoadPrefabContents(coin);var art=prefab.transform.Find("Relic Visual");art.GetComponent<SpriteRenderer>().sprite=sprites[7];art.localScale=Vector3.one*(.65f/sprites[7].bounds.size.y);PrefabUtility.SaveAsPrefabAsset(prefab,coin);PrefabUtility.UnloadPrefabContents(prefab);AssetDatabase.SaveAssets();
        }
    }
}
