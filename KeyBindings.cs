using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace AniimoAffinities {
    internal static class KeyBindings {
        internal static readonly string[] Actions={"Clear","Dark","Earth","Fire","Grass","Ice","Light","Lightning","Water","Wind","Pin","Lock / unlock","Settings","Minimize","Effectiveness matrix","Affinity radar"};
        internal static readonly string FilePath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"AniimoAffinities","keys.txt");
        internal static Keys[] Defaults() { return Enumerable.Range(0,10).Select(i=>(Keys)((int)Keys.D0+i)).Concat(new[]{Keys.F6,Keys.Control|Keys.Alt|Keys.L,Keys.F7,Keys.F9,Keys.F8,Keys.F10}).ToArray(); }
        internal static Keys[] Choices() { return Enumerable.Range(0,10).Select(i=>(Keys)((int)Keys.D0+i)).Concat(Enumerable.Range((int)Keys.A,26).Select(i=>(Keys)i)).Concat(Enumerable.Range((int)Keys.F1,12).Select(i=>(Keys)i)).ToArray(); }
        internal static string Label(Keys key) {
            if(key==Keys.None)return "";
            var code=key&Keys.KeyCode;
            return ((key&Keys.Control)!=0?"Ctrl+":"")+((key&Keys.Alt)!=0?"Alt+":"")+((key&Keys.Shift)!=0?"Shift+":"")+(code>=Keys.D0 && code<=Keys.D9?((int)code-(int)Keys.D0).ToString():code.ToString());
        }
        internal static bool Valid(Keys[] keys) { var assigned=keys.Where(key=>key!=Keys.None).ToArray();return keys.Length==Actions.Length && assigned.Distinct().Count()==assigned.Length && keys.Select((key,index)=>key==Keys.None || (index<10?Choices().Contains(key):Choices().Contains(key&Keys.KeyCode))).All(valid=>valid); }
        // Inputs: saved per-user bindings. Output: complete unique map. Fallback: defaults on invalid/missing data. Raises: none for filesystem/config errors.
        internal static Keys[] Load(string path) {
            try {
                var keys=File.ReadAllLines(path).Select(value=>(Keys)int.Parse(value)).ToArray();
                if(keys.Length==10 || keys.Length==14 || keys.Length==15)keys=keys.Concat(Defaults().Skip(keys.Length).Select(key=>keys.Contains(key)?Keys.None:key)).ToArray();
                return Valid(keys)?keys:Defaults();
            } catch(IOException) { return Defaults(); } catch(UnauthorizedAccessException) { return Defaults(); } catch(FormatException) { return Defaults(); } catch(OverflowException) { return Defaults(); }
        }
        // Inputs: validated mapping. Output: atomic per-user persistence. Fallback: retain prior mapping on write failure. Raises: invalid mapping or filesystem errors.
        internal static void Save(string path,Keys[] keys) {
            if(!Valid(keys))throw new ArgumentException("Each action must have a different key.");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var temp=path+".tmp";File.WriteAllLines(temp,keys.Select(key=>((int)key).ToString()).ToArray());
            if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);
        }
        internal static int Resolve(Keys[] keys,Keys input) {
            var key=input&Keys.KeyCode;var modifiers=input&Keys.Modifiers;
            if(key==Keys.None)return -1;
            if((modifiers&(Keys.Control|Keys.Alt))!=0)return -1;
            if(key>=Keys.NumPad0 && key<=Keys.NumPad9)key=(Keys)((int)Keys.D0+(int)key-(int)Keys.NumPad0);
            if(modifiers==Keys.Shift && !(key>=Keys.D0 && key<=Keys.D9))return -1;
            return Array.IndexOf(keys,key);
        }
    }
}
