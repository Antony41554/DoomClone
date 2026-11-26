using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Ukubola.Audio;
using Ukubola.Combat;
using Ukubola.Enemies;
using Ukubola.Player;
using Ukubola.World;
using Ukubola.Campaign;
using Ukubola.Persistence;

namespace Ukubola.UI
{
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class MenuFlow : MonoBehaviour
    {
        public const string TitlePath="Assets/_UKUBOLA/Scenes/Menus/MainMenu.unity";
        public const string GamePath="Assets/_UKUBOLA/Scenes/Tests/MenuLab.unity";
        [SerializeField] private bool titleScene;
        [SerializeField] private bool persistenceEnabled;
        public bool PersistenceEnabled=>persistenceEnabled;
        public void EnablePersistence()=>persistenceEnabled=true;
        private bool initializing, restoreFailed, initialInput;
        private int initialFrames;
        private string saveMessage="";
        [SerializeField] private string routeTitle, routeGame, missionLabel;
        public string RouteTitle=>string.IsNullOrEmpty(routeTitle)?TitlePath:routeTitle;
        public string RouteGame=>string.IsNullOrEmpty(routeGame)?GamePath:routeGame;
        public string MissionLabel=>missionLabel;
        public void ConfigureRoute(string title,string game,string label){routeTitle=title;routeGame=game;missionLabel=label;}
        [SerializeField] private ClassicHud hud;
        [SerializeField] private AudioSource titleMusic;
        [SerializeField] private Font font;
        private MenuScreen screen;
        private Ukubola.Release.ReleasePresentation release;
        private CampaignStage campaign;
        private MenuPreferences prefs;
        private MenuPage page, returnPage, confirmationReturn;
        private Health hp;
        private PlayerInputReader input;
        private PlayerMotor motor;
        private NavMeshAgent[] agents=Array.Empty<NavMeshAgent>();
        private bool[] wasStopped=Array.Empty<bool>();
        private bool frozen, ownsClock, inputWasEnabled, closing;
        private string error="";
        public bool TitleScene=>titleScene;
        public ClassicHud Hud=>hud;
        public AudioSource TitleMusic=>titleMusic;
        public Font Typeface=>font;
        public MenuPage Page=>page;
        public void Configure(bool title,ClassicHud gameplay,AudioSource music,Font typeface)
        {titleScene=title;hud=gameplay;titleMusic=music;font=typeface;}
        private void Start()
        {
            if(!font || (titleScene?!titleMusic:!hud)) {Debug.LogError("[UKUBOLA P10] Missing menu references.",this);enabled=false;return;}
            Time.timeScale=1;AudioListener.pause=false;ownsClock=true;
            prefs=MenuPreferences.Load();release=GetComponent<Ukubola.Release.ReleasePresentation>();screen=new MenuScreen(font,release&&release.enabled?release.Art:null);campaign=GetComponent<CampaignStage>();
            if(titleScene){if(campaign)campaign.Enter(null);Unlock();ApplySettings();titleMusic.Play();if(persistenceEnabled&&!string.IsNullOrEmpty(SaveStore.Notice)){error=SaveStore.Notice;SaveStore.Notice=null;}Show(MenuPage.Title);}
            else
            {
                hp=hud.World.Source.Player;input=hp.GetComponent<PlayerInputReader>();motor=hp.GetComponent<PlayerMotor>();
                if(!input||!motor){Debug.LogError("[UKUBOLA P10] Missing player controls.",this);enabled=false;return;}
                hud.DiagnosticsEnabled=false;hud.Audio.DiagnosticsEnabled=false;
                agents=hud.World.Source.Enemies.Select(e=>e.GetComponent<NavMeshAgent>()).ToArray();wasStopped=new bool[agents.Length];
                if(persistenceEnabled && SaveStore.Pending!=null)
                {
                    // Preserve the entry checkpoint for retry; current state is applied after all Start methods.
                    try{SaveWorld.Check(SaveStore.Pending,campaign,hud);CampaignRun.Set(SaveStore.Pending.Entry());SaveWorld.PrepareNavigation(hud);}
                    catch(Exception ex){AbortRestore(ex);return;}
                }
                if(campaign)campaign.Enter(hud);
                ApplySettings();page=MenuPage.Playing;screen.Hide();Unlock();
                if(persistenceEnabled&&(SaveStore.Pending!=null||SaveStore.AutoSavePending))
                {initializing=true;initialFrames=1;initialInput=input.enabled;input.enabled=false;Time.timeScale=0;AudioListener.pause=true;Show(MenuPage.Loading);}
            }
        }
        private void Update()
        {
            if(screen==null||closing||initializing||restoreFailed)return;
            if(page==MenuPage.Playing)
            {
                var state=MenuRules.GameplayPage(hp.IsDead,hud.Stats.Completed);
                if(state!=MenuPage.Playing){Freeze(false);Show(state);return;}
            }
            var k=Keyboard.current;if(k==null||!Application.isFocused||!k.escapeKey.wasPressedThisFrame)return;
            switch(page)
            {
                case MenuPage.Playing: Freeze(true);Show(MenuPage.Paused);break;
                case MenuPage.Paused: Resume();break;
                case MenuPage.Options: Show(returnPage);break;
                case MenuPage.ConfirmRestart:case MenuPage.ConfirmTitle:case MenuPage.ConfirmExit:case MenuPage.ConfirmNew:case MenuPage.ConfirmLoad:Show(confirmationReturn);break;
            }
        }
        private void LateUpdate()
        {
            if(!initializing||closing)return;if(initialFrames-- > 0)return;initializing=false;
            var pending=SaveStore.Pending;bool auto=SaveStore.AutoSavePending;SaveStore.Pending=null;SaveStore.AutoSavePending=false;
            try{if(pending!=null)SaveWorld.Apply(pending,campaign,hud);}
            catch(Exception ex){AbortRestore(ex);return;}
            if(auto&&pending==null)WriteSave(false);
            Time.timeScale=1;AudioListener.pause=false;input.enabled=initialInput;page=MenuPage.Playing;screen.Hide();Unlock();
        }
        private void AbortRestore(Exception ex)
        {
            SaveStore.Pending=null;SaveStore.AutoSavePending=false;SaveStore.Notice="LOAD FAILED - SAVE PRESERVED";
            restoreFailed=true;initializing=false;Time.timeScale=0;AudioListener.pause=true;if(input)input.enabled=false;Unlock();
            Debug.LogError("[UKUBOLA P14] RESTORE FAILED: "+ex);Load(RouteTitle);
        }
        private void WriteSave(bool redraw)
        {
            try
            {var data=SaveWorld.Capture(campaign,hud);SaveStore.Write(SaveStore.FilePath,data);saveMessage="GAME SAVED";Debug.Log("[UKUBOLA P14] SAVE OK | "+SaveStore.FilePath);}
            catch(Exception ex){error="SAVE FAILED - PREVIOUS SAVE PRESERVED";Debug.LogWarning("[UKUBOLA P14] SAVE FAILED: "+ex);}
            if(redraw)Show(MenuPage.Paused);
        }
        private void NewCampaign()
        {if(persistenceEnabled&&SaveStore.Exists(SaveStore.FilePath))Confirm(MenuPage.ConfirmNew);else Load(RouteGame,false,null,persistenceEnabled);}
        private void ContinueGame()
        {
            if(!SaveStore.TryRead(SaveStore.FilePath,out var data,out var backup,out var reason)){error="NO VALID SAVE";Debug.LogWarning("[UKUBOLA P14] "+reason);Show(page);return;}
            if(backup)Debug.LogWarning("[UKUBOLA P14] Recovered previous backup.");
            Load(SaveCampaignPaths.Missions[data.stage],false,data);
        }
        private static void Unlock(){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
        private void Freeze(bool pauseAudio)
        {
            if(!frozen)
            {
                frozen=true;inputWasEnabled=input&&input.enabled;if(input)input.enabled=false;
                for(int i=0;i<agents.Length;i++){var a=agents[i];if(a&&a.enabled&&a.isOnNavMesh){wasStopped[i]=a.isStopped;a.isStopped=true;}}
            }
            Time.timeScale=0;AudioListener.pause=pauseAudio;Unlock();
        }
        private void Resume()
        {
            if(titleScene||!frozen||hp.IsDead||hud.Stats.Completed)return;
            for(int i=0;i<agents.Length;i++){var a=agents[i];if(a&&a.enabled&&a.isOnNavMesh)a.isStopped=wasStopped[i];}
            frozen=false;Time.timeScale=1;AudioListener.pause=false;if(input)input.enabled=inputWasEnabled;
            page=MenuPage.Playing;screen.Hide();Unlock(); // First gameplay click captures, does not shoot.
        }
        private void ApplySettings()
        {
            prefs=prefs.Normalized();if(titleMusic)titleMusic.volume=.24f*prefs.Music;
            if(hud)hud.Audio.SetMix(prefs.Music,prefs.Effects);
            if(motor)motor.SensitivityMultiplier=prefs.Sensitivity;
            var mode=prefs.Fullscreen?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed;
            if(!Application.isEditor&&Screen.fullScreenMode!=mode)Screen.fullScreenMode=mode;
        }
        private void Change(Action change){change();prefs=prefs.Normalized();try{prefs.Save();}catch(Exception ex){error="SETTINGS NOT SAVED";Debug.LogWarning("[UKUBOLA P10] Settings save failed: "+ex.Message);}ApplySettings();Show(MenuPage.Options);}
        private void Options(){returnPage=page;Show(MenuPage.Options);}
        private void Confirm(MenuPage next){confirmationReturn=page;Show(next);}
        private void Load(string path, bool advance=false,SaveData restore=null,bool newCampaign=false)
        {
            if(closing)return;
            int index=SceneUtility.GetBuildIndexByScenePath(path);
            if(index<0||!Application.CanStreamedLevelBeLoaded(index)) {error="SCENE NOT REGISTERED - RUN PHASE 10 SETUP";Show(page);return;}
            var oldEntry=CampaignRun.Entry;
            var previous=page;bool audioWasPaused=AudioListener.pause;
            try{
                if(advance && (!campaign || !campaign.TryAdvance(hud,path)))return;
                if(newCampaign)CampaignRun.Reset();
                if(persistenceEnabled){SaveStore.Pending=restore;SaveStore.AutoSavePending=newCampaign||advance;}
                closing=true;AudioListener.pause=false;Unlock();Show(MenuPage.Loading);
                SceneManager.LoadScene(index,LoadSceneMode.Single);
            }catch(Exception ex){if(advance||newCampaign)CampaignRun.Set(oldEntry);if(persistenceEnabled){SaveStore.Pending=null;SaveStore.AutoSavePending=false;}closing=false;error="LOAD FAILED: "+ex.Message;Show(previous);AudioListener.pause=audioWasPaused;Debug.LogError("[UKUBOLA P10] "+ex);}
        }
        private void Quit()
        {
            if(closing)return;
            Time.timeScale=1;AudioListener.pause=false;Unlock();closing=true;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
        private void Button(string label,int row,Action action,bool available=true)=>screen.Action(label,140,151+row*49,360,42,action,available);
        private void Show(MenuPage value)
        {
            page=value;
            switch(value)
            {
                case MenuPage.Title:
                    screen.Begin("UKUBOLA",string.IsNullOrEmpty(missionLabel)?"RETRO FPS  /  PHASE 10":missionLabel,true);
                    Button("NEW GAME",0,NewCampaign);
                    if(persistenceEnabled)
                    {
                        bool available=SaveStore.TryRead(SaveStore.FilePath,out var saved,out var backup,out _);
                        Button(available?"CONTINUE - E1M"+(saved.stage+1)+(backup?" (BACKUP)":""):"CONTINUE - NO VALID SAVE",1,ContinueGame,available);
                    }
                    else Button("CONTINUE  -  NOT AVAILABLE",1,()=>{},false);
                    Button("OPTIONS",2,Options);Button("EXIT",3,()=>Confirm(MenuPage.ConfirmExit));
                    screen.Label("WASD  /  MOUSE  /  E: USE",60,374,520,34,14);break;
                case MenuPage.Paused:
                    screen.Begin("PAUSED","TIME STOPPED  /  ESC TO RESUME");
                    if(persistenceEnabled)
                    {
                        screen.Action("RESUME",140,145,360,36,Resume);
                        screen.Action("SAVE GAME",140,185,360,36,()=>WriteSave(true));
                        screen.Action("LOAD GAME",140,225,360,36,()=>Confirm(MenuPage.ConfirmLoad),SaveStore.TryRead(SaveStore.FilePath,out _,out _,out _));
                        screen.Action("RESTART LEVEL",140,265,360,36,()=>Confirm(MenuPage.ConfirmRestart));
                        screen.Action("OPTIONS",140,305,360,36,Options);screen.Action("MAIN MENU",140,345,360,36,()=>Confirm(MenuPage.ConfirmTitle));
                        if(!string.IsNullOrEmpty(saveMessage)){screen.Label(saveMessage,60,386,520,24,14,new Color32(134,239,172,255));saveMessage="";}
                    }
                    else {Button("RESUME",0,Resume);Button("RESTART LEVEL",1,()=>Confirm(MenuPage.ConfirmRestart));Button("OPTIONS",2,Options);Button("MAIN MENU",3,()=>Confirm(MenuPage.ConfirmTitle));}break;
                case MenuPage.Options:
                    screen.Begin("OPTIONS","CHANGES SAVE AUTOMATICALLY");
                    OptionRow("MUSIC",Mathf.RoundToInt(prefs.Music*100)+"%",0,()=>prefs.Music-=.1f,()=>prefs.Music+=.1f);
                    OptionRow("SOUND EFFECTS",Mathf.RoundToInt(prefs.Effects*100)+"%",1,()=>prefs.Effects-=.1f,()=>prefs.Effects+=.1f);
                    OptionRow("MOUSE SENSITIVITY",Mathf.RoundToInt(prefs.Sensitivity*100)+"%",2,()=>prefs.Sensitivity-=.1f,()=>prefs.Sensitivity+=.1f);
                    if(release&&release.enabled){screen.Action(prefs.Fullscreen?"FULLSCREEN":"WINDOWED",45,295,266,36,()=>Change(()=>prefs.Fullscreen=!prefs.Fullscreen));
                        screen.Action("FX: "+Ukubola.Release.ReleaseQuality.Label(Ukubola.Release.ReleaseQuality.Read()),329,295,266,36,()=>{try{int q=(Ukubola.Release.ReleaseQuality.Read()+1)%3;Ukubola.Release.ReleaseQuality.Write(q);release.ApplyQuality(q);}catch(Exception ex){error="GRAPHICS UPDATE FAILED";Debug.LogWarning(ex.Message);}Show(MenuPage.Options);});}
                    else screen.Action(prefs.Fullscreen?"DISPLAY: FULLSCREEN":"DISPLAY: WINDOWED",100,295,440,36,()=>Change(()=>prefs.Fullscreen=!prefs.Fullscreen));
                    screen.Action("DEFAULTS",100,343,210,40,()=>Change(()=>{prefs=MenuPreferences.Defaults;if(release&&release.enabled){try{Ukubola.Release.ReleaseQuality.Write(Ukubola.Release.ReleaseQuality.Default);release.ApplyQuality(Ukubola.Release.ReleaseQuality.Default);}catch(Exception ex){Debug.LogWarning(ex.Message);}}}));screen.Action("BACK",330,343,210,40,()=>Show(returnPage));
                    screen.Label("Display mode applies to standalone builds",65,393,510,27,14);break;
                case MenuPage.ConfirmNew:case MenuPage.ConfirmLoad:
                    screen.Begin(value==MenuPage.ConfirmNew?"START NEW CAMPAIGN?":"LOAD SAVED GAME?",value==MenuPage.ConfirmNew?"THE SAVE SLOT WILL BE REPLACED":"UNSAVED PROGRESS WILL BE LOST");
                    Button("NO - GO BACK",1,()=>Show(confirmationReturn));Button("YES",2,()=>{if(value==MenuPage.ConfirmNew)Load(RouteGame,false,null,true);else ContinueGame();});break;
                case MenuPage.ConfirmRestart:case MenuPage.ConfirmTitle:case MenuPage.ConfirmExit:
                    string title=value==MenuPage.ConfirmRestart?"RESTART LEVEL?":value==MenuPage.ConfirmTitle?"RETURN TO MAIN MENU?":"EXIT UKUBOLA?";
                    screen.Begin(title,value==MenuPage.ConfirmExit?"CLOSE THE GAME":"CURRENT LEVEL PROGRESS WILL BE LOST");
                    Button("NO - GO BACK",1,()=>Show(confirmationReturn));Button("YES",2,()=>{if(value==MenuPage.ConfirmExit)Quit();else Load(value==MenuPage.ConfirmRestart?RouteGame:RouteTitle);});break;
                case MenuPage.Dead:
                    screen.Begin("YOU DIED","UKUBOLA  /  TRY AGAIN");
                    screen.Label("TIME  "+MenuRules.Clock(hud.Stats.ElapsedSeconds),70,151,500,36,24);
                    Button("RETRY",1,()=>Load(RouteGame));Button("MAIN MENU",2,()=>Load(RouteTitle));break;
                case MenuPage.Results:
                    screen.Begin(campaign && campaign.Final ? "CAMPAIGN COMPLETE" : "LEVEL COMPLETE",campaign ? campaign.Label : "MISSION RESULTS");
                    var world=hud.World;int kills=world.Source.Enemies.Count(e=>e.State==EnemyState.Dead),items=world.Source.Pickups.Count(p=>p.Collected),secret=world.Secret.Found?1:0;
                    screen.Label(campaign && campaign.Final ? campaign.TotalResults(hud) : $"KILLS   {kills}/{world.Source.Enemies.Length}   {MenuRules.Percent(kills,world.Source.Enemies.Length)}%\nITEMS   {items}/{world.Source.Pickups.Length}   {MenuRules.Percent(items,world.Source.Pickups.Length)}%\nSECRETS   {secret}/1   {MenuRules.Percent(secret,1)}%\nTIME   {MenuRules.Clock(hud.Stats.ElapsedSeconds)}",70,145,500,143,24);
                    if(campaign && !campaign.Final)
                        screen.Action("NEXT MISSION",140,304,360,42,()=>Load(campaign.NextPath,true));
                    else if(campaign && campaign.Final)
                        screen.Action("NEW CAMPAIGN",140,304,360,42,()=>Load(RouteTitle));
                    else screen.Action("REPLAY LEVEL",140,304,360,42,()=>Load(RouteGame));
                    screen.Action("MAIN MENU",140,354,360,42,()=>Load(RouteTitle));break;
                case MenuPage.Loading:screen.Begin("LOADING","PREPARING SCENE");break;
            }
            if(!string.IsNullOrEmpty(error)){screen.Label(error,34,410,572,34,13,new Color32(255,118,91,255));error="";}
            screen.Finish();
        }
        private void OptionRow(string label,string value,int row,Action minus,Action plus)
        {
            int y=151+row*46;screen.Label(label,43,y,248,36,17);screen.Action("-",300,y,55,36,()=>Change(minus));screen.Label(value,359,y,108,36,20);screen.Action("+",471,y,55,36,()=>Change(plus));
        }
        private void OnDestroy()
        {
            if(screen!=null)screen.Dispose();if(!ownsClock)return;
            Time.timeScale=1;AudioListener.pause=false;Unlock();
        }
    }
}
