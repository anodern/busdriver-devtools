using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace BusDriverFile {
    public class PddFile {
        public static PddFile[] prefabs;
        public const uint fileHead = 0x0F;
        public int nodeCount;
        public int curveCount;
        public int unCount;
        public int lightCount;
        public int terCount;
        public int terVarCount;
        public int un2Count;
        public int endDataCount;

        public int nodeOffset;
        public int curveOffset;
        public int unOffset;
        public int lightOffset;
        public int terOffset;
        public int terNorOffset;
        public int terVarOffset;
        public int un2Offset;
        public int endDataOffset;
        public PNode[] node;
        public Curve[] curve;
        public Light[] light;

        private BinaryReader br;

        public PddFile(string path):this(new FileStream(path, FileMode.Open)) {

        }

        public PddFile(Stream stream) {
            br=new BinaryReader(stream);
            if(br.ReadUInt32()!=fileHead) {
                Debug.WriteLine("PDD文件格式错误:"+stream);
                return;
            }
            nodeCount=br.ReadInt32();
            curveCount=br.ReadInt32();
            unCount=br.ReadInt32();
            lightCount=br.ReadInt32();
            terCount=br.ReadInt32();
            terVarCount=br.ReadInt32();
            un2Count=br.ReadInt32();
            endDataCount=br.ReadInt32();

            nodeOffset=br.ReadInt32();
            curveOffset=br.ReadInt32();
            unOffset=br.ReadInt32();
            lightOffset=br.ReadInt32();
            terOffset=br.ReadInt32();
            terNorOffset=br.ReadInt32();
            terVarOffset=br.ReadInt32();
            un2Offset=br.ReadInt32();
            endDataOffset=br.ReadInt32();

            node=new PNode[nodeCount];
            for(int i = 0;i<nodeCount;i++) {
                node[i]=new PNode(br);
            }

            curve=new Curve[curveCount];
            for(int i = 0;i<curveCount;i++) {
                curve[i]=new Curve(br);
            }

            light=new Light[lightCount];
            for(int i = 0;i<lightCount;i++) {
                light[i]=new Light(br);
            }

            br.Close();
        }

        public class PNode {
            public int uData0;
            public int uData1;
            public Float3 pos;
            public Float3 dir;
            //1280字节未知数据
            public byte[] unBytes;
            public int[] inLane;
            public int[] outLane;

            public PNode(BinaryReader br) {
                uData0=br.ReadInt32();
                uData1=br.ReadInt32();
                pos=new Float3(br);
                //pos.z=-pos.z;
                dir=new Float3(br);
                //br.BaseStream.Seek(1280,SeekOrigin.Current);  //跳过1280字节
                unBytes = br.ReadBytes(1280);
                inLane=new int[8];
                for(int i = 0;i<8;i++) {
                    inLane[i]=br.ReadInt32();
                }
                outLane=new int[8];
                for(int i = 0;i<8;i++) {
                    outLane[i]=br.ReadInt32();
                }
            }
        }
        public class Curve {
            public int uData0;
            public int uData1;
            public int uData2;
            public int uData3;
            public int uData4;
            public Float3 startPos;
            public Float3 endPos;
            public Float3 startDir;
            public Float3 endDir;
            public float length;
            public int[] next;
            public int[] prve;
            public int inCount;
            public int outCount;
            public int lightTime;

            public Curve(BinaryReader br) {
                uData0=br.ReadInt32();
                uData1=br.ReadInt32();
                uData2=br.ReadInt32();
                uData3=br.ReadInt32();
                uData4=br.ReadInt32();
                startPos=new Float3(br);
                //startPos.z=-startPos.z;
                endPos=new Float3(br);
                //endPos.z=-endPos.z;
                startDir=new Float3(br);
                endDir=new Float3(br);
                length=br.ReadSingle();
                next=new int[4];
                for(int i = 0;i<4;i++) {
                    next[i]=br.ReadInt32();
                }
                prve=new int[4];
                for(int i = 0;i<4;i++) {
                    prve[i]=br.ReadInt32();
                }
                inCount=br.ReadInt32();
                outCount=br.ReadInt32();
                lightTime=br.ReadInt32();
            }
        }
        public class Light {
            public Float3 pos;
            public Float3 dir;
            public int uData0;
            public int lightNum;
            public Light(BinaryReader br) {
                pos=new Float3(br);
                //pos.z=-pos.z;
                dir=new Float3(br);
                uData0=br.ReadInt32();
                lightNum=br.ReadInt32();
            }
        }
    }

}
