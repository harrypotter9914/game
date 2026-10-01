using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Babel.Runtime.Core;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.World;
using Babel.Runtime.Combat;
namespace Bable {
 [DefaultExecutionOrder(-500)]
 public sealed class CampaignStore:MonoBehaviour {
  [Serializable] public class Data {
   public int version=1;public string savedAt;public GameSession.CampaignState player;
   public string[] removed=Array.Empty<string>(),purchases=Array.Empty<string>(),journal=Array.Empty<string>(),tutorials=Array.Empty<string>();
   public Vector3Int[] explored=Array.Empty<Vector3Int>();public bool bridge,princessArrived,completed;
  }
  const string Filename="campaign.json";
  static CampaignStore instance;static Data pending;static bool newJourney,ready,restoring,writeBlocked;
  static readonly HashSet<string> removed=new(),purchases=new();
  public static bool Ready=>ready;
  public static bool Restoring=>restoring;
  public static string Status {get;private set;}="";
  public static bool IsCampaign=>SceneManager.GetActiveScene().name=="Gameplay_Main";
  public static bool IsPractice=>SceneManager.GetActiveScene().name.StartsWith("Boss_Test_")||SceneManager.GetActiveScene().name=="Rune_Combat_Lab";
  float nextSave;GameSession hooked;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){instance=null;pending=null;ready=false;restoring=false;newJourney=false;writeBlocked=false;removed.Clear();purchases.Clear();Status="";}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Boot(){var g=new GameObject("Campaign persistence");DontDestroyOnLoad(g);instance=g.AddComponent<CampaignStore>();}
  public static string Identity(Component item){var p=item.transform.position;return item.GetType().Name+"/"+item.name+"/"+p.x.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+","+p.y.ToString("F3",System.Globalization.CultureInfo.InvariantCulture);}
  public static void MarkRemoved(string id){if(IsCampaign&&!restoring&&!string.IsNullOrEmpty(id)){removed.Add(id);ScheduleSave();}}
  public static bool WasRemoved(string id)=>removed.Contains(id);
  public static bool Purchased(string id)=>IsCampaign&&purchases.Contains(id);
  public static void Purchase(string id){if(IsCampaign){purchases.Add(id);ScheduleSave();}}
  public static bool TryRead(out Data data){
   data=null;if(BuildFlavor.Practice)return false;try{
    bool recovered;string json=LocalStorage.Read(Filename,out recovered);if(json==null)return false;
    data=JsonUtility.FromJson<Data>(json);if(data==null||data.version!=1||data.player==null||!float.IsFinite(data.player.checkpoint.x)||!float.IsFinite(data.player.checkpoint.y)||data.player.gold<0||!float.IsFinite(data.player.bonusDamage)||!float.IsFinite(data.player.bonusCooldown)||!float.IsFinite(data.player.bonusRange))throw new InvalidDataException("Save version or data is not supported");
    writeBlocked=false;if(recovered)Status="Recovered the previous save backup.";return true;
   }catch(Exception e){writeBlocked=true;Status="Save could not be read. Your files have been kept.";Debug.LogWarning(e.Message);data=null;return false;}
  }
  public static bool HasSave=>TryRead(out _);
  public static bool HasAnySave=>File.Exists(Path.Combine(LocalStorage.Root,Filename))||File.Exists(Path.Combine(LocalStorage.Root,Filename+".bak"));
  public static bool RequestContinue(){if(!TryRead(out pending))return false;newJourney=false;return true;}
  public static void RequestNew(){
   if(BuildFlavor.Practice)return;
   var path=Path.Combine(LocalStorage.Root,Filename);if(File.Exists(path))File.Copy(path,path+".before-new-game",true);
   pending=null;newJourney=true;writeBlocked=false;
  }
  public static bool BeforeLeave(){if(IsCampaign&&ready)return SaveNow();return true;}
  public static void ScheduleSave(){if(instance!=null&&IsCampaign&&ready&&!restoring)instance.nextSave=Mathf.Min(instance.nextSave,Time.unscaledTime+1);}
  public static IEnumerator RestoreScene(){
   ready=false;restoring=true;removed.Clear();purchases.Clear();
   if(!IsCampaign){restoring=false;yield break;}
   var session=GameSession.Instance;if(session==null){restoring=false;yield break;}
   var data=pending;pending=null;
   if(data==null&&!newJourney)TryRead(out data);
   if(data!=null&&!newJourney){
    foreach(var s in data.removed??Array.Empty<string>())removed.Add(s);foreach(var s in data.purchases??Array.Empty<string>())purchases.Add(s);
    session.ImportCampaign(data.player);
    foreach(var pickup in FindObjectsByType<CollectiblePickup>(FindObjectsSortMode.None))if(WasRemoved(pickup.PersistentId))pickup.gameObject.SetActive(false);
    foreach(var wall in FindObjectsByType<BreakableWall>(FindObjectsSortMode.None))if(WasRemoved(Identity(wall)))wall.RestoreBroken();
    foreach(var gate in FindObjectsByType<AbilityGate>(FindObjectsSortMode.None))if(WasRemoved(Identity(gate)))gate.RestoreOpen();
    foreach(var boss in FindObjectsByType<BossBrain>(FindObjectsSortMode.None))if(boss.profile!=null&&(data.player.bosses??Array.Empty<string>()).Contains(boss.profile.kind.ToString()))boss.gameObject.SetActive(false);
    if(data.bridge)FindFirstObjectByType<ScriptedBridgeCollapse>()?.RestoreCompleted();
    var princess=FindFirstObjectByType<PrincessRescue>();if(princess!=null)princess.RestoreProgress((data.player.bosses??Array.Empty<string>()).Contains("Nero"),data.princessArrived,data.completed);
    var journal=FindFirstObjectByType<ScrollJournal>();if(journal!=null){journal.recovered.Clear();foreach(var id in data.journal??Array.Empty<string>())journal.Recover(id,false);}
    ExploredAtlas.Current?.Restore(data.explored);NarrativeGuidance.Instance?.RestoreSeen(data.tutorials);
    var marker=FindObjectsByType<CheckpointMarker>(FindObjectsSortMode.None).OrderBy(m=>Vector2.Distance(m.Position,data.player.checkpoint)).FirstOrDefault();
    if(marker!=null&&Vector2.Distance(marker.Position,data.player.checkpoint)<1)FindFirstObjectByType<CheckpointService>()?.RegisterCheckpoint(marker);
    var player=FindFirstObjectByType<PlayerController2D>();if(player!=null){player.GetComponent<PlayerRespawnController>().Respawn(false);session.ImportCampaign(data.player);player.GetComponent<PlayerRuntimeState>().SynchronizeFromSession();}
   }
   newJourney=false;restoring=false;ready=true;
   if(instance!=null){if(instance.hooked!=null)instance.hooked.StateChanged-=instance.Dirty;instance.hooked=session;session.StateChanged+=instance.Dirty;instance.nextSave=Time.unscaledTime+2;}
   yield return null;
  }
  void Dirty(){ScheduleSave();}
  public static Data Capture(){var session=GameSession.Instance;var p=FindFirstObjectByType<PrincessRescue>();var data=new Data{savedAt=DateTime.UtcNow.ToString("o"),player=session.ExportCampaign(),removed=removed.ToArray(),purchases=purchases.ToArray(),journal=FindFirstObjectByType<ScrollJournal>()?.recovered.ToArray()??Array.Empty<string>(),tutorials=NarrativeGuidance.Instance?.ExportSeen()??Array.Empty<string>(),explored=ExploredAtlas.Current?.Export()??Array.Empty<Vector3Int>(),bridge=FindFirstObjectByType<ScriptedBridgeCollapse>()?.HasTriggered==true,princessArrived=p!=null&&p.Arrived,completed=p!=null&&p.Rescued};if(data.player.health<=0){data.player.health=session.MaxHealth;data.player.mana=session.MaxMana;}return data;}
  public static bool SaveNow(){
   if(!IsCampaign||!ready||restoring||GameSession.Instance==null)return true;
   if(writeBlocked){Status="Existing save is unreadable or from a newer version. It has not been overwritten.";return false;}
   if(BableGameUI.Instance!=null&&BableGameUI.Instance.CinematicActive)return false;
   try{LocalStorage.Write(Filename,JsonUtility.ToJson(Capture()));Status="Journey saved. Continue returns to your altar.";return true;}catch(Exception e){Status="Could not save. Check free space and folder permissions.";Debug.LogError("Campaign save: "+e.Message);return false;}
  }
  void Update(){if(!IsCampaign){ready=false;return;}if(ready&&!TowerLoading.Busy&&Time.unscaledTime>=nextSave){if(SaveNow())nextSave=Time.unscaledTime+12;else nextSave=Time.unscaledTime+2;}}
  void OnApplicationQuit(){SaveNow();}
  void OnApplicationFocus(bool focused){if(!focused){SaveNow();if(BableGameUI.Instance!=null&&BableGameUI.Instance.Ready&&!TowerLoading.Busy&&BableGameUI.Instance.Mode=="play"&&Babel.Runtime.Core.GameSession.Instance?.IsPaused!=true)BableGameUI.Instance.Pause();}}
  void OnDestroy(){if(hooked!=null)hooked.StateChanged-=Dirty;}
 }
}
