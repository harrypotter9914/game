using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using UnityEngine;
using TMPro;
using Bable;
using Babel.Runtime.Core;
using Babel.Runtime.Runes;
using Babel.Runtime.Combat;
using Babel.Runtime.World;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Characters.Enemies;

public static class BableNineteenthTests
{
    static readonly List<string> checks=new(),failures=new();
    static BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(string title,bool pass){checks.Add(title);if(!pass)failures.Add(title);File.WriteAllText("../reference/revision19/rune-tests.json",JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));}
    static void Set(object obj,string field,object value)=>obj.GetType().GetField(field,Private).SetValue(obj,value);
    public static void Run(){checks.Clear();failures.Clear();new GameObject("Rune specification audit").AddComponent<BableTestHost>().StartCoroutine(Audit());}
    static HealthComponent Enemy(string name,Vector2 pos){var g=new GameObject(name);g.transform.position=pos;g.AddComponent<BoxCollider2D>().size=Vector2.one*.4f;var h=g.AddComponent<HealthComponent>();Set(h,"alignment",TeamAlignment.Enemy);h.Configure(20,20);return h;}
    static GameObject Wall(Vector2 pos,Vector2 size){var g=new GameObject("Audit terrain");g.transform.position=pos;g.AddComponent<BoxCollider2D>().size=size;return g;}
    static void Only(GameSession s,RuneDefinition rune){foreach(var r in s.RuneInventory.ActiveSlots.ToArray())if(r!=null)s.RuneInventory.TryDeactivate(r.RuneId);if(rune!=null)s.RuneInventory.TryActivateToEmptySlot(rune);}
    static IEnumerator Audit()
    {
        yield return new WaitForSeconds(.8f);
        var s=GameSession.Instance;var ui=BableGameUI.Instance;ui.Resume();var p=Object.FindFirstObjectByType<PlayerController2D>();var body=p.GetComponent<Rigidbody2D>();var hp=p.GetComponent<HealthComponent>();
        foreach(var e in Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None))e.enabled=false;
        p.SetTestInput(0,0);p.enabled=false;body.simulated=false;p.transform.position=new Vector2(1000,100);Physics2D.SyncTransforms();
        var runes=Resources.LoadAll<RuneDefinition>("Bable/Runes").ToDictionary(r=>r.RuneId);s.RuneInventory.ResetInventory();
        Check("Eight canonical rune definitions",runes.Count==8);
        foreach(var id in new[]{RuneId.Moon,RuneId.Sun,RuneId.Rings,RuneId.Flight,RuneId.Star,RuneId.Romance,RuneId.Woman,RuneId.Harvest})
        {
            var r=runes[id];var g=new GameObject("Audit rune pickup "+id);var pickup=g.AddComponent<CollectiblePickup>();pickup.ConfigureRune(r);pickup.Interact(p.GetComponent<PlayerRuntimeState>());yield return new WaitForSecondsRealtime(.18f);
            Check(id+" pickup recovers matching rune and original English scroll",s.RuneInventory.IsCollected(id)&&ui.Mode=="scroll"&&string.Equals(ui.CurrentScrollId,"hoxi "+id,System.StringComparison.OrdinalIgnoreCase));
            Check(id+" is not auto-equipped",!s.RuneInventory.IsActive(id));
            var button=GameObject.Find("E  RUNE DETAILS");Check(id+" pickup offers details",button!=null);button.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();yield return new WaitForSecondsRealtime(.12f);
            var view=Object.FindFirstObjectByType<RuneRepositoryView>();Check(id+" details select acquired rune",view.DisplayedRuneAt(view.FocusedCell)==r);
            var effect=GameObject.Find("Rune description").GetComponent<TMP_Text>();var lore=GameObject.Find("Rune lore").GetComponent<TMP_Text>();effect.ForceMeshUpdate();lore.ForceMeshUpdate();
            Check(id+" complete English effect and lore fit",effect.text.Contains(r.EffectDescription)&&lore.text.Contains(r.Lore)&&!effect.isTextOverflowing&&!lore.isTextOverflowing&&!System.Text.RegularExpressions.Regex.IsMatch(effect.text+lore.text,"[\\u4e00-\\u9fff]"));
            if(id==RuneId.Sun)BableVerification.Capture("revision19/rune-sun.png");if(id==RuneId.Moon)BableVerification.Capture("revision19/rune-moon.png");ui.Resume();
        }
        Check("Acquisition order retained",s.RuneInventory.CollectedRunes.First().RuneId==RuneId.Moon&&s.RuneInventory.CollectedRunes.Last().RuneId==RuneId.Harvest);
        foreach(var id in new[]{RuneId.Star,RuneId.Sun,RuneId.Harvest,RuneId.Flight})s.RuneInventory.TryActivateToEmptySlot(runes[id]);
        Check("Four runes equip and fifth is rejected",s.RuneInventory.ActiveSlots.Count(r=>r!=null)==4&&!s.RuneInventory.TryActivateToEmptySlot(runes[RuneId.Romance]));
        var combat=p.GetComponent<PlayerCombatController>();var modify=typeof(PlayerCombatController).GetMethod("ApplyDamageModifiers",Private);int max=s.MaxHealth;
        Only(s,runes[RuneId.Romance]);Check("Romance adds health and reduces combat damage",s.MaxHealth==max+1&&(int)modify.Invoke(combat,new object[]{20})==17);Only(s,null);Check("Removing Romance restores health cap",s.MaxHealth==max);
        Only(s,runes[RuneId.Rings]);s.SetHealth(max,max);Check("Rings inactive at full health",Mathf.Approximately(s.DamageMultiplier,1));s.SetHealth(3,max);Check("Rings half-health tier",Mathf.Approximately(s.DamageMultiplier,1.5f));s.SetHealth(1,max);Check("Rings critical tier",Mathf.Approximately(s.DamageMultiplier,2));s.SetHealth(max,max);Check("Rings boost ends on healing",Mathf.Approximately(s.DamageMultiplier,1));
        Only(s,runes[RuneId.Star]);Check("Star changes actual attack recovery",Mathf.Approximately((float)typeof(PlayerCombatController).GetMethod("GetCooldownMultiplier",Private).Invoke(combat,null),1/1.4f));
        Only(s,runes[RuneId.Sun]);Check("Sun changes actual combat reach",Mathf.Approximately((float)typeof(PlayerCombatController).GetMethod("GetRangeMultiplier",Private).Invoke(combat,null),1.4f));
        Only(s,runes[RuneId.Harvest]);Check("Harvest applies 20 percent shop discount",Mathf.Approximately(s.ShopPriceMultiplier,.8f));
        var shop=Object.FindFirstObjectByType<Babel.Runtime.Shop.ShopMenuController>();var merchant=Object.FindFirstObjectByType<Babel.Runtime.Shop.ShopkeeperController>();var stock=(Babel.Runtime.Shop.ShopItemDefinition[])typeof(Babel.Runtime.Shop.ShopkeeperController).GetField("stock",Private).GetValue(merchant);var price=typeof(Babel.Runtime.Shop.ShopMenuController).GetMethod("GetPrice",Private);
        Check("Every stocked item's purchase price uses Harvest",stock.All(item=>(int)price.Invoke(shop,new object[]{item})==Mathf.Max(1,Mathf.RoundToInt(item.Price*.8f))));
        Only(s,null);Check("Harvest discount ends on removal",s.ShopPriceMultiplier==1);
        Only(s,runes[RuneId.Flight]);var coin=new GameObject("Audit magnet coin");coin.transform.position=p.transform.position+Vector3.left*3;coin.AddComponent<CollectiblePickup>().ConfigureGoldAmount(1);float before=Vector2.Distance(coin.transform.position,p.transform.position);yield return new WaitForSeconds(.15f);Check("Flight moves a nearby coin",Vector2.Distance(coin.transform.position,p.transform.position)<before-.2f);Object.Destroy(coin);
        var wall=Wall((Vector2)p.transform.position+Vector2.right*1.5f,new Vector2(.2f,4));coin=new GameObject("Audit blocked coin");coin.transform.position=p.transform.position+Vector3.right*3;coin.AddComponent<CollectiblePickup>().ConfigureGoldAmount(1);var old=coin.transform.position;Physics2D.SyncTransforms();yield return new WaitForSeconds(.15f);Check("Flight does not pull through terrain",Vector2.Distance(old,coin.transform.position)<.01f);Object.Destroy(coin);
        Only(s,runes[RuneId.Moon]);var near=Enemy("Near enemy",(Vector2)p.transform.position+Vector2.left);near.gameObject.AddComponent<CircleCollider2D>().radius=.15f;var above=Enemy("Second enemy",(Vector2)p.transform.position+Vector2.up);var blocked=Enemy("Walled enemy",(Vector2)p.transform.position+Vector2.right*2);var far=Enemy("Distant attacker",(Vector2)p.transform.position+Vector2.left*8);Physics2D.SyncTransforms();
        hp.Configure(max,max);hp.ApplyDamage(4,new DamageInfo(4,p.transform.position,Vector2.zero,far.gameObject,TeamAlignment.Enemy));Check("Moon reflects half damage to every nearby enemy once",near.CurrentHealth==18&&above.CurrentHealth==18);Check("Moon excludes distant attacker and walled enemy",far.CurrentHealth==20&&blocked.CurrentHealth==20);
        yield return new WaitForSecondsRealtime(.3f);hp.Configure(max,max);hp.ApplyDamage(2,new DamageInfo(2,p.transform.position,Vector2.zero,null,TeamAlignment.Enemy));Check("Moon scales with received damage and supports missing projectile owner",near.CurrentHealth==17&&above.CurrentHealth==17);hp.ReceiveDamage(new DamageInfo(2,p.transform.position,Vector2.zero,null,TeamAlignment.Enemy));Check("Invulnerable hit does not trigger another reflection",near.CurrentHealth==17);
        foreach(var h in new[]{near,above,blocked,far})Object.Destroy(h.gameObject);Object.Destroy(wall);Only(s,runes[RuneId.Woman]);var ceiling=Wall((Vector2)p.transform.position+Vector2.up*3,new Vector2(6,.5f));Physics2D.SyncTransforms();var deathPosition=p.transform.position;hp.Configure(max,1);hp.ApplyDamage(1);Check("Woman restores half health without changing floor",hp.CurrentHealth==Mathf.CeilToInt(max*.5f)&&Vector2.Distance(p.transform.position,deathPosition)<.001f&&ui.Mode=="play");Check("Woman consumed once and cannot be re-equipped",s.RevivalConsumed&&s.RuneInventory.IsSpent(runes[RuneId.Woman])&&!s.RuneInventory.TryActivateToEmptySlot(runes[RuneId.Woman])&&!s.RuneInventory.EquipAt(runes[RuneId.Woman],0)&&!s.ConsumeReviveIfAvailable());Object.Destroy(ceiling);
        ui.Runes();yield return new WaitForSecondsRealtime(.2f);var repository=Object.FindFirstObjectByType<RuneRepositoryView>();for(int i=0;i<12;i++)if(repository.DisplayedRuneAt(i)==runes[RuneId.Woman])repository.SelectCell(i);Check("Spent rune status visible",GameObject.Find("Repository status").GetComponent<TMP_Text>().text.Contains("SPENT"));BableVerification.Capture("revision19/rune-spent.png");
        Debug.Log("Revision19 rune audit complete: "+checks.Count+" checks; "+failures.Count+" failures");
    }
}

