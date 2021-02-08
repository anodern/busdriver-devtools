using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusDriverFile {
    static class DefLib {
        public static PrefabFile[] pdds;
        public static void Load() {
            string path;
            if(File.Exists("prefab.def")) {
                path="prefab.def";
            } else if(File.Exists("def/world/prefab.def")) {
                path="def/world/prefab.def";
            } else if(File.Exists("base/def/world/prefab.def")) {
                path="base/def/world/prefab.def";
            } else {
                return;
            }
            DefReader prefabReader = new DefReader(path);
            prefabReader.keys.TryGetValue("prefab_count", out string temp);
            uint prefabCount = Convert.ToUInt32(temp);

            pdds=new PrefabFile[prefabCount];
            for(uint i = 0; i<prefabCount; i++) {
                if(prefabReader.keys.TryGetValue("prefab"+i, out temp)) {
                    if(temp.Equals("\"\"")) continue;
                    string path2 = temp.Substring(2, temp.Length-3).Replace(".pmd", ".pdd");
                    if(!File.Exists(path2)) {
                        path2 = "base/" + path2;
                    }
                    if(File.Exists(path2)) {
                        try {
                            pdds[i]=new PrefabFile(path2);
                        } catch(Exception e) {
                            throw e;
                        }
                    } else {
                        throw new Exception("base不完整");
                    }
                }
            }
        }
    }
}
