using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Bable;
using System.Linq;
namespace Babel.EditorTools
{
    public static class BableFinalPresentation
    {
        public static void Apply()
        {
            string[] names={"Abaddon, Herald of Ruin","Korah, the Earthbound","Azazel, the Exiled Watcher","Bel, the Gilded Idol","Nero, Heir of Nimrod"};
            for(int i=0;i<5;i++){
                string path="Assets/Prefabs/Bosses/Boss_"+(i+1)+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
                var brain=root.GetComponent<BossBrain>();brain.profile.displayName=names[i];EditorUtility.SetDirty(brain.profile);
                root.GetComponent<BossEncounter>().title=names[i];root.name=names[i];PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
            }
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Gameplay_Main.unity");
            foreach(var boss in Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None)){boss.GetComponent<BossEncounter>().title=names[(int)boss.profile.kind];boss.name=names[(int)boss.profile.kind];}
            var final=Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None).First(b=>b.profile.kind==BossKind.Nero);
            if(Object.FindFirstObjectByType<PrincessRescue>()==null){
                var go=new GameObject("Princess Livia");go.AddComponent<PrincessRescue>();
                var ground=GameObject.Find("GroundTilemap").GetComponent<UnityEngine.Tilemaps.Tilemap>();
                Vector2 wanted=final.arenaCenter+Vector2.right*final.arenaSize.x*.3f,best=final.transform.position;float score=float.MaxValue;
                foreach(var c in ground.cellBounds.allPositionsWithin){if(!ground.HasTile(c)||ground.HasTile(c+Vector3Int.up)||ground.HasTile(c+Vector3Int.up*2))continue;Vector2 p=ground.GetCellCenterWorld(c)+Vector3.up*.5f;if(!final.InArena(p))continue;float d=(p-wanted).sqrMagnitude;if(d<score){score=d;best=p;}}
                go.transform.position=best;var sr=go.AddComponent<SpriteRenderer>();sr.sprite=AssetDatabase.LoadAllAssetsAtPath("Assets/Resources/Bable/NewArt/Princess.png").OfType<Sprite>().OrderBy(s=>s.name).First();sr.sortingOrder=8;go.transform.localScale=Vector3.one*(2.3f/sr.sprite.bounds.size.y);
                go.AddComponent<Animator>().runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Art/NativeAnimations/Princess.controller");
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
    }
}
