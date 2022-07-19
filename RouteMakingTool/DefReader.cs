using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BusDriverFile {
    class DefReader {
        public Dictionary<string,string> keys;
        public DefReader(string path): this(path,Encoding.UTF8) { }
        public DefReader(string path,Encoding a): this(new FileStream(path, FileMode.Open), a) {
        }
        public DefReader(Stream stream, Encoding a) {
            StreamReader sr = new StreamReader(stream, a);
            string temp;
            keys=new Dictionary<string, string>();
            while(!sr.EndOfStream) {
                temp=sr.ReadLine().Trim();
                if(temp.Contains("#")) {
                    int index = temp.IndexOf('#');
                    if(index==0) continue;
                    temp=temp.Substring(0, index);
                    if(temp.Length<1) continue; //去注释
                }

                if(temp.Contains(":")) {
                    //属性行
                    int index = temp.IndexOf(':');
                    string keyName = temp.Substring(0, index).Trim();
                    string value = temp.Substring(index+1, temp.Length-index-1).Trim();
                    if(!keys.ContainsKey(keyName))
                        keys.Add(keyName, value);
                }
            }
            sr.Close();
        }

        public Encoding GetFileEncodeType(string filename) {
            FileStream fs = new FileStream(filename,FileMode.Open,FileAccess.Read);
            BinaryReader br = new BinaryReader(fs);
            byte[] buffer = br.ReadBytes(2);
            if(buffer[0]>=0xEF) 
                if(buffer[0]==0xEF && buffer[1]==0xBB) return Encoding.UTF8;
                else if(buffer[0]==0xFE && buffer[1]==0xFF) return Encoding.BigEndianUnicode;
                else if(buffer[0]==0xFF && buffer[1]==0xFE) return Encoding.Unicode;
                else return Encoding.Default;
             else return Encoding.Default;
        }
    }

}
