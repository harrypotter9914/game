using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Babel.Runtime.Shop
{
    public sealed class ShopWindowView:MonoBehaviour
    {
        readonly List<Button> rows=new List<Button>();
        readonly List<TMP_Text> labels=new List<TMP_Text>();
        readonly List<Image> icons=new List<Image>();
        TMP_Text purse,title,description,notice,buyLabel;Image preview;Button buy;
        static readonly Color Ink=new Color(.032f,.043f,.072f,1f),Gold=new Color(.86f,.70f,.40f),Paper=new Color(.91f,.89f,.81f);
        public void Build(GameObject root,Action<int> select,Action purchase,Action close)
        {
            if(root==null)return;
            foreach(Transform child in root.transform)child.gameObject.SetActive(false);
            var rt=root.GetComponent<RectTransform>();rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);rt.pivot=new Vector2(.5f,.5f);rt.anchoredPosition=Vector2.zero;rt.sizeDelta=new Vector2(1280,800);rt.localScale=Vector3.one*.86f;
            var bg=root.GetComponent<Image>();if(bg==null)bg=root.AddComponent<Image>();bg.color=Color.clear;
            Box(root.transform,"Midnight vellum",new Vector2(78,91),new Vector2(1124,628),Ink);
            var inside=Box(root.transform,"Merchant interior",new Vector2(118,170),new Vector2(1044,510),Color.clear);
            Text(inside,"THE WAYFARER'S EXCHANGE",new Vector2(20,0),new Vector2(750,38),27,Gold);
            Text(inside,"Provisions for the ascent",new Vector2(22,38),new Vector2(740,28),18,Paper);
            purse=Text(inside,"",new Vector2(824,10),new Vector2(195,36),22,Gold);
            Box(inside,"Divider",new Vector2(20,72),new Vector2(994,1),Gold);
            Box(inside,"Illuminated column divider",new Vector2(526,88),new Vector2(1,353),new Color(.6f,.45f,.22f,.4f));
            Diamond(inside,new Vector2(523,70),6);Diamond(inside,new Vector2(523,441),6);
            for(int i=0;i<7;i++){
                int index=i;var card=Box(inside,"Wares "+i,new Vector2(20,90+i*51),new Vector2(478,46),new Color(.065f,.082f,.12f));
                var outline=card.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.58f,.44f,.24f,.4f);outline.effectDistance=new Vector2(1,-1);
                Diamond(card,new Vector2(462,20),5);
                var button=card.gameObject.AddComponent<Button>();button.onClick.AddListener(()=>select(index));rows.Add(button);
                var icon=Box(card,"Item icon",new Vector2(10,4),new Vector2(38,38),Color.white).GetComponent<Image>();icon.preserveAspect=true;icons.Add(icon);
                labels.Add(Text(card,"",new Vector2(64,4),new Vector2(385,40),20,Paper));
            }
            preview=Box(inside,"Selected relic",new Vector2(704,90),new Vector2(110,110),Color.white).GetComponent<Image>();preview.preserveAspect=true;
            title=Text(inside,"",new Vector2(560,205),new Vector2(440,38),27,Gold);
            description=Text(inside,"",new Vector2(560,253),new Vector2(425,116),22,Paper);
            var purchaseBox=Box(inside,"Purchase",new Vector2(560,380),new Vector2(424,48),Gold);buy=purchaseBox.gameObject.AddComponent<Button>();buy.onClick.AddListener(()=>purchase());
            buyLabel=Text(purchaseBox,"PURCHASE",new Vector2(12,5),new Vector2(400,38),23,Ink);buyLabel.alignment=TextAlignmentOptions.Center;
            notice=Text(inside,"",new Vector2(560,429),new Vector2(424,26),17,Gold);
            Bable.LiveInputHint.Attach(Text(inside,"ARROWS  Select    ENTER  Buy    E / ESC  Leave",new Vector2(20,457),new Vector2(830,30),17,Paper),"ARROWS  Select    ENTER  Buy    Q  Leave");
            var leave=Box(inside,"Leave",new Vector2(890,455),new Vector2(116,34),new Color(.15f,.18f,.22f));leave.gameObject.AddComponent<Button>().onClick.AddListener(()=>close());var leaveLabel=Text(leave,"LEAVE",new Vector2(8,3),new Vector2(100,28),17,Gold);leaveLabel.alignment=TextAlignmentOptions.Center;
            var frame=Box(root.transform,"Illuminated manuscript border",Vector2.zero,new Vector2(1280,800),Color.white).GetComponent<Image>();frame.sprite=Resources.Load<Sprite>("Bable/NewArt/MerchantIlluminatedFrame");frame.preserveAspect=true;frame.raycastTarget=false;
        }
        public void Render(IReadOnlyList<ShopItemDefinition> items,int selected,int gold,HashSet<ShopItemDefinition> sold,Func<ShopItemDefinition,int> price)
        {
            if(purse==null)return;purse.text=gold+"  GOLD";notice.text="";
            for(int i=0;i<rows.Count;i++){
                rows[i].gameObject.SetActive(i<items.Count);if(i>=items.Count)continue;var item=items[i];bool purchased=item.OneTimePurchase&&sold.Contains(item);
                rows[i].GetComponent<Image>().color=i==selected?new Color(.17f,.22f,.30f):new Color(.065f,.082f,.12f);
                rows[i].GetComponent<Outline>().effectColor=i==selected?Gold:new Color(.58f,.44f,.24f,.3f);
                labels[i].text=item.DisplayName+"\n<size=15><color=#C5AA73>"+(purchased?"OWNED":price(item)+" gold")+"</color></size>";
                icons[i].sprite=item.Icon;icons[i].enabled=item.Icon!=null;
            }
            if(items.Count==0){title.text="No wares today";preview.enabled=false;description.text="";buy.interactable=false;return;}
            var chosen=items[selected];bool owned=chosen.OneTimePurchase&&sold.Contains(chosen);title.text=chosen.DisplayName;
            description.text=chosen.Description+"\n\n<size=17><color=#C5AA73>"+(chosen.OneTimePurchase?"Permanent relic · one per journey":"Provision · consumed immediately")+"</color></size>";
            preview.sprite=chosen.Icon;preview.enabled=chosen.Icon!=null;buy.interactable=!owned;buyLabel.text=owned?"ALREADY OWNED":"PURCHASE  ·  "+price(chosen)+" GOLD";
        }
        public void Message(string value){if(notice!=null)notice.text=value;}
        static void Diamond(Transform parent,Vector2 pos,float size){var r=Box(parent,"Gold leaf lozenge",pos,new Vector2(size,size),Gold);r.localRotation=Quaternion.Euler(0,0,45);r.GetComponent<Image>().raycastTarget=false;}
        static RectTransform Box(Transform parent,string name,Vector2 pos,Vector2 size,Color color){var go=new GameObject(name,typeof(RectTransform),typeof(Image));var rt=go.GetComponent<RectTransform>();rt.SetParent(parent,false);rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(pos.x,-pos.y);rt.sizeDelta=size;go.GetComponent<Image>().color=color;return rt;}
        static TMP_Text Text(Transform parent,string text,Vector2 pos,Vector2 size,float fontSize,Color color){var go=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));var rt=go.GetComponent<RectTransform>();rt.SetParent(parent,false);rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(pos.x,-pos.y);rt.sizeDelta=size;var t=go.GetComponent<TextMeshProUGUI>();t.text=text;t.font=fontSize>=27?Bable.MenuTypography.Sdf:Resources.Load<TMP_FontAsset>("Bable/GuideFonts/CrimsonText-Regular SDF");t.fontSize=fontSize;t.color=color;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;return t;}
    }
}
