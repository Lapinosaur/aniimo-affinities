using System;
using System.IO;
using System.Linq;
namespace AniimoAffinities {
    // Only committed selections and view placement are persisted; pin and overlay are transient.
    internal sealed class SessionState {
        internal int Main=Array.IndexOf(Model.Names,"Grass"),Sub=Array.IndexOf(Model.Names,"Water");
        internal string Current="affinities",Radar="radar",Matrix="matrix";
        internal static readonly string FilePath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"AniimoAffinities","session.txt");
        internal static SessionState Load(string path) {
            var state=new SessionState();
            try {
                var lines=File.ReadAllLines(path);
                if(lines.Length!=6 || lines[0]!="1")return state;
                int main=lines[1]==""?-1:Array.IndexOf(Model.Names,lines[1]),sub=lines[2]==""?-1:Array.IndexOf(Model.Names,lines[2]);
                if((main<0 && lines[1]!="") || (sub<0 && lines[2]!="") || (main<0 && sub>=0) || (main>=0 && main==sub))return state;
                var views=lines.Skip(3).ToArray();
                if(views.Distinct().Count()!=3 || views.Any(view=>view!="affinities" && view!="radar" && view!="matrix"))return state;
                state.Main=main;state.Sub=sub;state.Current=lines[3];state.Radar=lines[4];state.Matrix=lines[5];
            } catch(IOException) {} catch(UnauthorizedAccessException) {}
            return state;
        }
        internal void Save(string path) {
            string temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllLines(temporary,new[]{"1",Main<0?"":Model.Names[Main],Sub<0?"":Model.Names[Sub],Current,Radar,Matrix});
                if(File.Exists(path))File.Replace(temporary,path,null);else File.Move(temporary,path);
            } catch(IOException) {} catch(UnauthorizedAccessException) {}
            finally { try { if(File.Exists(temporary))File.Delete(temporary); } catch(IOException) {} catch(UnauthorizedAccessException) {} }
        }
    }
}
