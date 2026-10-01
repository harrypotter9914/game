using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Bable
{
    public static class ElevenVoiceImportAudit
    {
        [Serializable] class ClipRow { public string name; public float seconds; public int channels, frequency; }
        [Serializable] class Report { public int expected=78; public int imported; public List<ClipRow> clips=new List<ClipRow>(); public List<string> errors=new List<string>(); }
        public static void Run()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var report=new Report();
            const string folder="Assets/Resources/Bable/Dialogue/Voices";
            foreach(var path in Directory.GetFiles(folder,"*.wav"))
            {
                var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path.Replace('\\','/'));
                if(clip==null){report.errors.Add(path+": import failed");continue;}
                if(clip.length<=0||clip.samples<=0)report.errors.Add(path+": empty clip");
                if(Resources.Load<AudioClip>("Bable/Dialogue/Voices/"+Path.GetFileNameWithoutExtension(path))==null)report.errors.Add(path+": runtime resource missing");
                report.clips.Add(new ClipRow{name=clip.name,seconds=clip.length,channels=clip.channels,frequency=clip.frequency});
            }
            report.imported=report.clips.Count;
            if(report.imported!=report.expected)report.errors.Add("Expected 78 dialogue and prologue clips");
            File.WriteAllText("../reference/revision38-eleven/unity-import-audit.json",JsonUtility.ToJson(report,true));
            if(report.errors.Count>0)throw new Exception(string.Join("; ",report.errors));
            Debug.Log("ELEVEN_V4_IMPORT_OK "+report.imported);
        }
    }
}
