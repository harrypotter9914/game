using UnityEngine;
using UnityEditor;
using TMPro;
public static class BableEleventhRevision
{
    public static string Apply(){
        const string frame="Assets/Resources/Bable/NewArt/GuidanceFrame.png";
        var im=(TextureImporter)AssetImporter.GetAtPath(frame);im.textureType=TextureImporterType.Sprite;im.spriteImportMode=SpriteImportMode.Single;im.alphaIsTransparency=true;im.textureCompression=TextureImporterCompression.Uncompressed;im.filterMode=FilterMode.Bilinear;im.maxTextureSize=4096;im.SaveAndReimport();
        foreach(var name in new[]{"CinzelDecorative-Regular","CrimsonText-Regular"}){
            string path="Assets/Resources/Bable/GuideFonts/"+name;
            if(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path+" SDF.asset")!=null)continue;
            var font=AssetDatabase.LoadAssetAtPath<Font>(path+".ttf");
            var asset=TMP_FontAsset.CreateFontAsset(font,64,8,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic);
            asset.name=name+" SDF";asset.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 ,.!?;:'-()/+·");
            AssetDatabase.CreateAsset(asset,path+" SDF.asset");AssetDatabase.AddObjectToAsset(asset.material,asset);foreach(var tex in asset.atlasTextures)AssetDatabase.AddObjectToAsset(tex,asset);EditorUtility.SetDirty(asset);
        }
        AssetDatabase.SaveAssets();return "Guidance frame and two TMP fonts imported";
    }
}
