using System.Collections.Generic;
using UnityEngine;
namespace Bable
{
    public sealed class ScrollJournal : MonoBehaviour
    {
        public readonly List<string> recovered = new();
        readonly Queue<string> pending = new();
        public bool Recover(string id,bool present=true) { if(!ScrollLibrary.TryGet(id,out _) || recovered.Contains(id))return false;recovered.Add(id);if(present)pending.Enqueue(id);return true; }
        void Update(){if(pending.Count>0 && BableGameUI.Instance!=null && BableGameUI.Instance.Mode=="play")BableGameUI.Instance.Scroll(pending.Dequeue());}
    }
}
