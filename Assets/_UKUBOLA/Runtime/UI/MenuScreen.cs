using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Ukubola.UI
{
    // All layout is created at runtime, in a separate high-order overlay canvas.
    public sealed class MenuScreen
    {
        private readonly GameObject canvasRoot, eventsRoot;
        private readonly RectTransform surface;
        private readonly Image shade;
        private readonly Font font;
        private readonly Ukubola.Release.ReleaseArt art;
        private GameObject pageRoot;
        private Button first, preferred;
        private string heading, previousSelection;
        private readonly Color ink=new Color32(231,214,178,255), ember=new Color32(220,79,48,255), metal=new Color32(45,51,49,255);
        public MenuScreen(Font typeface, Ukubola.Release.ReleaseArt releaseArt=null)
        {
            font=typeface;art=releaseArt;
            canvasRoot=new GameObject("Phase10 Menu Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            
            var canvas=canvasRoot.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
            var scaler=canvasRoot.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(640,480);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            shade=Add<Image>(canvasRoot.transform,"Shade",0,0,640,480);var sr=shade.rectTransform;sr.anchorMin=Vector2.zero;sr.anchorMax=Vector2.one;sr.offsetMin=sr.offsetMax=Vector2.zero;
            surface=Rect(canvasRoot.transform,"4:3 Surface",0,0,640,480);surface.anchorMin=surface.anchorMax=new Vector2(.5f,.5f);surface.pivot=new Vector2(.5f,.5f);surface.anchoredPosition=Vector2.zero;surface.sizeDelta=new Vector2(640,480);
            // Previous lab scenes contain no EventSystem; use the Input System defaults at runtime.
            eventsRoot=new GameObject("Phase10 EventSystem");eventsRoot.AddComponent<EventSystem>();eventsRoot.AddComponent<InputSystemUIInputModule>();
        }
        private static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {
            var go=new GameObject(name,typeof(RectTransform));var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
        }
        private static T Add<T>(Transform parent,string name,float x,float y,float w,float h) where T:Graphic
        {var r=Rect(parent,name,x,y,w,h);return r.gameObject.AddComponent<T>();}
        private void Panel(Transform parent,float x,float y,float w,float h,Color color)
        {var i=Add<Image>(parent,"Panel",x,y,w,h);i.color=color;i.raycastTarget=false;}
        public void Begin(string title,string subtitle,bool main=false)
        {
            canvasRoot.SetActive(true);eventsRoot.SetActive(true);shade.color=main?Color.black:new Color(0,0,0,.76f);
            previousSelection=heading==title&&EventSystem.current&&EventSystem.current.currentSelectedGameObject?EventSystem.current.currentSelectedGameObject.name:null;
            heading=title;if(pageRoot){pageRoot.SetActive(false);UnityEngine.Object.Destroy(pageRoot);}
            pageRoot=Rect(surface,"Menu Page",0,0,640,480).gameObject;first=null;preferred=null;
            var p=pageRoot.transform;
            if(art)pageRoot.AddComponent<Ukubola.Release.ReleasePageFade>();
            Panel(p,0,0,640,480,new Color32(20,25,24,245));
            Panel(p,12,12,616,456,new Color32(73,61,48,255));Panel(p,16,16,608,448,new Color32(25,30,28,255));
            Panel(p,26,26,588,5,ember);Panel(p,26,449,588,5,ember);
            for(int i=0;i<8;i++){Panel(p,29+i*75,34,38,3,metal);Panel(p,29+i*75,442,38,3,metal);}
            if(art&&art.Texture("MenuBackground"))
            {
                var bg=Add<RawImage>(p,"P15 background",16,16,608,448);bg.texture=art.Texture("MenuBackground");bg.color=new Color(.72f,.86f,.87f,main?.92f:.37f);bg.raycastTarget=false;
                if(main)bg.gameObject.AddComponent<Ukubola.Release.ReleaseMenuMotion>();
                Panel(p,26,26,588,3,new Color32(79,207,182,255));Panel(p,26,449,588,3,new Color32(79,207,182,255));
                Panel(p,124,143,392,232,new Color(0.02f,.045f,.06f,main?.80f:.25f));
            }
            if(main&&art&&art.Texture("Logo")){var logo=Add<RawImage>(p,"UKUBOLA logo",30,39,580,77);logo.texture=art.Texture("Logo");logo.raycastTarget=false;}
            else Label(title,30,42,580,65,main?62:36,art?(Color)new Color32(121,231,208,255):ember,true);
            Label(subtitle,32,111,576,30,15,ink,false);
        }
        public Text Label(string value,float x,float y,float w,float h,int size,Color? tint=null,bool bold=false)
        {
            var t=Add<Text>(pageRoot.transform,value,x,y,w,h);t.font=font;t.text=value;t.fontSize=size;t.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;t.alignment=TextAnchor.MiddleCenter;t.color=tint??ink;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;
        }
        public Button Action(string label,float x,float y,float w,float h,System.Action callback,bool available=true)
        {
            var bg=Add<Image>(pageRoot.transform,label+"@"+x+","+y,x,y,w,h);bg.color=Color.white;var b=bg.gameObject.AddComponent<Button>();b.targetGraphic=bg;
            var colors=b.colors;colors.normalColor=metal;colors.highlightedColor=new Color32(112,61,39,255);colors.selectedColor=new Color32(146,70,42,255);colors.pressedColor=ember;colors.disabledColor=new Color32(34,37,35,255);colors.fadeDuration=.08f;if(art){colors.normalColor=new Color32(29,47,53,250);colors.highlightedColor=new Color32(43,111,110,255);colors.selectedColor=new Color32(49,125,120,255);colors.pressedColor=new Color32(25,170,144,255);}b.colors=colors;
            var t=Add<Text>(bg.transform,"Label",0,0,w,h);t.font=font;t.text=label;t.fontSize=20;t.fontStyle=FontStyle.Bold;t.alignment=TextAnchor.MiddleCenter;t.color=available?ink:new Color32(114,118,110,255);t.raycastTarget=false;
            b.interactable=available;if(available){b.onClick.AddListener(()=>callback());if(!first)first=b;if(bg.gameObject.name==previousSelection)preferred=b;}return b;
        }
        public void Finish(){var selected=preferred?preferred:first;if(selected&&EventSystem.current)EventSystem.current.SetSelectedGameObject(selected.gameObject);}
        public void Dispose(){if(canvasRoot)UnityEngine.Object.Destroy(canvasRoot);if(eventsRoot)UnityEngine.Object.Destroy(eventsRoot);}
        public void Hide(){if(EventSystem.current)EventSystem.current.SetSelectedGameObject(null);eventsRoot.SetActive(false);canvasRoot.SetActive(false);}
    }
}
