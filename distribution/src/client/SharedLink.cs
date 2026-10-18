using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace SLcraft.Client;

// SkyCraft protocol v11. This owns a new IPC mapping; it never reads or writes another process's memory.
public sealed unsafe class SharedLink : IDisposable
{
 public const string MappingName = "Local\\SLcraftClient_v11";
 public const long Input = 0x1000, Collision = 0x20000, CollisionSize = 32L << 20;
 public const long OverlayPixels = Collision + CollisionSize, OverlaySlotSize = 3840L * 2160 * 4;
 public const long Render = OverlayPixels + OverlaySlotSize * 3, RenderSize = 64L << 20, Capacity = Render + RenderSize;
 readonly MemoryMappedFile mapping;
 readonly MemoryMappedViewAccessor view;
 byte* data;
 int hostSeq, overlayFront = 2;
 long inputHead, collisionHead;
 public string LastError { get; private set; } = "";
 public SharedLink(string mappingName = MappingName)
 {
  mapping = MemoryMappedFile.CreateNew(mappingName, Capacity, MemoryMappedFileAccess.ReadWrite);
  view = mapping.CreateViewAccessor(0, Capacity, MemoryMappedFileAccess.ReadWrite);
  view.SafeMemoryMappedViewHandle.AcquirePointer(ref data);
  *(uint*)(data + 4) = 11;
  *(uint*)(data + 8) = (uint)Environment.ProcessId;
  *(uint*)data = 0x43594B53;
  Heartbeat();
 }
 public void Heartbeat() => Volatile.Write(ref *(long*)(data + 0x10), Environment.TickCount64);
 public bool MinecraftResponsive => Read64(0x18) != 0 && Environment.TickCount64 - Read64(0x18) < 3000;
 public bool MinecraftAlive {
  get {
   if(MinecraftResponsive) return true;
   if(Read64(0x18)==0 || MinecraftPid<=0) return false;
   // Resource reloads/GC may stop the render heartbeat for several seconds.
   // Keep the last HUD and pause movement while that same process is alive.
   try { using var process=Process.GetProcessById(MinecraftPid); return !process.HasExited; }
   catch(ArgumentException) { return false; }
  }
 }
 public int MinecraftPid => *(int*)(data + 0x0C);
 public int Read32(long offset) => Volatile.Read(ref *(int*)(data + offset));
 public long Read64(long offset) => Volatile.Read(ref *(long*)(data + offset));
 public void HostState(bool active, bool paused, int world, int epoch, double x, double y, double z, float yaw, float pitch, int teleport, int width, int height)
 {
  byte* p = data + 0x100;
  Volatile.Write(ref *(int*)p, ++hostSeq);
  *(int*)(p + 4) = (active ? 1 : 0) | (paused ? 2 : 0);
  *(int*)(p + 8) = world; *(int*)(p + 12) = epoch;
  *(double*)(p + 16) = x; *(double*)(p + 24) = y; *(double*)(p + 32) = z;
  *(float*)(p + 40) = yaw; *(float*)(p + 44) = pitch;
  *(int*)(p + 48) = teleport; *(int*)(p + 52) = width; *(int*)(p + 56) = height; *(float*)(p + 60) = 12;
  Volatile.Write(ref *(int*)p, ++hostSeq);
 }
 public McState? PlayerState()
 {
  for (int attempt = 0; attempt < 4; attempt++) {
   int seq = Read32(0x200); if ((seq & 1) != 0) continue;
   byte* p = data + 0x200;
   var state = new McState(*(int*)(p + 4), *(double*)(p + 8), *(double*)(p + 16), *(double*)(p + 24),
    *(float*)(p + 0x28), *(int*)(p + 0x30), *(float*)(p + 0x40), *(double*)(p + 0x50), *(double*)(p + 0x58), *(double*)(p + 0x60));
   Thread.MemoryBarrier();
   if (seq == Read32(0x200)) return state;
  }
  return null;
 }
 public void InputEvent(ushort type, ushort code = 0, int a = 0, int b = 0, int c = 0)
 {
  long tail = Read64(Input + 0x40);
  if (inputHead - tail >= 4096) { LastError = "Minecraft input queue full"; return; }
  byte* p = data + Input + 0x80 + (inputHead & 4095) * 16;
  *(ushort*)p = type; *(ushort*)(p + 2) = code;
  *(int*)(p + 4) = a; *(int*)(p + 8) = b; *(int*)(p + 12) = c;
  Volatile.Write(ref *(long*)(data + Input), ++inputHead);
 }
 public void CollisionMessage(int kind, ReadOnlySpan<byte> payload)
 {
  const long size = CollisionSize - 0x80;
  long bytes = (8L + payload.Length + 7) & ~7L, pos = collisionHead % size;
  long pad = pos + bytes > size ? size - pos : 0;
  if (bytes + pad > size || collisionHead - Read64(Collision + 0x40) + bytes + pad > size) return;
  if (pad > 0) { *(long*)(data + Collision + 0x80 + pos) = 0; collisionHead += pad; pos = 0; }
  byte* target = data + Collision + 0x80 + pos;
  *(int*)target = kind; *(int*)(target + 4) = payload.Length;
  payload.CopyTo(new Span<byte>(target + 8, payload.Length));
  collisionHead += bytes; Volatile.Write(ref *(long*)(data + Collision), collisionHead);
 }
 public List<RenderPacket> RenderMessages(int budgetBytes = 16 << 20)
 {
  var result = new List<RenderPacket>();
  const long size = RenderSize - 0x80;
  long head = Read64(Render), tail = Read64(Render + 0x40), used = 0;
  if (tail < 0 || head < tail || head - tail > size) throw new InvalidDataException("Invalid Minecraft render ring counters");
  while (tail < head && used < budgetBytes) {
   long pos = tail % size;
   if (size - pos < 8) throw new InvalidDataException("Invalid render packet boundary");
   byte* p = data + Render + 0x80 + pos;
   int kind = *(int*)p, count = *(int*)(p + 4);
   if (kind == 0) { tail += size - pos; continue; }
   long bytes = (8L + count + 7) & ~7L;
   if (count < 0 || bytes > head - tail || bytes > size - pos || count > 64 * 1024 * 1024) throw new InvalidDataException("Invalid Minecraft render packet size");
   result.Add(new RenderPacket(kind, new ReadOnlySpan<byte>(p + 8, count).ToArray()));
   tail += bytes; used += bytes;
  }
  Volatile.Write(ref *(long*)(data + Render + 0x40), tail);
  // Combat events are consumed without applying server-side effects in this client-only adapter.
  Volatile.Write(ref *(long*)(data + 0x17040), Read64(0x17000));
  return result;
 }
 public OverlayFrame? Overlay()
 {
  if ((Read32(0x300) & 4) == 0) return null;
  int old = Interlocked.Exchange(ref *(int*)(data + 0x300), overlayFront);
  overlayFront = old & 3;
  if (overlayFront > 2) throw new InvalidDataException("Invalid overlay slot");
  byte* hdr = data + 0x340 + overlayFront * 64;
  int width = *(int*)hdr, height = *(int*)(hdr + 4), flags = *(int*)(hdr + 8);
  if (width < 1 || width > 3840 || height < 1 || height > 2160) throw new InvalidDataException("Invalid Minecraft overlay dimensions");
  int count = checked(width * height * 4);
  var pixels = new ReadOnlySpan<byte>(data + OverlayPixels + overlayFront * OverlaySlotSize, count).ToArray();
  return new OverlayFrame(width, height, (flags & 1) != 0, pixels);
 }
 public void Dispose()
 {
  if (data == null) return;
  Volatile.Write(ref *(long*)(data + 0x10), 0);
  view.SafeMemoryMappedViewHandle.ReleasePointer(); data = null;
  view.Dispose(); mapping.Dispose();
 }
}
public readonly record struct McState(int Flags, double X, double Y, double Z, float EyeHeight, int TeleportAck, float Fov, double EyeX, double EyeY, double EyeZ)
{
 public bool InWorld => (Flags & 1) != 0;
 public bool MenuOpen => (Flags & 2) != 0;
}
public readonly record struct RenderPacket(int Kind, byte[] Bytes);
public readonly record struct OverlayFrame(int Width, int Height, bool BottomUp, byte[] Pixels);
