using System;
using System.Collections.Generic;
using UnityEngine;
namespace Bable
{
    public static class ScrollLibrary
    {
        public struct Entry { public string id,title,text; public Entry(string i,string t,string v){id=i;title=t;text=v;} }
        public static readonly Entry[] Entries = {
            new("hoxi 4.23 travellers","Traveller I  |  April 23","My homeland has burned. I have not eaten in three days. There are rats and corpses everywhere. God, protect me."),
            new("hoxi 4.24 travellers","Traveller II  |  April 24","God be praised. At the edge of the forest stands a tower. It looks long abandoned, with nobody inside. It may be a good place to take shelter."),
            new("hoxi 4.25 travellers","Traveller III  |  April 25","I have settled inside the tower. Its name is carved on the wall: Ba... The remaining letters are illegible. At last I have time to pray and repent. I have only one indulgence left. When the plague ends, I will give it to the Church."),
            new("hoxi 4.28 travellers","Traveller IV  |  April 28","This tower is a maze. I cannot find the way out, and my food is nearly gone. I must climb higher and see how I can escape."),
            new("hoxi 4.30 travellers","Traveller V  |  April 30","Today is Easter, but I may die here. I hear a voice from the top of the tower. Perhaps hunger is making me imagine it. I want to go home."),
            new("hoxi 6.14 builders","Builder I  |  June 14","Today is the first day of building Babel. We will build so high that the disaster can never hurt us again. They call us hollow, but only we know this is the sole way to survive. Praise great Nero, who saved us."),
            new("hoxi 8.30 builders","Builder II  |  August 30","The foundations are finished. Forgive these two silent months, Mother. There has been too much work. Our great leader Nero has extended our day from five in the morning until seven at night to four in the morning until eight at night. I am so tired."),
            new("hoxi 10.1 builders","Builder III  |  October 1","I am certain we are high enough to escape the great flood. But he, Nero, still thinks it is not enough. He wants to build as high as the sky, level with heaven and the gods. We are no longer merely protecting ourselves. This is becoming blasphemy. I am afraid."),
            new("hoxi 12.12 builders","Builder IV  |  December 25","Mother, I have found faith again. I am hollow no longer. Today my great god was born: Nero.")
        };
        public static bool TryGet(string id,out Entry entry)
        { foreach(var e in Entries)if(string.Equals(e.id,id,StringComparison.OrdinalIgnoreCase)){entry=e;return true;}entry=default;return false; }
    }
}
