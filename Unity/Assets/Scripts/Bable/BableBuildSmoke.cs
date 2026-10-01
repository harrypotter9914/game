using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Core;

namespace Bable
{
    // Explicit command-line smoke check of the exported player, independent of the Editor.
    public sealed class BableBuildSmoke : MonoBehaviour
    {
        string reportPath;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-bableSmokeReport");
            if(i>=0 && i+1<args.Length)new GameObject("Build Smoke Check").AddComponent<BableBuildSmoke>().reportPath=args[i+1];
        }
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);Debug.Log("BABLE_BUILD_SMOKE starting");
            yield return new WaitForSecondsRealtime(2);while(TowerLoading.Busy)yield return null;
            bool menu=BableGameUI.Instance!=null&&BableGameUI.Instance.IsTitleScene&&BableGameUI.Instance.Mode=="main"&&GameSession.Instance==null&&FindFirstObjectByType<PlayerController2D>()==null&&FindFirstObjectByType<VotiveHud>()==null&&GameObject.Find("GroundTilemap")==null;
            if(BuildFlavor.Practice){yield return PracticeChecks(menu);yield break;}
            BableGameUI.Instance.Begin();yield return null;
            bool prologue=BableGameUI.Instance.Mode=="prologue"&&FindFirstObjectByType<TowerPrologue>()!=null&&GameSession.Instance==null;
            FindFirstObjectByType<TowerPrologue>().Skip();yield return new WaitForSecondsRealtime(3);while(TowerLoading.Busy)yield return null;
            var ground=GameObject.Find("GroundTilemap").GetComponent<Tilemap>();
            bool sprites=false;foreach(var cell in ground.cellBounds.allPositionsWithin)if(ground.GetSprite(cell)!=null){sprites=true;break;}
            var player=FindFirstObjectByType<PlayerController2D>();
            bool grounded=player.IsGrounded && player.transform.position.y>-110 && BableGameUI.Instance.GameplayHudVisible;
            bool alive=GameSession.Instance.CurrentHealth>0;
            bool feedbackAudio=Resources.LoadAll<AudioClip>("Bable/Sfx48").Length==84&&Resources.Load<Shader>("Bable/NewArt/HitFlash48")!=null;
            bool feedbackRuntime=player.GetComponent<PlayerHitFeedback>()!=null;
            bool movementAudio=player.GetComponent<ActorMovementAudio>()!=null&&Resources.LoadAll<AudioClip>("Bable/Sfx49").Length==19&&FindObjectsByType<Babel.Runtime.Characters.Enemies.EnemyControllerBase>(FindObjectsSortMode.None).All(e=>e.GetComponent<ActorMovementAudio>()!=null);
            bool actorBodies=player.gameObject.layer==Babel.Runtime.Combat.ActorBodyCollision.PlayerBodyLayer&&Physics2D.GetIgnoreLayerCollision(8,9)&&!Physics2D.GetIgnoreLayerCollision(8,0)&&FindObjectsByType<Babel.Runtime.Characters.Enemies.EnemyControllerBase>(FindObjectsSortMode.None).All(e=>e.gameObject.layer==Babel.Runtime.Combat.ActorBodyCollision.EnemyBodyLayer);
            bool recoilRules=player.GetComponent<Babel.Runtime.Combat.HitRecoil>()!=null&&FindObjectsByType<Babel.Runtime.Characters.Enemies.EnemyControllerBase>(FindObjectsSortMode.None).All(e=>(e is BossBrain||e is Babel.Runtime.Characters.Enemies.GiantEnemyController)==(e.GetComponent<Babel.Runtime.Combat.HitRecoil>()==null));
            bool sustainedBeam=Resources.Load<Texture2D>("Bable/NewArt/PlayerBeam45")!=null&&player.Definition.ShockwaveRange==10&&player.Definition.ShockwaveDuration==2.5f&&player.Definition.ShockwaveTickInterval==.3f;
            var session=GameSession.Instance;session.UnlockAbility(AbilityId.Shockwave);var combat=player.GetComponent<PlayerCombatController>();int mana=session.CurrentMana;
            sustainedBeam&=combat.PerformShockwave();yield return new WaitForSeconds(.4f);
            sustainedBeam&=combat.IsShockwaveActive&&FindFirstObjectByType<DirectionalShockwaveVisual>()!=null&&session.CurrentMana==mana-1;
            feedbackRuntime&=FindFirstObjectByType<CombatSoundLoop>()!=null;
            yield return new WaitForSeconds(2.5f);sustainedBeam&=!combat.IsShockwaveActive;
            feedbackRuntime&=FindFirstObjectByType<CombatSoundLoop>()==null;
            int baseMana=session.MaxMana;bool bossMana=true;foreach(BossKind kind in Enum.GetValues(typeof(BossKind)))bossMana&=session.GrantBossMana(kind.ToString())&&!session.GrantBossMana(kind.ToString());bossMana&=session.MaxMana==baseMana+5;session.ResetForNewGame();bossMana&=session.MaxMana==baseMana;
            var walls=FindObjectsByType<BreakableWall>(FindObjectsSortMode.None);
            bool fractureArt=walls.Length>=635&&walls.All(w=>!w.requiresShockwave||w.GetComponent<SpriteRenderer>().sprite.name.StartsWith("FracturedStone44"));
            bool bossFrames=new[]{"Abaddon","Korah","Azazel","Bel","Nero"}.All(n=>Resources.Load<Sprite>("Bable/NewArt/BossHud44_"+n)!=null);
            bool burrowArt=Resources.LoadAll<Sprite>("Bable/NewArt/BurrowRubble44").Length==4;
            var bossHud=FindFirstObjectByType<BossHealthHud>();bool encounterHud=bossHud!=null&&!bossHud.Visible;
            bool noLabScene=!Application.CanStreamedLevelBeLoaded("Rune_Combat_Lab");
            bool noBossTestScenes=Enumerable.Range(1,5).All(i=>!Application.CanStreamedLevelBeLoaded("Boss_Test_"+i));
            BableGameUI.Instance.Pause();
            bool noPracticeButtons=!FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).Any(b=>b.name.Contains("PRACTICE")||b.name.Contains("LAB"));
            BableGameUI.Instance.Resume();BableGameUI.Instance.OpenLab();BableGameUI.Instance.Practice();yield return null;
            bool campaignClean=CampaignStore.IsCampaign&&BableGameUI.Instance.Mode=="play"&&BableGameUI.Instance.GameplayHudVisible;
            BableGameUI.Instance.Pause();bool pause=!BableGameUI.Instance.GameplayHudVisible&&Time.timeScale==0;BableGameUI.Instance.MainMenu();yield return new WaitForSecondsRealtime(3);while(TowerLoading.Busy)yield return null;bool returned=BableGameUI.Instance.IsTitleScene&&GameSession.Instance==null&&FindFirstObjectByType<PlayerController2D>()==null&&FindFirstObjectByType<VotiveHud>()==null;
            var report=new Result{prologue=prologue,pause=pause,returnMenu=returned,noLabScene=noLabScene,noBossTestScenes=noBossTestScenes,noPracticeButtons=noPracticeButtons,campaignClean=campaignClean,tileSprite=sprites,mainMenu=menu,groundCollision=grounded,alive=alive,passed=prologue&&sprites&&menu&&grounded&&alive&&noLabScene&&noBossTestScenes&&noPracticeButtons&&campaignClean&&pause&&returned};
            report.fractureArt=fractureArt;report.bossFrames=bossFrames;report.burrowArt=burrowArt;report.encounterHud=encounterHud;report.passed&=fractureArt&&bossFrames&&burrowArt&&encounterHud;
            report.sustainedBeam=sustainedBeam;report.bossMana=bossMana;report.passed&=sustainedBeam&&bossMana;
            report.actorBodies=actorBodies;report.passed&=actorBodies;
            report.recoilRules=recoilRules;report.passed&=recoilRules;
            report.feedbackAudio=feedbackAudio;report.feedbackRuntime=feedbackRuntime;report.passed&=feedbackAudio&&feedbackRuntime;
            report.movementAudio=movementAudio;report.passed&=movementAudio;
            File.WriteAllText(reportPath,JsonUtility.ToJson(report,true));
            Debug.Log("BABLE_BUILD_SMOKE "+JsonUtility.ToJson(report)+" mode="+BableGameUI.Instance.Mode);
            Application.Quit(report.passed?0:1);
        }
        float started;
        void Awake(){started=Time.realtimeSinceStartup;}
        void Update(){if(Time.realtimeSinceStartup-started>180){File.WriteAllText(reportPath,"{\"passed\":false,\"error\":\"Smoke timeout\"}");Application.Quit(2);}}
        IEnumerator Loaded(){yield return null;while(TowerLoading.Busy||BableGameUI.Instance==null||!BableGameUI.Instance.Ready)yield return null;yield return new WaitForSecondsRealtime(.5f);}
        void Click(string name){var b=FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).First(x=>x.name==name&&x.IsActive());b.onClick.Invoke();}
        IEnumerator PracticeChecks(bool menu){
            var checks=new System.Collections.Generic.List<string>();var failures=new System.Collections.Generic.List<string>();
            void Check(string name,bool pass){checks.Add(name);if(!pass)failures.Add(name);Debug.Log("PRACTICE_SMOKE "+(pass?"PASS ":"FAIL ")+name);}
            Check("Practice title has no gameplay HUD",menu);
            Check("Campaign scene excluded",!Application.CanStreamedLevelBeLoaded("Gameplay_Main"));
            Check("All six practice scenes included",Application.CanStreamedLevelBeLoaded("Rune_Combat_Lab")&&Enumerable.Range(1,5).All(i=>Application.CanStreamedLevelBeLoaded("Boss_Test_"+i)));
            Check("No campaign title actions",!FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).Any(b=>b.name=="NEW JOURNEY"||b.name=="CONTINUE JOURNEY"));
            Check("Independent default save identity",Application.productName=="bable-practice"&&BuildFlavor.StorageFolder=="BabelPractice");
            Check("Campaign saves cannot be read",!CampaignStore.TryRead(out _));
            BableGameUI.Instance.OpenSettings();yield return null;Check("Practice settings accessible",BableGameUI.Instance.Mode=="settings");Click("RETURN");yield return null;
            BableGameUI.Instance.OpenLab();yield return Loaded();var lab=FindFirstObjectByType<RuneCombatLab>();
            Check("Lab ready with eight runes",lab!=null&&lab.Ready&&GameSession.Instance.RuneInventory.CollectedRunes.Count==8);
            Check("Lab replenishes spending",GameSession.Instance.TrySpendGold(5000)&&GameSession.Instance.CurrentGold==999999);
            BableGameUI.Instance.Pause();Check("Practice pause freezes simulation",Time.timeScale==0);
            Click("CHAMBER TOOLS");yield return null;Check("Chamber tools present",BableGameUI.Instance.Mode=="tools");Click("RETURN TO CHAMBER");yield return null;
            Check("Chamber resumes",Time.timeScale==1&&BableGameUI.Instance.Mode=="play");
            for(int i=0;i<5;i++){
                BableGameUI.Instance.Practice();yield return null;Click(TowerLore.GuardianTitles[i]);yield return Loaded();
                var boss=FindFirstObjectByType<BossBrain>();var practice=FindFirstObjectByType<BossPractice>();
                Check("Guardian "+(i+1)+" loads shared profile",boss!=null&&boss.profile!=null&&(int)boss.profile.kind==i&&practice!=null&&practice.bossIndex==i+1);
                BableGameUI.Instance.Pause();Click("CHAMBER TOOLS");yield return null;
                Check("Guardian "+(i+1)+" has restart and control tools",BableGameUI.Instance.Mode=="tools"&&FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).Any(b=>b.name=="RESTART THIS ROOM"));
            }
            BableGameUI.Instance.ReturnToCampaign();yield return Loaded();
            Check("Return routes to practice title",BableGameUI.Instance.IsTitleScene&&GameSession.Instance==null);
            Check("No campaign file written",!File.Exists(Path.Combine(LocalStorage.Root,"campaign.json")));
            var result=new PracticeResult{passed=failures.Count==0,checks=checks.ToArray(),failures=failures.ToArray()};
            File.WriteAllText(reportPath,JsonUtility.ToJson(result,true));Application.Quit(result.passed?0:1);
        }
        [Serializable] class PracticeResult{public bool passed;public string[] checks,failures;}
        [Serializable] class Result {public bool prologue,tileSprite,mainMenu,groundCollision,alive,noLabScene,noBossTestScenes,noPracticeButtons,campaignClean,pause,returnMenu,fractureArt,bossFrames,burrowArt,encounterHud,sustainedBeam,bossMana,actorBodies,recoilRules,feedbackAudio,feedbackRuntime,movementAudio,passed;}
    }
}
