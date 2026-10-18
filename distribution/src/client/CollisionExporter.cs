using System.Buffers.Binary;
using UnityEngine;

namespace SLcraft.Client;

// Samples real Unity collision near the player into SkyCraft's 1/8-block occupancy format.
// Sampling is spread across frames; Minecraft still performs its own movement/collision resolution.
public sealed class CollisionExporter
{
 readonly Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Collider> overlaps=new(64);
 readonly List<byte[]> blocks=new();
 int minX,minY,minZ,maxX,maxY,maxZ,blockIndex,subIndex;
 ulong[] bits=new ulong[8];
 Vector3 lastCenter=new(float.PositiveInfinity,0,0);
 float nextScan;
 int epoch;
 bool scanning;
 const int width=32,depth=32,height=14,total=width*depth*height;
 public int SentRegions { get; private set; }
 public int LastSolidBlocks { get; private set; }
 public void Rescan()
 {
  // Roles and admin teleports stay in the same facility. Retain earlier areas
  // so mobs left behind do not lose their floor when the player moves away.
  scanning=false; lastCenter=new Vector3(float.PositiveInfinity,0,0); nextScan=0;
 }
 public void Reset(SharedLink link,int nextEpoch)
 {
  epoch=nextEpoch; scanning=false; lastCenter=new Vector3(float.PositiveInfinity,0,0);
  SentRegions=0; LastSolidBlocks=0;
  Span<byte> reset=stackalloc byte[4]; BinaryPrimitives.WriteInt32LittleEndian(reset,epoch); link.CollisionMessage(1,reset);
 }
 public void Tick(SharedLink link,Vector3 feet,int budget=1500)
 {
  Vector3 mc=new(-feet.x,feet.y,feet.z);
  if(!scanning) {
   if(Time.unscaledTime<nextScan && (mc-lastCenter).sqrMagnitude<4) return;
   minX=Mathf.FloorToInt(mc.x)-width/2; minY=Mathf.FloorToInt(mc.y)-10; minZ=Mathf.FloorToInt(mc.z)-depth/2;
   maxX=minX+width-1; maxY=minY+height-1; maxZ=minZ+depth-1;
   lastCenter=mc; blocks.Clear(); blockIndex=0; subIndex=0; bits=new ulong[8]; scanning=true;
  }
  for(int sample=0;sample<budget && blockIndex<total;sample++) {
   int bx=blockIndex%width+minX, bz=blockIndex/width%depth+minZ, by=blockIndex/(width*depth)+minY;
   // Most cells are empty. A conservative whole-cell probe avoids 64 tiny
   // queries there and makes the wider mob navigation area practical.
   if(subIndex==0 && !Solid(new Vector3(-(bx+.5f),by+.5f,bz+.5f),.501f)) { blockIndex++; continue; }
   int sx=subIndex%4,sz=subIndex/4%4,sy=subIndex/16;
   Vector3 center=new(-(bx+(sx+.5f)/4),by+(sy+.5f)/4,bz+(sz+.5f)/4);
   // A cell is .25 m wide: smaller than .125 leaves gaps at cell boundaries,
   // missing thin floor and wall meshes exactly on an integer coordinate.
   bool solid=Solid(center,.126f);
   if(solid) for(int dy=0;dy<2;dy++) for(int dz=0;dz<2;dz++) for(int dx=0;dx<2;dx++) bits[sy*2+dy]|=1UL<<((sz*2+dz)*8+sx*2+dx);
   if(++subIndex==64) {
    if(bits.Any(v=>v!=0)) {
     byte[] block=new byte[80]; BinaryPrimitives.WriteInt32LittleEndian(block.AsSpan(0),bx); BinaryPrimitives.WriteInt32LittleEndian(block.AsSpan(4),by); BinaryPrimitives.WriteInt32LittleEndian(block.AsSpan(8),bz);
     for(int i=0;i<8;i++) BinaryPrimitives.WriteUInt64LittleEndian(block.AsSpan(16+i*8),bits[i]); blocks.Add(block);
    }
    bits=new ulong[8]; subIndex=0; blockIndex++;
   }
  }
  if(blockIndex==total) {
   byte[] payload=new byte[32+blocks.Count*80];
   int[] header={minX,minY,minZ,maxX,maxY,maxZ,epoch,blocks.Count};
   for(int i=0;i<8;i++) BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(i*4),header[i]);
   for(int i=0;i<blocks.Count;i++) blocks[i].CopyTo(payload,32+i*80);
   link.CollisionMessage(2,payload); SentRegions++; LastSolidBlocks=blocks.Count; nextScan=Time.unscaledTime+.15f; scanning=false;
  }
 }
 bool Solid(Vector3 center,float half) {
  int count=Physics.OverlapBoxNonAlloc(center,Vector3.one*half,overlaps,Quaternion.identity,~0,QueryTriggerInteraction.Ignore);
  for(int i=0;i<count;i++) {
   var collider=overlaps[i]; if(!collider || !collider.enabled) continue;
   if(collider.GetComponentInParent<Il2Cpp.ReferenceHub>()) continue;
   if(collider.transform.root.name.StartsWith("SLcraft.Client")) continue;
   return true;
  }
  return false;
 }
}
