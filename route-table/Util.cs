using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusDriverFile {
    class Util {
        public static int HexToFloat(string temp) {
            if(temp.IndexOf('&')<0) return int.Parse(temp);
            else {
                temp = temp.Replace("&","");
                byte[] bytes = new byte[4];
                bytes[0]=Convert.ToByte(temp.Substring(0,2),16);
                bytes[1]=Convert.ToByte(temp.Substring(2,2),16);
                bytes[2]=Convert.ToByte(temp.Substring(4,2),16);
                bytes[3]=Convert.ToByte(temp.Substring(6,2),16);

                float f = BitConverter.ToSingle(bytes,0);
                return (int)f;
            }
        }
    }
}
