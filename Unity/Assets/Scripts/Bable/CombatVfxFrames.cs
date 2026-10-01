using System.Linq;
using UnityEngine;
namespace Bable
{
    public static class CombatVfxFrames
    {
        static Sprite[] frames;
        static Sprite[] bossFrames;
        public static Sprite Get(string art,int stage)
        {
            int bossRow=art=="BossWave41"?0:art=="CrystalTrap41"?1:art=="GroundImpact41"?2:-1;
            if(bossRow>=0){if(bossFrames==null)bossFrames=Resources.LoadAll<Sprite>("Bable/NewArt/BossEffects41").OrderBy(s=>s.name).ToArray();if(bossFrames.Length==12)return bossFrames[bossRow*4+Mathf.Clamp(stage,0,3)];}
            int row=art=="SwordArc"?0:art=="Shockwave"?1:art=="ArcaneOrb"?2:art=="CrystalBolt"?3:-1;
            if(row<0)return Resources.Load<Sprite>("Bable/NewArt/"+art);
            if(frames==null)frames=Resources.LoadAll<Sprite>("Bable/NewArt/CombatVfxAtlas").OrderBy(s=>s.name).ToArray();
            return frames.Length==16?frames[row*4+Mathf.Clamp(stage,0,3)]:Resources.Load<Sprite>("Bable/NewArt/"+(row>1?"Shockwave":art));
        }
    }
}
