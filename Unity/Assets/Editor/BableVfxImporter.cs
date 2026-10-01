using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
namespace Babel.EditorTools
{
    public static class BableVfxImporter
    {
        public static void Import()
        {
            const string path="Assets/Resources/Bable/NewArt/CombatVfxAtlas.png";
            var t=new Texture2D(2,2,TextureFormat.RGBA32,false);ImageConversion.LoadImage(t,File.ReadAllBytes(path));var pixels=t.GetPixels32();
            for(int i=0;i<pixels.Length;i++){var c=pixels[i];if(c.g>110&&c.g-System.Math.Max(c.r,c.b)>40)pixels[i]=new Color32(0,0,0,0);}
            t.SetPixels32(pixels);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());int w=t.width,h=t.height;Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path);var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Multiple;imp.spritePixelsPerUnit=100;imp.filterMode=FilterMode.Point;imp.alphaIsTransparency=true;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;
            int[] edges={0,390,620,958,1254};var rects=new List<SpriteMetaData>();
            for(int row=0;row<4;row++)for(int col=0;col<4;col++){int x=col*w/4,right=(col+1)*w/4,top=edges[row]*h/1254,bottom=edges[row+1]*h/1254;rects.Add(new SpriteMetaData{name="CombatVfx_"+(row*4+col).ToString("D2"),rect=new Rect(x,h-bottom,right-x,bottom-top),alignment=0,pivot=new Vector2(.5f,.5f)});}
            imp.spritesheet=rects.ToArray();EditorUtility.SetDirty(imp);AssetDatabase.WriteImportSettingsIfDirty(path);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);AssetDatabase.SaveAssets();
        }
    }
}
