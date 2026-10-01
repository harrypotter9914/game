using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;
namespace Bable {
 public static class LocalStorage {
  [Serializable] class Envelope {public int version=1;public string payload,sha256;}
  public static string Root {get{var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-bableSaveRoot");return i>=0&&i+1<args.Length?Path.GetFullPath(args[i+1]):Path.Combine(Application.persistentDataPath,BuildFlavor.StorageFolder);}}
  static string Hash(string value){using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-","");}
  public static void Write(string name,string payload){
   Directory.CreateDirectory(Root);string path=Path.Combine(Root,name),temp=path+".tmp";
   byte[] bytes=Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Envelope{payload=payload,sha256=Hash(payload)},true));
   using(var file=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)){file.Write(bytes,0,bytes.Length);file.Flush(true);}
   if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);
  }
  public static string ReadFile(string path){var e=JsonUtility.FromJson<Envelope>(File.ReadAllText(path,Encoding.UTF8));if(e==null||e.version!=1||e.payload==null||e.sha256!=Hash(e.payload))throw new InvalidDataException("Unsupported or damaged save");return e.payload;}
  public static string Read(string name,out bool recovered){
   recovered=false;var path=Path.Combine(Root,name);
   try{if(File.Exists(path))return ReadFile(path);}catch(Exception e){Debug.LogWarning(name+": "+e.Message);}
   if(File.Exists(path+".bak")){var result=ReadFile(path+".bak");recovered=true;
    if(File.Exists(path))File.Copy(path,path+".damaged",true);
    File.Copy(path+".bak",path,true);return result;}
   if(File.Exists(path))throw new InvalidDataException("No usable backup for "+name);return null;
  }
 }
}
