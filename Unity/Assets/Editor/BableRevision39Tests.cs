using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Bable;
using Babel.Runtime.Core;
using Babel.Runtime.Combat;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.Runes;
using Babel.Runtime.Shop;
using Babel.Runtime.World;
using Object=UnityEngine.Object;

public static class BableRevision39Tests {
    static readonly List<string> checks=new(),failures=new(),trace=new();
    static readonly BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    static void Check(string title,bool pass){checks.Add(title);if(!pass)failures.Add(title);File.WriteAllText("../reference/revision39/runtime-tests.json",JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));}
    static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,Private).Invoke(target,args);
    static bool Clear(Collider2D shape)=>!Physics2D.OverlapBoxAll(shape.bounds.center,shape.bounds.size-Vector3.one*.08f,0).Any(c=>c!=shape&&TerrainMotion.Solid(c)&&Physics2D.Distance(shape,c).distance<-.06f);
    static float FloorGap(Collider2D shape){var box=(BoxCollider2D)shape;Vector2 center=box.transform.TransformPoint(box.offset);float foot=center.y-TerrainMotion.Size(box).y/2;float gap=999;foreach(var h in Physics2D.RaycastAll(new Vector2(center.x,foot+.05f),Vector2.down,2))if(TerrainMotion.Solid(h.collider))gap=Mathf.Min(gap,foot-h.point.y);return gap;}
    static void Move(GameObject go,Vector2 position){go.transform.position=position;var rb=go.GetComponent<Rigidbody2D>();if(rb!=null){rb.position=position;rb.linearVelocity=Vector2.zero;}Physics2D.SyncTransforms();}
    public static void Run(){Directory.CreateDirectory("../reference/revision39");checks.Clear();failures.Clear();trace.Clear();new GameObject("Revision39 regression").AddComponent<BableTestHost>().StartCoroutine(Audit());}
    static IEnumerator Audit(){
        while(TowerLoading.Busy||BableGameUI.Instance==null||!BableGameUI.Instance.Ready)yield return null;
        var ui=BableGameUI.Instance;ui.Resume();
        var p=Object.FindFirstObjectByType<PlayerController2D>();var body=p.GetComponent<Rigidbody2D>();var hp=p.GetComponent<HealthComponent>();var s=GameSession.Instance;
        p.SetTestInput(0,0);s.UnlockWeapon();hp.Invincible=true;
        var enemies=Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None);
        foreach(var e in enemies){e.enabled=false;e.GetComponent<Rigidbody2D>().simulated=false;}
        foreach(var speech in Object.FindObjectsByType<BossSpeech>(FindObjectsSortMode.None))speech.enabled=false;
        foreach(var zone in Object.FindObjectsByType<SummoningEncounter>(FindObjectsSortMode.None))zone.enabled=false;
        TowerDialogue.AbortStory();
        yield return new WaitForSeconds(.3f);
        s.SetHealth(2,s.MaxHealth);s.SetMana(1,s.MaxMana);
        var bridge=Object.FindFirstObjectByType<ScriptedBridgeCollapse>();
        Move(p.gameObject,new Vector2(bridge.firstCell.x+2,14));Camera.main.GetComponent<CameraFollow2D>().SetTarget(p.transform);bridge.Trigger(p);
        var phases=new HashSet<string>();float end=Time.time+12;Vector2 previous=body.position;bool continuous=true,black=false,grounded=true;float fallDuration=0;
        bool pausedFall=false;int reel=0;float nextShot=0;Directory.CreateDirectory("../reference/revision39/fall-reel");
        while(bridge.IsRunning&&Time.time<end){
            yield return new WaitForEndOfFrame();
            if(bridge.Phase=="Fall"&&!pausedFall){pausedFall=true;ui.Pause();ui.Respawn();Check("Checkpoint cannot interrupt an active cinematic",!TowerLoading.Busy);Vector2 at=body.position;yield return new WaitForSecondsRealtime(.35f);Check("Pausing a collapse freezes descent",Vector2.Distance(at,body.position)<.001f&&bridge.IsRunning);ui.Resume();}
            if(Time.time>=nextShot){nextShot=Time.time+.12f;BableVerification.Capture("revision39/fall-reel/"+(reel++).ToString("D3")+".png");}
            // Allow the fixed-step/render cadence, but reject any multi-unit teleport.
            continuous&=Vector2.Distance(previous,body.position)<Mathf.Max(1.5f,Time.deltaTime*22f);previous=body.position;
            black|=GameObject.Find("Collapse transition")!=null;
            if(bridge.Phase=="Fall"||bridge.Phase=="Approach")fallDuration+=Time.deltaTime;
            if(bridge.Phase=="Impact"||bridge.Phase=="Rise")grounded&=Mathf.Abs(FloorGap(p.GetComponent<Collider2D>()))<.1f&&Clear(p.GetComponent<Collider2D>());
            if(phases.Add(bridge.Phase))BableVerification.Capture("revision39/bridge-"+bridge.Phase+".png");
        }
        Check("Bridge completes all five visible phases",new[]{"Stumble","Fall","Approach","Impact","Rise"}.All(phases.Contains)&&bridge.Phase=="Complete");
        Check("Bridge traverses shaft continuously without black curtain",continuous&&!black&&fallDuration>2);
        Check("Bridge impact and rise remain on supporting floor",grounded);
        Check("Bridge retains health and mana",s.CurrentHealth==2&&s.CurrentMana==1);
        Check("Bridge restores controls and simulation",p.enabled&&body.simulated&&p.GetComponent<PlayerCombatController>().enabled);
        Check("Bridge actually removes crossing tiles",!GameObject.Find("GroundTilemap").GetComponent<UnityEngine.Tilemaps.Tilemap>().HasTile(bridge.firstCell));
        trace.Add("Bridge fall seconds="+fallDuration+" end="+body.position);
        p.enabled=false;body.simulated=false;
        var forest=Object.FindFirstObjectByType<ForestAscentBackdrop>();
        foreach(float x in new[]{140f,157f,173f,157f,140f}){
            Move(p.gameObject,new Vector2(x,15));Camera.main.GetComponent<CameraFollow2D>().SetTarget(p.transform);
            float last=forest.Opacity,lastTime=Time.time;bool smooth=true;float until=Time.time+1.2f;
            while(Time.time<until){yield return new WaitForEndOfFrame();smooth&=Mathf.Abs(forest.Opacity-last)<=(Time.time-lastTime)*1.3f+.025f;last=forest.Opacity;lastTime=Time.time;}
            Check("Forest blend matches position smoothly x="+x,Mathf.Abs(forest.Opacity-ForestAscentBackdrop.VisibilityAt(p.transform.position))<.025f&&smooth);
            trace.Add("Forest x="+x+" alpha="+forest.Opacity+" expected="+ForestAscentBackdrop.VisibilityAt(p.transform.position)+" smooth="+smooth);BableVerification.Capture("revision39/forest-"+x+".png");
        }
        var dawn=Object.FindObjectsByType<DawnRescueBackdrop>(FindObjectsSortMode.None).First(x=>x.name.Contains("western"));
        foreach(float x in new[]{181f,163f,149f}){Move(p.gameObject,new Vector2(x,80));Camera.main.GetComponent<CameraFollow2D>().SetTarget(p.transform);yield return new WaitForSeconds(1.1f);Check("Dawn gradually revealed at exit x="+x,Mathf.Abs(dawn.Opacity-DawnRescueBackdrop.VisibilityAt(p.transform.position,false))<.03f);BableVerification.Capture("revision39/dawn-"+x+".png");}
        Move(p.gameObject,new Vector2(188.5f,-36.1f));body.simulated=true;p.enabled=true;yield return new WaitForSeconds(.6f);
        s.SetHealth(2,s.MaxHealth);s.SetMana(1,s.MaxMana);int gold=s.CurrentGold;
        ui.Pause();Vector2 pausedAt=body.position;float clock=Time.time;yield return new WaitForSecondsRealtime(.6f);
        Check("Pause freezes time and body, preserving resources",Time.time==clock&&Vector2.Distance(body.position,pausedAt)<.001f&&s.CurrentHealth==2&&s.CurrentMana==1&&s.CurrentGold==gold);
        hp.Invincible=false;hp.ReceiveDamage(new DamageInfo(1,p.transform.position,Vector2.zero,null,TeamAlignment.Enemy));Check("Incoming damage rejected during pause",hp.CurrentHealth==2);ui.Resume();hp.Invincible=true;
        Check("Resume retains resources",s.CurrentHealth==2&&s.CurrentMana==1&&!s.IsPaused&&Time.timeScale==1);
        p.GetComponent<PlayerRuntimeState>().enabled=false;p.GetComponent<PlayerRuntimeState>().enabled=true;s.SetHealth(3,s.MaxHealth);Check("Re-enabled player state stays subscribed",hp.CurrentHealth==3);
        yield return CheckShop(p,s);
        yield return CheckCombinations(s);
        s.RuneInventory.ResetInventory();
        // Actual death menu -> loading -> altar recovery, not a direct health setter.
        var altar=Object.FindObjectsByType<AltarCheckpoint>(FindObjectsSortMode.None).First();altar.Activate();
        hp.Invincible=false;hp.ApplyDamage(999);yield return null;Check("Death opens a paused death menu",ui.Mode=="death"&&s.IsPaused&&hp.CurrentHealth==0);
        ui.Respawn();while(TowerLoading.Busy)yield return null;yield return new WaitForSeconds(1.2f);
        Check("Respawn restores health and mana",s.CurrentHealth==s.MaxHealth&&s.CurrentMana==s.MaxMana&&hp.CurrentHealth==hp.MaxHealth);
        Check("Respawn completes on altar with active controls",p.enabled&&body.simulated&&Mathf.Abs(FloorGap(p.GetComponent<Collider2D>()))<.1f&&!s.IsPaused);
        hp.Invincible=true;p.enabled=false;body.simulated=false;
        var constraints=body.constraints;float gravity=body.gravityScale;body.simulated=true;body.constraints=RigidbodyConstraints2D.FreezeAll;body.gravityScale=0;
        foreach(var e in enemies.Where(x=>!(x is BossBrain)).GroupBy(x=>x.GetType()).Select(g=>g.First())){
            var rb=e.GetComponent<Rigidbody2D>();rb.simulated=true;e.enabled=true;
            Move(e.gameObject,new Vector2(172,-35));Move(p.gameObject,new Vector2(172+Mathf.Min(6,e.Definition.ChaseRange*.8f),-36.15f));
            var bounds=e.GetComponent<Collider2D>();float stop=Time.time+4;var states=new HashSet<string>();bool clear=true;float minX=e.transform.position.x,maxX=minX;
            while(Time.time<stop){yield return new WaitForFixedUpdate();states.Add(e.BehaviourState);clear&=Clear(bounds);minX=Mathf.Min(minX,e.transform.position.x);maxX=Mathf.Max(maxX,e.transform.position.x);}
            Check(e.GetType().Name+" chase moves without embedding",maxX-minX>1&&clear&&states.Contains("Pursue"));trace.Add(e.name+" states="+string.Join(",",states)+" travel="+(maxX-minX));
            Move(p.gameObject,new Vector2(e.transform.position.x-2,e.transform.position.y));yield return new WaitForSeconds(2.5f);Check(e.GetType().Name+" turns toward passed player",e.LookDirection==-1);
            Camera.main.GetComponent<CameraFollow2D>().SetTarget(e.transform);BableVerification.Capture("revision39/ai-"+e.GetType().Name+".png");
            e.enabled=false;rb.simulated=false;
        }
        body.constraints=constraints;body.gravityScale=gravity;body.simulated=false;
        foreach(var b in enemies.OfType<BossBrain>()){
            b.ResetEncounter();b.enabled=true;var rb=b.GetComponent<Rigidbody2D>();var box=b.GetComponent<Collider2D>();if(b.profile.kind!=BossKind.Burrow)rb.simulated=true;
            Move(p.gameObject,(Vector2)b.transform.position+Vector2.right*5);var states=new HashSet<string>();bool clear=true,inside=true,trail=true,ground=true;int sample=0;float stop=Time.time+14;float travel=0;Vector2 old=b.transform.position;
            while(Time.time<stop){yield return new WaitForFixedUpdate();sample++;states.Add(b.State);if(box.enabled&&rb.simulated)clear&=Clear(box);inside&=b.InArena(b.transform.position);travel+=Vector2.Distance(old,b.transform.position);old=b.transform.position;
                if(sample==200){Move(p.gameObject,(Vector2)b.transform.position+Vector2.left*5);b.GetComponent<HealthComponent>().ApplyDamage(Mathf.Max(1,b.GetComponent<HealthComponent>().CurrentHealth/2+1));}
                if(b.State=="Burrow")trail&=b.GetComponent<BurrowPresentation>().TrailVisible;
                if(b.State=="EmergeWarning"&&b.StateProgress>.8f)ground&=Mathf.Abs(FloorGap(box))<.1f;
            }
            Check(b.profile.kind+" cycles AI states",states.Count>=3);Check(b.profile.kind+" stays clear of terrain and inside arena",clear&&inside);
            if(b.profile.kind==BossKind.Burrow)Check("Burrow remains visible and emerges at ground",trail&&ground);
            if(b.profile.kind==BossKind.Crystal)Check("Flying boss actively routes toward target",travel>8&&b.GetComponent<BossFlightRoute>().PlansMade>1);
            trace.Add(b.profile.kind+" states="+string.Join(",",states)+" travel="+travel+" clear="+clear+" inside="+inside);
            Camera.main.GetComponent<CameraFollow2D>().SetTarget(b.transform);BableVerification.Capture("revision39/boss-"+b.profile.kind+".png");b.enabled=false;rb.simulated=false;
        }
        Check("Five campaign bosses and four minion types audited",enemies.OfType<BossBrain>().Count()==5&&enemies.Where(e=>!(e is BossBrain)).Select(e=>e.GetType()).Distinct().Count()==4);
        File.WriteAllLines("../reference/revision39/runtime-trace.txt",trace);File.WriteAllText("../reference/revision39/complete.txt",checks.Count+" checks; "+failures.Count+" failures");Debug.Log("REVISION39_COMPLETE "+checks.Count+" / failures "+failures.Count);
    }
    static IEnumerator CheckShop(PlayerController2D p,GameSession s){
        var shop=Object.FindFirstObjectByType<ShopMenuController>();var merchant=Object.FindFirstObjectByType<ShopkeeperController>();var stock=(ShopItemDefinition[])typeof(ShopkeeperController).GetField("stock",Private).GetValue(merchant);
        s.RuneInventory.ResetInventory();s.AddGold(5000);s.SetHealth(1,s.MaxHealth);s.SetMana(0,s.MaxMana);
        foreach(var item in stock){float before=item.EffectType switch{ShopItemEffectType.MaxHealth=>s.MaxHealth,ShopItemEffectType.MaxMana=>s.MaxMana,ShopItemEffectType.RestoreHealth=>s.CurrentHealth,ShopItemEffectType.RestoreMana=>s.CurrentMana,ShopItemEffectType.DamageBoost=>s.DamageMultiplier,ShopItemEffectType.AttackSpeed=>s.AttackCooldownMultiplier,_=>s.AttackRangeMultiplier};
            int gold=s.CurrentGold;shop.Open(new[]{item});yield return null;Call(shop,"TryBuySelected");float after=item.EffectType switch{ShopItemEffectType.MaxHealth=>s.MaxHealth,ShopItemEffectType.MaxMana=>s.MaxMana,ShopItemEffectType.RestoreHealth=>s.CurrentHealth,ShopItemEffectType.RestoreMana=>s.CurrentMana,ShopItemEffectType.DamageBoost=>s.DamageMultiplier,ShopItemEffectType.AttackSpeed=>s.AttackCooldownMultiplier,_=>s.AttackRangeMultiplier};
            Check("Purchase applies "+item.EffectType,after!=before&&s.CurrentGold==gold-item.Price);
            if(item.OneTimePurchase){gold=s.CurrentGold;Call(shop,"TryBuySelected");Check("Cannot buy permanent item twice "+item.EffectType,s.CurrentGold==gold);}
            shop.Close();
        }
        Check("Shop closes and resumes gameplay",!shop.IsOpen&&!s.IsPaused);
    }
    static IEnumerator CheckCombinations(GameSession s){
        var runes=Resources.LoadAll<RuneDefinition>("Bable/Runes").OrderBy(r=>r.RuneId).ToArray();foreach(var r in runes)s.CollectRune(r);
        float baseRange=s.AttackRangeMultiplier,baseCooldown=s.AttackCooldownMultiplier,baseDamage=s.DamageMultiplier;int baseHp=s.MaxHealth,count=0;
        for(int a=0;a<8;a++)for(int b=a+1;b<8;b++)for(int c=b+1;c<8;c++)for(int d=c+1;d<8;d++){
            foreach(var r in s.RuneInventory.ActiveSlots.ToArray())if(r!=null)s.RuneInventory.TryDeactivate(r.RuneId);
            var combo=new[]{runes[a],runes[b],runes[c],runes[d]};foreach(var r in combo)s.RuneInventory.TryActivateToEmptySlot(r);s.SetHealth(s.MaxHealth,s.MaxHealth);
            bool has(RuneId id)=>combo.Any(r=>r.RuneId==id);
            bool pass=s.MaxHealth==baseHp+(has(RuneId.Romance)?1:0)&&Mathf.Approximately(s.AttackRangeMultiplier,baseRange*(has(RuneId.Sun)?1.4f:1))&&Mathf.Approximately(s.AttackCooldownMultiplier,baseCooldown/(has(RuneId.Star)?1.4f:1))&&Mathf.Approximately(s.ShopPriceMultiplier,has(RuneId.Harvest)?.8f:1)&&Mathf.Approximately(s.DamageMultiplier,baseDamage*(has(RuneId.Romance)?.85f:1))&&(s.CoinMagnetRadius>0)==has(RuneId.Flight)&&(s.DamageReflectAmount>0)==has(RuneId.Moon);
            Check("Four-rune combination "+string.Join("+",combo.Select(r=>r.RuneId)),pass);count++;yield return null;
        }
        foreach(var r in s.RuneInventory.ActiveSlots.ToArray())if(r!=null)s.RuneInventory.TryDeactivate(r.RuneId);
        Check("All 70 combinations remove cleanly",count==70&&s.MaxHealth==baseHp&&Mathf.Approximately(s.AttackRangeMultiplier,baseRange)&&Mathf.Approximately(s.AttackCooldownMultiplier,baseCooldown));
    }
}
