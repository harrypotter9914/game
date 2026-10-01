using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Bable
{
    // Keep the hit target still while only its decoration and lettering rise.
    public sealed class ManuscriptMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        public RectTransform lettering;
        public CanvasGroup ornament;
        bool hovered, selected;
        public bool ActiveTab {get;set;}
        float blend;
        public float Highlight => blend;
        void Awake(){var button=GetComponent<Button>();if(button!=null)button.onClick.AddListener(()=>CombatAudio.UI("ui_confirm"));}
        public void OnPointerEnter(PointerEventData e) { if(!hovered&&!selected)CombatAudio.UI("ui_select",.45f);hovered=true; }
        public void OnPointerExit(PointerEventData e) { hovered=false; }
        public void OnSelect(BaseEventData e) { if(!hovered&&!selected)CombatAudio.UI("ui_select",.45f);selected=true; }
        public void OnDeselect(BaseEventData e) { selected=false; }
        void Update()
        {
            bool focus=GameInput.NavigationActive?selected:hovered;
            blend=Mathf.MoveTowards(blend,focus||ActiveTab?1:0,Time.unscaledDeltaTime/ .18f);
            float ease=blend*blend*(3-2*blend);
            lettering.anchoredPosition=new Vector2(0,6*ease);
            ornament.alpha=ease;
        }
    }
}
