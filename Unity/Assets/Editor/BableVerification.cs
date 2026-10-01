using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using Bable;
using Babel.Runtime.Core;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.Combat;
using Babel.Runtime.Runes;
using Babel.Runtime.World;
using Object = UnityEngine.Object;

public static class BableVerification
{
    public static string Output => Path.GetFullPath(Path.Combine(Application.dataPath,"../../reference"));
    [MenuItem("Bable/Verify Scene")]
    public static void VerifyScene()
    {
        var issues=new List<string>();
        foreach(var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            foreach(var t in root.GetComponentsInChildren<Transform>(true)) if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)issues.Add("Missing script: "+t.name);
        foreach(var p in Object.FindObjectsByType<CharacterPresentation>(FindObjectsSortMode.None))
            if(p.animator==null || p.animator.runtimeAnimatorController==null || p.animator.runtimeAnimatorController.animationClips.Length==0)issues.Add("No animation: "+p.name);
        if(Object.FindObjectsByType<BossEncounter>(FindObjectsSortMode.None).Length!=5)issues.Add("Expected five boss encounters");
        if(Resources.LoadAll<RuneDefinition>("Bable/Runes").Length!=8)issues.Add("Expected eight runes");
        var ground=GameObject.Find("GroundTilemap")?.GetComponent<Tilemap>();if(ground==null || ground.GetUsedTilesCount()<2)issues.Add("Missing modular ground tiles");
        foreach(var tile in AssetDatabase.FindAssets("t:Tile",new[]{"Assets/Art/Generated"}).Select(g=>AssetDatabase.LoadAssetAtPath<Tile>(AssetDatabase.GUIDToAssetPath(g)))) if(tile!=null && tile.name.StartsWith("Dungeon_") && tile.sprite==null)issues.Add("Missing persisted tile sprite: "+tile.name);
        File.WriteAllText(Path.Combine(Output,"scene-validation.json"),JsonUtility.ToJson(new Report{checks=new[]{"Missing scripts","Native animation controllers","Five bosses","Eight runes","Modular tilemap"},failures=issues.ToArray()},true));
        Debug.Log("BABLE_SCENE_VALIDATION: "+(issues.Count==0?"PASS":string.Join("; ",issues)));
    }
    [Serializable] public class Report { public string[] checks; public string[] failures; }
    [MenuItem("Bable/Capture Game")]
    public static void CaptureGame() => Capture("game-current.png");
    public static void Capture(string name)
    {
        var camera=Camera.main;if(camera==null)return;
        var canvases=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.renderMode==RenderMode.ScreenSpaceOverlay).OrderBy(c=>c.sortingOrder).ToArray();
        var rt=new RenderTexture(1600,900,24);var old=camera.targetTexture;var active=RenderTexture.active;
        var canvasOrders=canvases.Select(c=>c.sortingOrder).ToArray();
        for(int i=0;i<canvases.Length;i++){var c=canvases[i];c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=1;c.sortingOrder=2000+i;}
        camera.targetTexture=rt; Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;
        var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Output,name),image.EncodeToPNG());
        camera.targetTexture=old;RenderTexture.active=active;for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=RenderMode.ScreenSpaceOverlay;canvases[i].sortingOrder=canvasOrders[i];}Object.DestroyImmediate(image);Object.DestroyImmediate(rt);
    }
    [MenuItem("Bable/Run Gameplay Verification")]
    public static void Run()
    {
        if(!EditorApplication.isPlaying){Debug.LogError("Enter Play Mode first.");return;}
        new GameObject("Gameplay Verification").AddComponent<BableVerificationRunner>();
    }
}
public sealed class BableVerificationRunner : MonoBehaviour
{
    readonly List<string> checks=new(),failures=new();
    void Check(string name,bool result){checks.Add(name);if(!result)failures.Add(name);Debug.Log("BABLE_TEST "+(result?"PASS ":"FAIL ")+name);}
    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(.5f);
        var ui=BableGameUI.Instance;var session=GameSession.Instance;var player=Object.FindFirstObjectByType<PlayerController2D>();var body=player.GetComponent<Rigidbody2D>();var combat=player.GetComponent<PlayerCombatController>();var hp=player.GetComponent<HealthComponent>();
        BableVerification.Capture("01-main-menu.png"); Check("Main menu pauses simulation",session.IsPaused && Time.timeScale==0);
        ui.Begin();foreach(var e in Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None))e.enabled=false;
        foreach(var pickup in Object.FindObjectsByType<CollectiblePickup>(FindObjectsSortMode.None))pickup.gameObject.SetActive(false);
        player.SetTestInput(0,0);player.transform.position=new Vector3(0,2,0);body.linearVelocity=Vector2.zero;
        yield return new WaitForSeconds(.6f);Check("Player grounded on modular collision",player.IsGrounded && player.transform.position.y>.7f && player.transform.position.y<1.1f);
        float x=player.transform.position.x;player.SetTestInput(1,0);yield return new WaitForSeconds(.4f);player.SetTestInput(0,0);Check("Fixed-step movement",player.transform.position.x>x+1.5f);
        float y=player.transform.position.y;player.SetTestInput(0,0,true);yield return new WaitForSeconds(.3f);Check("Ground jump rises",player.transform.position.y>y+2);
        ui.Pause();var frozen=player.transform.position;yield return new WaitForSecondsRealtime(.25f);Check("Pause freezes physics and attacks",player.transform.position==frozen && !combat.PerformMeleeAttack());BableVerification.Capture("03-pause.png");ui.Resume();
        yield return new WaitForSeconds(1f);BableVerification.Capture("02-gameplay.png");
        player.transform.position=new Vector3(0,1,0);body.linearVelocity=Vector2.zero;player.SetTestInput(1,0);yield return new WaitForSeconds(.03f);player.SetTestInput(0,0);
        var melee=Object.FindFirstObjectByType<MeleeEnemyController>();melee.transform.position=new Vector3(1.2f,1,0);melee.GetComponent<Rigidbody2D>().bodyType=RigidbodyType2D.Kinematic;Physics2D.SyncTransforms();int enemyHp=melee.GetComponent<HealthComponent>().CurrentHealth;
        combat.PerformMeleeAttack();Check("Melee attack damages enemy through physics query",melee.GetComponent<HealthComponent>().CurrentHealth<enemyHp);
        melee.transform.position=new Vector3(20,1,0);
        var shield=Object.FindFirstObjectByType<ShieldSentinelController>();shield.transform.position=new Vector3(-2,1,0);shield.GetComponent<Rigidbody2D>().bodyType=RigidbodyType2D.Kinematic;
        typeof(EnemyControllerBase).GetProperty("FacingSign",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(shield,1);
        var hit=new DamageInfo(1,player.transform.position,Vector2.zero,player.gameObject,TeamAlignment.Player);bool blocked=!shield.TryReceiveDamage(hit);
        typeof(ShieldSentinelController).GetMethod("ExecuteShieldBash",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(shield,null);Check("Shield blocks front, then exposes attack recovery",blocked && shield.TryReceiveDamage(hit));shield.transform.position=new Vector3(30,1,0);
        session.SetHealth(4,6);session.SetMana(3,3);var heal=player.GetComponent<PlayerHealChannelController>();heal.TestHold=true;yield return new WaitForSeconds(3.15f);heal.TestHold=false;Check("Three-second healing consumes one mana",session.CurrentHealth==5 && session.CurrentMana==2);
        session.SetHealth(6,6);BableCombatFX.Projectile(melee.gameObject,new Vector2(-2,1),Vector2.right,8,1,Color.cyan);yield return new WaitForSeconds(.45f);Check("Enemy projectile travels and damages player",session.CurrentHealth<6);
        session.SetHealth(6,6);
        var inv=session.RuneInventory;inv.ResetInventory();var runes=Resources.LoadAll<RuneDefinition>("Bable/Runes");foreach(var r in runes)inv.Collect(r);
        Check("Eight collected runes, four active slots",inv.CollectedRunes.Count==8 && inv.ActiveSlots.Count==4 && inv.ActiveSlots.Count(r=>r!=null)==4);
        ui.Runes();yield return null;BableVerification.Capture("06-runes.png");ui.Resume();
        ui.Scroll("hoxi 10.1 builders");yield return null;Check("Original scroll opens and pauses",ui.Mode=="scroll" && session.IsPaused);BableVerification.Capture("07-scroll.png");ui.Resume();
        var shop=Object.FindFirstObjectByType<Babel.Runtime.Shop.ShopMenuController>();var keeper=Object.FindFirstObjectByType<Babel.Runtime.Shop.ShopkeeperController>();
        var stock=(Babel.Runtime.Shop.ShopItemDefinition[])typeof(Babel.Runtime.Shop.ShopkeeperController).GetField("stock",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(keeper);
        session.AddGold(100);shop.Open(stock);yield return null;BableVerification.Capture("08-shop.png");int gold=session.CurrentGold;
        typeof(Babel.Runtime.Shop.ShopMenuController).GetMethod("TryBuySelected",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(shop,null);Check("Shop purchase charges gold",session.CurrentGold<gold);shop.Close();
        foreach(var r in runes)inv.TryDeactivate(r.RuneId);
        var woman=runes.First(r=>r.RuneId==RuneId.Woman);inv.TryActivateToEmptySlot(woman);bool first=session.ConsumeReviveIfAvailable();inv.TryActivateToEmptySlot(woman);Check("Revival is consumed once",first && !session.ConsumeReviveIfAvailable());inv.ResetInventory();session.SetHealth(6,6);
        var gate=Object.FindObjectsByType<AbilityGate>(FindObjectsSortMode.None).First(g=>g.shockwaveOnly);gate.Strike(gate.transform.position,new Vector2(4,14));Check("Shockwave gate stays locked before ability",!gate.Open);
        var shock=Object.FindFirstObjectByType<ShockwaveBossController>();Check("Boss starts at configured max HP",shock.GetComponent<HealthComponent>().CurrentHealth==shock.Definition.MaxHealth);
        shock.GetComponent<HealthComponent>().ApplyDamage(999);Check("Shockwave boss unlocks ability",session.HasAbility(AbilityId.Shockwave));
        player.transform.position=new Vector3(59,1,0);body.linearVelocity=Vector2.zero;player.SetTestInput(1,0);session.SetMana(3,3);yield return new WaitForSeconds(.05f);player.SetTestInput(0,0);
        bool cast=combat.PerformShockwave();Check("Shockwave consumes mana and breaks gate",cast && session.CurrentMana==2 && gate.Open);
        session.SetMana(0,3);yield return new WaitForSeconds(1.7f);Check("No spell without mana",!combat.PerformShockwave());session.SetMana(3,3);
        var burrow=Object.FindFirstObjectByType<BurrowBossController>();burrow.GetComponent<HealthComponent>().ApplyDamage(999);Check("Burrow boss unlocks wall jump",session.HasAbility(AbilityId.WallJump));
        player.transform.position=new Vector3(116.5f,9,0);body.linearVelocity=new Vector2(0,-2);player.SetTestInput(1,0);yield return new WaitForSeconds(.2f);Check("Wall contact permits controlled slide",player.IsWallSliding);y=player.transform.position.y;player.SetTestInput(1,0,true);yield return new WaitForSeconds(.08f);Check("Wall jump pushes away and up",player.transform.position.y>y && body.linearVelocity.x<0);
        var sky=Object.FindFirstObjectByType<AerialJudgeBossController>();sky.GetComponent<HealthComponent>().ApplyDamage(999);Check("Aerial boss unlocks double jump",session.HasAbility(AbilityId.DoubleJump));
        player.transform.position=new Vector3(103,31,0);body.linearVelocity=Vector2.zero;player.SetTestInput(0,0);yield return new WaitForSeconds(.5f);player.SetTestInput(0,0,true);yield return new WaitForSeconds(.55f);player.SetTestInput(0,0,true);yield return new WaitForSeconds(.08f);Check("Second jump restores upward velocity",body.linearVelocity.y>10);
        var giant=GameObject.Find("giant_penitent");giant.GetComponent<HealthComponent>().ApplyDamage(999);Check("Fourth boss unlocks crystal dash",session.HasAbility(AbilityId.CrystalDash));
        player.transform.position=new Vector3(28,43,0);body.linearVelocity=Vector2.zero;player.SetTestInput(-1,0);session.SetMana(3,3);yield return new WaitForSeconds(.04f);player.SetTestInput(0,0);float startX=player.transform.position.x;bool dash=combat.PerformCrystalDash();yield return new WaitForSeconds(.46f);Check("Crystal dash crosses over eleven units",dash && startX-player.transform.position.x>11 && session.CurrentMana==2);yield return new WaitForSeconds(.5f);Check("Gravity restores after dash",body.gravityScale==3);Check("Dash gap lands on destination platform",player.transform.position.x<12 && player.transform.position.y>42 && player.IsGrounded);
        session.SetRespawnPoint(new Vector2(38,1));hp.ApplyDamage(999);yield return null;Check("Death opens paused death menu",ui.Mode=="death" && session.IsPaused);BableVerification.Capture("04-death.png");ui.Respawn();yield return new WaitForSeconds(.15f);Check("Respawn restores HP, mana and checkpoint",session.CurrentHealth==session.MaxHealth && session.CurrentMana==session.MaxMana && Mathf.Abs(player.transform.position.x-38)<.1f);
        var nero=Object.FindFirstObjectByType<FinalBossController>();
        typeof(FinalBossController).GetMethod("CastJudgement",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(nero,null);Check("Nero fires a seven-projectile fan",Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None).Length>=7);
        nero.GetComponent<HealthComponent>().ApplyDamage(nero.Definition.MaxHealth/2+1);Check("Nero enters second phase below half health",nero.PhaseTwo);
        nero.GetComponent<HealthComponent>().ApplyDamage(999);yield return null;Check("Final boss opens ending",ui.Mode=="victory" && session.IsPaused);BableVerification.Capture("05-ending.png");
        File.WriteAllText(Path.Combine(BableVerification.Output,"gameplay-validation.json"),JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));Debug.Log("BABLE_GAMEPLAY_VERIFICATION_COMPLETE "+(checks.Count-failures.Count)+"/"+checks.Count);
    }
}
