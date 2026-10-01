using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Babel.Runtime.Core;
using Babel.Runtime.Runes;
using Babel.Runtime.World;
using Babel.Runtime.Characters.Player;

namespace Bable
{
    public sealed partial class BableGameUI : MonoBehaviour
    {
        public static BableGameUI Instance { get; private set; }
        // Retained for old scene authoring tools; the scene now determines startup.
        [HideInInspector] public bool startAtMenu = true;
        public bool IsTitleScene => gameObject.scene.name == "MainMenu";
        public bool Ready => canvas != null;
        public bool CinematicActive => TowerDialogue.StoryActive ||
            FindFirstObjectByType<ScriptedBridgeCollapse>()?.IsRunning==true ||
            FindFirstObjectByType<WeaponAwakening>()?.IsRunning==true ||
            FindFirstObjectByType<PrincessRescue>()?.IsRunning==true;
        bool loadingScene;
        public string Mode { get; private set; } = "play";
        Canvas canvas;
        RectTransform overlay;
        RectTransform gameplayHud;
        public bool GameplayHudVisible => gameplayHud != null && gameplayHud.gameObject.activeInHierarchy;
        public int ActiveMenuOrder => overlay != null ? overlay.GetComponent<Canvas>().sortingOrder : -1;
        Font font;
        TMPro.TMP_Text objective;
        VotiveHud vitals;
        GameFlowController flow;
        GameSession session;
        
        RuneDefinition selectedRune;
        int selectedSlot;
        public string CurrentScrollId {get;private set;}
        RuneDefinition pickupRune;
        RenderTexture mapTexture;
        Camera mapCamera;
        readonly System.Collections.Generic.Queue<string[]> pickupPages=new();
        public void QueuePickup(string art,string title,string text){pickupPages.Enqueue(new[]{art,title,text});}
        void Awake() { Instance = this; }
        void OnDestroy(){if(Instance==this)Instance=null;}
        IEnumerator Start()
        {
            yield return null;
            if(!IsTitleScene)
                while(GameSession.Instance==null||FindFirstObjectByType<GameFlowController>()==null)yield return null;
            flow = FindFirstObjectByType<GameFlowController>(); session = GameSession.Instance;
            font = MenuTypography.Font;
            var go = new GameObject("Bable Interface", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 50;
            var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = .5f;
            if (FindFirstObjectByType<EventSystem>() == null) new GameObject("Event System", typeof(EventSystem), typeof(StandaloneInputModule));
            // The title scene contains no player, game session, world, or HUD.
            if(IsTitleScene){Time.timeScale=1;MainMenu();yield break;}
            foreach(var legacy in FindObjectsByType<TMPro.TextMeshPro>(FindObjectsSortMode.None))if(legacy.name=="THE FOREST APPROACH"||legacy.name=="THE PROPHECY"||legacy.name=="THE BURIED LABYRINTH"||legacy.name=="THE LAST SUPPER"||legacy.name=="NERO'S CROWN")legacy.gameObject.SetActive(false);
            gameplayHud = Rect(canvas.transform,"Gameplay HUD",Vector2.zero,new Vector2(1600,900));
            gameplayHud.gameObject.SetActive(false);
            var hudCanvas=gameplayHud.gameObject.AddComponent<Canvas>();hudCanvas.overrideSorting=true;hudCanvas.sortingOrder=40;
            vitals = gameplayHud.gameObject.AddComponent<VotiveHud>();
            objective = Label(gameplayHud, "", new Vector2(515, 345), new Vector2(500, 45), 22);
            objective.gameObject.AddComponent<Shadow>().effectDistance=new Vector2(2,-2);
            gameplayHud.gameObject.AddComponent<BossHealthHud>();
            gameplayHud.gameObject.AddComponent<PurseHud>();
            gameObject.AddComponent<JourneyTutorial>();
            gameObject.AddComponent<FracturedStoneGuide>();
            var atlas=ExploredAtlas.Current;
            Resume();BableAudio.Music("tower");
        }
        void Update()
        {
            if(loadingScene||TowerLoading.Busy||canvas==null)return;
            UpdateSettingsDevice();
            if(HandleMenuBack())return;
            if(IsTitleScene){
                if(Mode=="main"&&Bable.GameInput.Down(Bable.GameAction.Submit)&&EventSystem.current.currentSelectedGameObject==null)Begin();
                else if(Mode=="practice"&&Bable.GameInput.Down(Bable.GameAction.Pause))MainMenu();
                return;
            }
            if (session == null || canvas == null) return;
            var p = FindFirstObjectByType<PlayerController2D>();
            bool actionScene=TowerDialogue.StoryActive||p!=null&&p.GetComponent<WeaponAwakening>()?.IsRunning==true||FindFirstObjectByType<ScriptedBridgeCollapse>()?.IsRunning==true;
            if(Mode=="play"&&!session.IsPaused&&!actionScene&&pickupPages.Count>0){var page=pickupPages.Dequeue();ShowPickup(page[0],page[1],page[2]);}
            if(Mode=="scroll"&&Bable.GameInput.Down(Bable.GameAction.Back)){Resume();return;}
            if(Mode=="scroll"&&pickupRune!=null&&Bable.GameInput.Down(Bable.GameAction.Interact)){OpenPickupRune();return;}
            string area = FindFirstObjectByType<RuneCombatLab>()!=null?"RUNE & COMBAT LAB":p == null ? "" : FindFirstObjectByType<BossPractice>()!=null ? "BOSS PRACTICE" : p.transform.position.x<0 && p.transform.position.y>65 ? "DAWN BEYOND BABEL" : p.transform.position.y>80 ? "NERO'S CROWN" : p.transform.position.x>390 && p.transform.position.y>10 ? "THE LAST SUPPER" : p.transform.position.y<-55 ? "BURIED LABYRINTH" : p.transform.position.x<155 && p.transform.position.y>-12 ? "FOREST APPROACH" : "THE PROPHECY";
            objective.text = area; objective.alignment=TMPro.TextAlignmentOptions.MidlineRight;
            bool cinematic=actionScene||FindFirstObjectByType<PrincessRescue>()?.IsRunning==true;
            if(cinematic){if(Bable.GameInput.Down(Bable.GameAction.Pause)){if(Mode=="pause")Resume();else if(Mode=="play")Pause();}return;}
            var trading=FindFirstObjectByType<Babel.Runtime.Shop.ShopMenuController>();
            if(trading!=null&&(trading.IsOpen||trading.ConsumedToggleThisFrame))return;
            if (Bable.GameInput.Down(Bable.GameAction.Pause))
            {
                if (Mode == "pause" || Mode == "runes" || Mode == "scroll" || Mode == "map" || Mode=="journal" || Mode=="practice" || Mode=="tools") Resume();
                else if (Mode == "play" && !session.IsPaused) Pause();
            }
            if (Bable.GameInput.Down(Bable.GameAction.Runes)) { if (Mode == "runes") Resume(); else if (Mode == "play" && !session.IsPaused) Runes(); }
            if (Bable.GameInput.Down(Bable.GameAction.Map)) { if (Mode == "map") Resume(); else if(Mode=="play" && !session.IsPaused) Map(); }
            if ((Mode == "main" || Mode == "death" || Mode == "victory") && Bable.GameInput.Down(Bable.GameAction.Submit)&&EventSystem.current.currentSelectedGameObject==null)
            { if (Mode == "main") Begin(); else if (Mode == "death") Respawn(); else Restart(); }
            if (p != null && p.transform.position.y < -110 && Mode == "play") p.GetComponent<Babel.Runtime.Combat.HealthComponent>().ApplyDamage(999);
        }
        // Visibility belongs to the whole gameplay layer, not the dynamically rebuilt vitals.
        // Applying it synchronously on transitions prevents a one-frame flash before Update.
        void SyncGameplayHud()
        {
            if(gameplayHud==null)return;
            bool visible=Mode=="play"&&session!=null&&!session.IsPaused&&!TowerDialogue.StoryActive;
            var rescue=FindFirstObjectByType<PrincessRescue>();
            if(rescue!=null&&rescue.IsRunning)visible=false;
            gameplayHud.gameObject.SetActive(visible);
        }
        void LateUpdate(){SyncGameplayHud();}
        static int MenuOrder(string mode)=>mode=="main"?400:mode=="death"||mode=="victory"?500:mode=="pause"?300:200;
        void Panel(string mode, string title, string art = null)
        {
            if (overlay != null) {overlay.gameObject.SetActive(false);Destroy(overlay.gameObject);}
            EventSystem.current?.SetSelectedGameObject(null);
            Mode = mode;if(flow!=null)flow.SetPaused(true);SyncGameplayHud();
            overlay = Rect(canvas.transform, mode, Vector2.zero, new Vector2(1600,900));
            overlay.anchorMin=Vector2.zero;overlay.anchorMax=Vector2.one;overlay.offsetMin=overlay.offsetMax=Vector2.zero;
            var menuCanvas=overlay.gameObject.AddComponent<Canvas>();menuCanvas.overrideSorting=true;menuCanvas.sortingOrder=MenuOrder(mode);overlay.gameObject.AddComponent<GraphicRaycaster>();
            var shade = overlay.gameObject.AddComponent<Image>(); shade.color = new Color(.025f,.021f,.035f,mode=="pause"?.28f:.96f);
            if (art != null) {
                var background=Picture(overlay, art, Vector2.zero, new Vector2(1600,900));
                if(mode=="main"||mode=="pause"||mode=="death"||mode=="runes")background.gameObject.AddComponent<FullBleedMenuArt>().Fit();
            }
            var titleLabel=Label(overlay, title, new Vector2(0,292), new Vector2(1300,100), 54);titleLabel.font=MenuTypography.Sdf;titleLabel.enableAutoSizing=true;titleLabel.fontSizeMin=28;titleLabel.fontSizeMax=54;
        }
        public void MainMenu()
        {
            if(!IsTitleScene){Load("MainMenu");return;}
            Panel("main", "", "NewArt/Release53/MenuTitle"); BableAudio.Music("mainscreen");
            Picture(overlay,"NewArt/Release53/BabelLogo",new Vector2(-425,225),new Vector2(600,290));
            if(BuildFlavor.Practice){
                Label(overlay,"PRACTICE EDITION",new Vector2(-425,60),new Vector2(620,40),23);
                Button(overlay,"TRIALS OF THE GUARDIANS",new Vector2(-425,-25),Practice,560,62);
                Button(overlay,"RUNE & COMBAT LAB",new Vector2(-425,-115),OpenLab,560,62);
                Button(overlay,"SETTINGS",new Vector2(-425,-205),OpenSettings,560,62);
                Button(overlay,"QUIT GAME",new Vector2(-425,-295),Quit,560,62);
                return;
            }
            bool saved=CampaignStore.HasSave;
            string[] captions=saved?new[]{"CONTINUE JOURNEY","NEW JOURNEY","SETTINGS","QUIT GAME"}:new[]{"NEW JOURNEY","SETTINGS","QUIT GAME"};
            UnityEngine.Events.UnityAction[] actions=saved?new UnityEngine.Events.UnityAction[]{ContinueJourney,Begin,OpenSettings,Quit}:new UnityEngine.Events.UnityAction[]{Begin,OpenSettings,Quit};
            for(int i=0;i<captions.Length;i++)Button(overlay,captions[i],new Vector2(-425,(saved?35:0)-i*100),actions[i],520,62);
            Label(overlay,CampaignStore.Status,new Vector2(-425,-365),new Vector2(650,48),18);
        }
        void ContinueJourney(){if(CampaignStore.RequestContinue())Load("Gameplay_Main");else MainMenu();}
        public void Begin()
        {
            if(BuildFlavor.Practice){Practice();return;}
            if(!IsTitleScene){Resume();return;}
            if(Mode=="prologue")return;
            if(CampaignStore.HasAnySave){Confirm("BEGIN A NEW JOURNEY?","Your current journey will be replaced. A backup is kept.",StartNewJourney,MainMenu);return;}
            StartNewJourney();
        }
        void StartNewJourney(){
            try{CampaignStore.RequestNew();}catch(System.Exception e){Notice("Could not preserve your previous save. "+e.Message,MainMenu);return;}
            Panel("prologue","");overlay.gameObject.AddComponent<TowerPrologue>().Build(()=>Load("Gameplay_Main"));
        }
        public void Resume() { GameInput.ConsumeTransition(); if(loadingScene)return;if(IsTitleScene){MainMenu();return;}if (overlay != null) {overlay.gameObject.SetActive(false);Destroy(overlay.gameObject);} if(mapCamera!=null){mapCamera.targetTexture=null;Destroy(mapCamera.gameObject);mapCamera=null;} if(mapTexture!=null){mapTexture.Release();Destroy(mapTexture);mapTexture=null;} Mode = "play"; flow.SetPaused(false);SyncGameplayHud(); }
        public void Map()
        {
            Panel("map","TOWER ATLAS");
            var texture=ExploredAtlas.Current?.Draw();
            var size=new Vector2(1120,610);if(texture!=null){float ratio=texture.width/(float)texture.height;size=ratio>size.x/size.y?new Vector2(size.x,size.x/ratio):new Vector2(size.y*ratio,size.y);}
            var view=Rect(overlay,"Explored passages only",new Vector2(0,-5),size).gameObject.AddComponent<RawImage>();view.texture=texture;view.raycastTarget=false;
            Label(overlay,"Gold: your position     Unexplored passages remain hidden",new Vector2(0,-345),new Vector2(1050,34),20);
            Button(overlay,"RETURN",new Vector2(0,-400),Resume,320,45);
        }
        public void Pause()
        {
            Panel("pause","PAUSED","NewArt/Release53/MenuPause");

            OriginalButton("CONTINUE",new Vector2(-260,220),Resume,"nos",new Rect(.218f,.642f,.31f,.134f),true);
            Button(overlay,"MAIN MENU",new Vector2(260,220),MainMenu,420,65);
            Button(overlay,"CHECKPOINT",new Vector2(-260,105),Respawn,420,65);
            if(CinematicActive)overlay.Find("CHECKPOINT").GetComponent<Button>().interactable=false;
            Button(overlay,"JOURNAL",new Vector2(260,105),Journal,420,65);
            if(CampaignStore.IsPractice){
                Button(overlay,"BOSS PRACTICE",new Vector2(-260,-10),Practice,420,65);
                Button(overlay,"RUNE & COMBAT LAB",new Vector2(260,-10),OpenLab,420,65);
            }
            if(CampaignStore.IsPractice)Button(overlay,"CHAMBER TOOLS",new Vector2(-260,-125),ChamberTools,420,65);
            else Button(overlay,"SAVE JOURNEY",new Vector2(-260,-10),()=>Notice(CampaignStore.IsCampaign?(CampaignStore.SaveNow()?CampaignStore.Status:"Unable to save now. Finish the current story sequence first."):"Practice progress is separate from your journey.",Pause),420,65);
            Button(overlay,"SETTINGS",new Vector2(260,CampaignStore.IsPractice?-125:-10),OpenSettings,420,65);
            OriginalButton("EXIT",new Vector2(0,CampaignStore.IsPractice?-255:-140),Quit,"nos",new Rect(.221f,.49f,.146f,.132f),true);
        }
        public void Death() { Panel("death","", "NewArt/Release53/MenuDeath"); BableAudio.Music("dead");Label(overlay,"THE FLAME ENDURES",new Vector2(385,200),new Vector2(680,80),42);Button(overlay,"RISE AT THE CHECKPOINT",new Vector2(385,40),Respawn,590,70);Button(overlay,"SETTINGS",new Vector2(385,-65),OpenSettings,590,70);Button(overlay,"MAIN MENU",new Vector2(385,-170),MainMenu,590,70);Button(overlay,"EXIT",new Vector2(385,-275),Quit,590,70); }
        public void Respawn() { TowerLoading.Respawn(); BableAudio.Music("tower"); }
        public void Victory() { Panel("victory","THE TOWER FALLS SILENT"); BableAudio.Music("end"); Label(overlay,"Nero's reign has ended.\nBeyond the tower, a new dawn waits.",new Vector2(0,60),new Vector2(1100,160),32); Button(overlay,"BEGIN AGAIN",new Vector2(0,-200),Restart);Button(overlay,"MAIN MENU",new Vector2(0,-290),MainMenu,320,52); }
        void Restart() { if(CampaignStore.IsPractice){Load(SceneManager.GetActiveScene().name);return;}MainMenu(); }
        void Load(string scene){
            if(loadingScene||!BuildFlavor.AllowsScene(scene))return;
            if(!Application.CanStreamedLevelBeLoaded(scene)){Debug.LogError("Scene is missing from Build Settings: "+scene);return;}
            if(TowerLoading.Busy)return;
            if(!CampaignStore.BeforeLeave()){Notice("Your journey could not be saved. Finish the story sequence or check your save folder before leaving.",Pause);return;}
            loadingScene=true;TowerLoading.Load(scene);
        }
        public void ReturnToCampaign(){if(BuildFlavor.Practice){MainMenu();return;}if(CampaignStore.IsCampaign){Resume();return;}if(CampaignStore.RequestContinue())Load("Gameplay_Main");else Load("MainMenu");}
        public void OpenLab(){if(BuildFlavor.CanOpenPractice)Load("Rune_Combat_Lab");}
        public void Practice()
        {
            if(!BuildFlavor.CanOpenPractice)return;
            Panel("practice","TRIALS OF THE GUARDIANS");
            string[] names=TowerLore.GuardianTitles;
            for(int i=0;i<5;i++){int index=i+1;Button(overlay,names[i],new Vector2(0,210-i*83),()=>Load("Boss_Test_"+index),660,64);}
            Button(overlay,"MAIN MENU",new Vector2(-330,-330),MainMenu,300,52);
            Button(overlay,"RETURN",new Vector2(330,-330),Resume,300,52);
            Button(overlay,"RUNE & COMBAT LAB",new Vector2(0,-240),OpenLab,660,56);
        }
        public void Journal()
        {
            Panel("journal","RECOVERED MEMORIES");var journal=FindFirstObjectByType<ScrollJournal>();
            for(int i=0;i<ScrollLibrary.Entries.Length;i++){var e=ScrollLibrary.Entries[i];bool found=journal!=null&&journal.recovered.Contains(e.id);Button(overlay,found?e.title:"UNRECOVERED MEMORY",new Vector2(i<5?-370:370,200-(i%5)*85),()=>{if(found)ShowScroll(e.id);},690,58);}
            Button(overlay,"RETURN",new Vector2(0,-360),Resume,320,48);
        }
        void Quit() {
            if(!CampaignStore.BeforeLeave()){Notice("Your journey could not be saved. Finish the story sequence before leaving.",Pause);return;}
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        public void Scroll(string art)
        {
            if (Mode != "play") return;
            ShowScroll(art);
        }
        void ShowScroll(string id)
        {
            if(!ScrollLibrary.TryGet(id,out var e))return;CurrentScrollId=id;
            ShowPickup(id,e.title,e.text);
        }
        void ShowPickup(string art,string title,string text)
        {
            pickupRune=null;
            foreach(var rune in session.RuneInventory.CollectedRunes)
                if(string.Equals(art,"hoxi "+rune.RuneId,System.StringComparison.OrdinalIgnoreCase)){pickupRune=rune;break;}
            CurrentScrollId=art;Panel("scroll","");overlay.GetComponent<Image>().color=new Color(.015f,.012f,.025f,.7f);
            bool original=pickupRune==null&&!string.IsNullOrEmpty(art)&&Sprite(art)!=null;
            Picture(overlay,original?art:"NewArt/Release51/Panel",new Vector2(0,30),new Vector2(920,675));
            if(!original){
                var heading=Label(overlay,title,new Vector2(0,170),new Vector2(750,85),34);heading.color=new Color(1,.91f,.68f);
                var body=Label(overlay,text??"",new Vector2(0,10),new Vector2(720,225),26);body.color=new Color(.93f,.89f,.78f);
            }
            if(pickupRune!=null){
                var runeIcon=Picture(overlay,"NewArt/Release51/Rune"+pickupRune.RuneId,new Vector2(0,270),new Vector2(105,105));
                Button(overlay,"E  RUNE DETAILS",new Vector2(-180,-365),OpenPickupRune,320,52);
                Button(overlay,"Q  CLOSE",new Vector2(180,-365),Resume,320,52);
            } else Button(overlay,"Q  CLOSE",new Vector2(0,-365),Resume,320,52);
        }
        void OpenPickupRune()
        {
            var rune=pickupRune;Runes();
            var view=overlay.GetComponent<RuneRepositoryView>();
            for(int i=0;i<12;i++)if(view.DisplayedRuneAt(i)==rune){view.SelectCell(i);break;}
        }
        public void Runes()
        {
            Panel("runes","","NewArt/Release53/RuneBackground");
            overlay.gameObject.AddComponent<RuneRepositoryView>().Build(session.RuneInventory,Resume);
        }
        static Sprite Sprite(string name) => Resources.Load<Sprite>(name.StartsWith("NewArt/")?"Bable/"+name:"Bable/images/" + name);
        RectTransform Rect(Transform parent,string name,Vector2 position,Vector2 size)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent,false); r.anchorMin=r.anchorMax=new Vector2(.5f,.5f); r.anchoredPosition=position; r.sizeDelta=size; return r;
        }
        Image Picture(Transform parent,string name,Vector2 pos,Vector2 size) { var im=Rect(parent,name,pos,size).gameObject.AddComponent<Image>(); im.sprite=Sprite(name); im.preserveAspect=true; im.raycastTarget=false; return im; }
        TMPro.TMP_Text Label(Transform parent,string value,Vector2 pos,Vector2 size,int fontSize)
        {
            var t=Rect(parent,"Label",pos,size).gameObject.AddComponent<TMPro.TextMeshProUGUI>();t.text=value;t.font=MenuTypography.Sdf;t.fontSize=fontSize;t.alignment=TMPro.TextAlignmentOptions.Center;t.color=new Color(.93f,.86f,.67f);t.raycastTarget=false;
            if(value.StartsWith("Q  ")||value.StartsWith("E  "))LiveInputHint.Attach(t,value);return t;
        }
        void Button(Transform parent,string text,Vector2 position,UnityEngine.Events.UnityAction action,float width=520,float height=65)
        {
            var r=Rect(parent,text,position,new Vector2(width,height)); var im=r.gameObject.AddComponent<Image>(); im.color=Color.clear;
            var button=r.gameObject.AddComponent<Button>(); button.targetGraphic=im;button.transition=Selectable.Transition.None;button.onClick.AddListener(action);
            var visual=Rect(r,"Lettering and ornament",Vector2.zero,new Vector2(width,height));
            var frame=Rect(visual,"Selection frame",Vector2.zero,new Vector2(width,height));
            if(width>180)for(int side=-1;side<=1;side+=2){var tip=Picture(frame,"NewArt/Release51/RuneStar",new Vector2(side*(width/2-26),0),new Vector2(52,52));tip.color=new Color(1f,.72f,.22f,1);var outline=tip.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.66f,.35f,.07f,1);outline.effectDistance=new Vector2(1,-1);outline.useGraphicAlpha=true;}
            if(width>180)for(int side=-1;side<=1;side+=2){var line=Rect(frame,"Gold rule",new Vector2(0,side*(height/2-6)),new Vector2(width-92,3)).gameObject.AddComponent<Image>();line.color=new Color(1f,.72f,.22f,1);line.raycastTarget=false;}
            var group=frame.gameObject.AddComponent<CanvasGroup>();group.alpha=0;group.blocksRaycasts=false;
            var label=Rect(visual,"Menu lettering",Vector2.zero,new Vector2(Mathf.Max(40,width-(width>180?96:12)),height-12)).gameObject.AddComponent<TMPro.TextMeshProUGUI>();
            label.text=text;label.font=MenuTypography.Sdf;label.fontSize=26;label.fontStyle=TMPro.FontStyles.Normal;label.alignment=TMPro.TextAlignmentOptions.Center;
            label.enableAutoSizing=true;label.fontSizeMin=17;label.fontSizeMax=26;label.color=new Color(1,.94f,.77f);label.raycastTarget=false;
            if(text.StartsWith("E  ")||text.StartsWith("Q  "))LiveInputHint.Attach(label,text);
            var motion=r.gameObject.AddComponent<ManuscriptMenuButton>();motion.lettering=visual;motion.ornament=group;
        }
        void MenuBacking(Vector2 pos,Vector2 size){var r=Rect(overlay,"Ink vellum",pos,size);var i=r.gameObject.AddComponent<IlluminatedMenuPanel>();i.color=new Color(.026f,.023f,.04f,Mode=="pause"?.28f:.60f);i.raycastTarget=false;if(Mode!="pause")Picture(r,"NewArt/Release51/Panel",Vector2.zero,size);}
        void OriginalButton(string name,Vector2 pos,UnityEngine.Events.UnityAction action,string art,Rect uv,bool red,bool gray=false)
        {
            Button(overlay,name,pos,action,Mode=="pause"?420:480,70);

        }
        void HitArea(Transform parent,Vector2 position,Vector2 size,UnityEngine.Events.UnityAction action)
        {
            var r=Rect(parent,"Original art button",position,size);var im=r.gameObject.AddComponent<Image>();im.color=Color.clear;var button=r.gameObject.AddComponent<Button>();button.targetGraphic=im;button.onClick.AddListener(action);
        }
    }
}

