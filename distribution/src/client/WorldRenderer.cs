using System.Buffers.Binary;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SLcraft.Client;

// Draws Minecraft-exported geometry and atlases in the host client's own depth-tested scene.
public sealed class WorldRenderer : IDisposable
{
 readonly Dictionary<string, GameObject> sections = new();
 readonly Dictionary<int, Texture2D> textures = new();
 readonly List<GameObject> dynamic = new();
 readonly record struct MaterialKey(int Texture,uint Tint,bool Blend);
 readonly Dictionary<MaterialKey,Material> materials = new();
 readonly HashSet<Mesh> meshes = new();
 readonly GameObject root;
 readonly GameObject overlayRoot;
 readonly Canvas overlayCanvas;
 readonly RawImage overlayImage;
 Texture2D? atlas, overlay;
 byte[]? atlasBytes;
 bool atlasDirty;
 float atlasUploadAt;
 bool bottomUp;
 Shader shader;
 public int SectionCount => sections.Count;
 public long OverlayFrames { get; private set; }
 public bool HasAtlas => atlas && atlasBytes!=null;
 public string AtlasStatus {
  get {
   bool bound=materials.Any(m=>m.Key.Texture==0 && m.Value && m.Value.GetTexture("_UnlitColorMap")?.Pointer==atlas?.Pointer);
   return $"atlas={atlas?.width}x{atlas?.height}; sections={sections.Count}; atlasMaterialBound={bound}";
  }
 }
 public bool Visible { set { if(root) root.SetActive(value); if(overlayCanvas) overlayCanvas.enabled=value; } }
 public void SetHidden() => Visible=false;
 public void Present(bool world,bool hud) { if(root) root.SetActive(world); if(overlayCanvas) overlayCanvas.enabled=hud; }
 public WorldRenderer()
 {
  root = new GameObject("SLcraft.Client.Minecraft"); Object.DontDestroyOnLoad(root);
  overlayRoot=new GameObject("SLcraft.Client.Overlay"); Object.DontDestroyOnLoad(overlayRoot);
  overlayCanvas=overlayRoot.AddComponent<Canvas>(); overlayCanvas.renderMode=RenderMode.ScreenSpaceOverlay; overlayCanvas.sortingOrder=30000;
  var imageObject=new GameObject("Minecraft hand and HUD"); imageObject.transform.SetParent(overlayRoot.transform,false);
  var rect=imageObject.AddComponent<RectTransform>(); rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=Vector2.zero; rect.offsetMax=Vector2.zero;
  overlayImage=imageObject.AddComponent<RawImage>(); overlayImage.color=Color.white; overlayImage.raycastTarget=false;
  shader = Shader.Find("HDRP/Unlit") ?? Shader.Find("HDRP/Lit") ?? Shader.Find("Unlit/Texture") ?? throw new Exception("No compatible scene shader found");
 }
 static int I(byte[] b, int at) => BinaryPrimitives.ReadInt32LittleEndian(b.AsSpan(at, 4));
 static float F(byte[] b, int at) => BitConverter.Int32BitsToSingle(I(b, at));
 static double D(byte[] b, int at) => BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(b.AsSpan(at, 8)));
 static Vector3 V(float x,float y,float z) => new(-x,y,z);
 static void Require(byte[] b, long bytes) { if (bytes < 0 || bytes > b.Length) throw new InvalidDataException("Truncated Minecraft geometry"); }
 static Texture2D Texture(int w, int h, byte[] b, int offset)
 {
  if (w < 1 || h < 1 || w > 16384 || h > 16384 || (long)w * h * 4 > 64L << 20) throw new InvalidDataException("Invalid texture dimensions");
  Require(b, (long)offset + w * h * 4);
  var texture = new Texture2D(w,h,TextureFormat.RGBA32,false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
  Upload(texture,b,offset,w*h*4); return texture;
 }
 static unsafe void Upload(Texture2D texture,byte[] bytes,int offset,int count) { fixed(byte* p=bytes) texture.LoadRawTextureData((IntPtr)(p+offset),count); texture.Apply(false,false); }
 public void Accept(RenderPacket packet, McState? player)
 {
  byte[] b=packet.Bytes;
  switch (packet.Kind) {
   case 1:
    Require(b,8); var next=Texture(I(b,0),I(b,4),b,8);
    if (atlas) Object.Destroy(atlas); atlas=next;
    atlasBytes=b.AsSpan(8,I(b,0)*I(b,4)*4).ToArray(); atlasDirty=false;
    foreach(var m in materials.Where(m=>m.Key.Texture==0)) SetTexture(m.Value,atlas);
    break;
   case 2:
    Require(b,16); int sx=I(b,0),sy=I(b,4),sz=I(b,8), count=I(b,12);
    string key=$"{sx}:{sy}:{sz}";
    if(sections.Remove(key,out var old)) { var mesh=old.GetComponent<MeshFilter>()?.sharedMesh; if(mesh) { meshes.Remove(mesh); Object.Destroy(mesh); } Object.Destroy(old); }
    if(count>0) sections[key]=Geometry(b,16,count,V(sx*16,sy*16,sz*16),0,"Section "+key);
    break;
   case 3: ClearWorld(); break;
   case 4:
    Require(b,16); int id=I(b,0); var tex=Texture(I(b,4),I(b,8),b,16);
    if(textures.Remove(id,out var prior)) Object.Destroy(prior); textures[id]=tex;
    foreach(var m in materials.Where(m=>m.Key.Texture==id)) SetTexture(m.Value,tex); break;
   case 5:
   case 6:
    int header=packet.Kind==5?8:32; Require(b,header);
    int batches=I(b,header-8), vertices=I(b,header-4);
    if(batches<0 || batches>2048 || vertices<0 || vertices>1000000) throw new InvalidDataException("Invalid entity mesh counts");
    Require(b,header+batches*16L+vertices*32L);
    // Each scene/avatar packet replaces its own group; Minecraft supplies its own entity renderer output.
    string tag=packet.Kind==5?"Avatar":"Entities";
    foreach(var item in dynamic.Where(o=>o && o.name.StartsWith(tag)).ToArray()) { var mesh=item.GetComponent<MeshFilter>()?.sharedMesh; if(mesh) { meshes.Remove(mesh); Object.Destroy(mesh); } Object.Destroy(item); dynamic.Remove(item); }
    Vector3 origin=packet.Kind==5 && player.HasValue?V((float)player.Value.X,(float)player.Value.Y,(float)player.Value.Z):packet.Kind==6?V((float)D(b,0),(float)D(b,8),(float)D(b,16)):Vector3.zero;
    for(int i=0;i<batches;i++) {
     int at=header+i*16, textureId=I(b,at),first=I(b,at+4),num=I(b,at+8);
     if(first<0 || num<0 || (long)first+num>vertices) throw new InvalidDataException("Invalid entity batch range");
     if(num>0) dynamic.Add(Geometry(b,header+batches*16+first*32,num,origin,textureId,tag+i));
    }
    break;
   case 7:
    Require(b,16); int x=I(b,0),y=I(b,4),w=I(b,8),h=I(b,12);
    if(atlas && atlasBytes!=null && x>=0 && y>=0 && w>0 && h>0 && x+(long)w<=atlas.width && y+(long)h<=atlas.height) {
     Require(b,16L+w*(long)h*4);
     for(int row=0;row<h;row++) Buffer.BlockCopy(b,16+row*w*4,atlasBytes,((y+row)*atlas!.width+x)*4,w*4);
     atlasDirty=true;
    }
    break;
  }
 }
 GameObject Geometry(byte[] b,int offset,int count,Vector3 position,int textureId,string name)
 {
  if(count<=0 || count>1000000 || count%3!=0) throw new InvalidDataException("Invalid Minecraft vertex count");
  Require(b,offset+(long)count*32);
  var vertices=new Vector3[count]; var uv=new Vector2[count]; var colors=new Color32[count]; var indices=new int[count];
  var groups=new Dictionary<MaterialKey,List<int>>();
  for(int i=0;i<count;i++) {
   int at=offset+i*32; float x=F(b,at),y=F(b,at+4),z=F(b,at+8);
   if(!float.IsFinite(x)||!float.IsFinite(y)||!float.IsFinite(z)) throw new InvalidDataException("Non-finite vertex");
   vertices[i]=V(x,y,z); uv[i]=new Vector2(F(b,at+12),F(b,at+16));
   colors[i]=new Color32(b[at+20],b[at+21],b[at+22],b[at+23]);
   indices[i]=i%3==1?i+1:i%3==2?i-1:i;
   if(i%3==2) {
    // HDRP/Unlit does not consume mesh vertex colours. Batch each face tint
    // explicitly so biome water, grass, hurt flashes and particles retain it.
    var a=colors[i-2]; var c=colors[i-1]; var d=colors[i];
    uint tint=0; int shift=0;
    foreach(int channel in new[]{(a.r+c.r+d.r)/3,(a.g+c.g+d.g)/3,(a.b+c.b+d.b)/3,(a.a+c.a+d.a)/3}) { tint|=(uint)Math.Clamp(((channel+8)/17)*17,0,255)<<shift; shift+=8; }
    var key=new MaterialKey(textureId,tint,(I(b,at+28)&2)!=0);
    if(!groups.TryGetValue(key,out var triangles)) groups[key]=triangles=new();
    triangles.Add(i-2); triangles.Add(i); triangles.Add(i-1);
   }
  }
  var mesh=new Mesh { indexFormat=IndexFormat.UInt32, vertices=vertices, uv=uv, colors32=colors, subMeshCount=groups.Count };
  var drawMaterials=new List<Material>(); int submesh=0;
  foreach(var group in groups) { mesh.SetTriangles(group.Value.ToArray(),submesh++,false); drawMaterials.Add(GetMaterial(group.Key)); }
  mesh.RecalculateBounds(); mesh.RecalculateNormals();
  var obj=new GameObject(name); obj.transform.SetParent(root.transform,false); obj.transform.position=position;
  obj.AddComponent<MeshFilter>().sharedMesh=mesh;
  var renderer=obj.AddComponent<MeshRenderer>();
  renderer.sharedMaterials=drawMaterials.ToArray();
  meshes.Add(mesh);
  return obj;
 }
 Material GetMaterial(MaterialKey key) {
  if(!materials.TryGetValue(key,out var material)) {
   material=new Material(shader); SetTexture(material,key.Texture!=0 && textures.TryGetValue(key.Texture,out var texture)?texture:atlas);
   var tint=new Color((key.Tint&255)/255f,((key.Tint>>8)&255)/255f,((key.Tint>>16)&255)/255f,((key.Tint>>24)&255)/255f).linear;
   if(material.HasProperty("_UnlitColor")) material.SetColor("_UnlitColor",tint);
   if(material.HasProperty("_BaseColor")) material.SetColor("_BaseColor",tint);
   if(material.HasProperty("_CullMode")) material.SetFloat("_CullMode",0);
   if(material.HasProperty("_AlphaCutoffEnable")) material.SetFloat("_AlphaCutoffEnable",1);
   if(material.HasProperty("_AlphaCutoff")) material.SetFloat("_AlphaCutoff",.1f);
   // HDRP compiles alpha clipping as a shader variant. Setting only the
   // inspector float leaves transparent villager clothing pixels opaque black.
   material.EnableKeyword("_ALPHATEST_ON");
   material.SetOverrideTag("RenderType","TransparentCutout");
   material.renderQueue=2450;
   if(key.Blend) {
    material.SetFloat("_SurfaceType",1); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
    material.SetFloat("_BlendMode",0); material.SetFloat("_SrcBlend",1); material.SetFloat("_DstBlend",10);
    material.SetFloat("_AlphaSrcBlend",1); material.SetFloat("_AlphaDstBlend",10);
    material.SetFloat("_ZWrite",0); material.SetFloat("_TransparentZWrite",0);
    material.SetShaderPassEnabled("DepthForwardOnly",false); material.SetShaderPassEnabled("MotionVectors",false);
    material.SetOverrideTag("RenderType","Transparent"); material.renderQueue=3000;
   }
   materials[key]=material;
  }
  return material;
 }
 static void SetTexture(Material material,Texture2D? texture)
 {
  if(!texture) return;
  if(material.HasProperty("_BaseColorMap")) material.SetTexture("_BaseColorMap",texture);
  if(material.HasProperty("_UnlitColorMap")) material.SetTexture("_UnlitColorMap",texture);
  if(material.HasProperty("_MainTex")) material.mainTexture=texture;
 }
 public void Accept(OverlayFrame frame)
 {
  if(!overlay || overlay.width!=frame.Width || overlay.height!=frame.Height) { if(overlay) Object.Destroy(overlay); overlay=new Texture2D(frame.Width,frame.Height,TextureFormat.RGBA32,false); }
  Upload(overlay!,frame.Pixels,0,frame.Pixels.Length); bottomUp=frame.BottomUp; OverlayFrames++;
  overlayImage.texture=overlay; overlayImage.uvRect=bottomUp?new Rect(0,0,1,1):new Rect(0,1,1,-1);
 }
 public void FlushTextures() {
  if(atlasDirty && atlas && atlasBytes!=null && Time.unscaledTime>atlasUploadAt) { Upload(atlas!,atlasBytes,0,atlasBytes.Length); atlasDirty=false; atlasUploadAt=Time.unscaledTime+.1f; }
 }
 public void DrawOverlay()
 {
  // The game strips unused IMGUI texture drawing overloads. Native uGUI RawImage handles the overlay.
 }
 public void ClearWorld()
 {
  foreach(var obj in sections.Values.Concat(dynamic)) if(obj) Object.Destroy(obj);
  sections.Clear(); dynamic.Clear();
  foreach(var mesh in meshes) if(mesh) Object.Destroy(mesh); meshes.Clear();
  foreach(var material in materials.Values) if(material) Object.Destroy(material); materials.Clear();
 }
 public void Dispose()
 {
  ClearWorld(); foreach(var tex in textures.Values) if(tex) Object.Destroy(tex); textures.Clear();
  if(atlas) Object.Destroy(atlas); if(overlay) Object.Destroy(overlay); if(root) Object.Destroy(root); if(overlayRoot) Object.Destroy(overlayRoot);
 }
}
