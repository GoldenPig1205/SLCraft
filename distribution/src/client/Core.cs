using HarmonyLib;
using Il2Cpp;
using Il2CppPlayerRoles.FirstPersonControl;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Text;

[assembly: MelonInfo(typeof(SLcraft.Client.Core), "SLcraft Client Passthrough", "0.2.6", "SLcraft contributors")]
[assembly: MelonGame("Northwood", "SCPSL")]

namespace SLcraft.Client;

public sealed class Core : MelonMod
{
 public static Core? Instance;
 SharedLink? link;
 WorldRenderer? renderer;
 readonly CollisionExporter collision=new();
 readonly NativeUiBridge nativeUi=new();
 readonly LinkSession session=new();
 readonly InventoryBlocker inventoryBlocker=new();
 string? smokeDir;
 int smokeStage,smokeTeleport;
 float smokeAt;
 int adminKeyFrame=-1;
 McState? state;
 FirstPersonMovementModule? module;
 Transform? camera;
 Vector3 feet;
 Vector3 safeFeet,originalCameraOffset;
 float originalFov;
 bool awaitingMenu,wasFocused=true;
 float menuDeadline;
 readonly Dictionary<KeyCode,float> repeatAt=new();
 static readonly HashSet<KeyCode> repeatKeys=new() {KeyCode.Backspace,KeyCode.Delete,KeyCode.LeftArrow,KeyCode.RightArrow,KeyCode.UpArrow,KeyCode.DownArrow,KeyCode.Tab};
 float yaw,pitch;
 int epoch=1,teleport=1;
 public bool Active => session.Connected;
 public bool NativePaused => session.NativeMenuOpen;
 public bool MinecraftMenuOpen => (state?.MenuOpen??false)||awaitingMenu;
 public bool OwnsMenuKeys => ReferenceHub.LocalHub && Il2CppMirror.NetworkServer.active;
 public bool NativeMenuKeyHandledThisFrame => adminKeyFrame==Time.frameCount;
 string status="F8: take Minecraft control | F10: release";
 readonly Dictionary<KeyCode,ushort> keys=new() {
  {KeyCode.W,26},{KeyCode.A,4},{KeyCode.S,22},{KeyCode.D,7},{KeyCode.Space,44},
  {KeyCode.LeftShift,225},{KeyCode.LeftControl,224},{KeyCode.E,8},{KeyCode.Q,20},
  {KeyCode.F,9},{KeyCode.R,21},{KeyCode.T,23},{KeyCode.Escape,41},{KeyCode.Return,40},
  {KeyCode.Backspace,42},{KeyCode.Tab,43},{KeyCode.Slash,56},
  {KeyCode.UpArrow,82},{KeyCode.DownArrow,81},{KeyCode.LeftArrow,80},{KeyCode.RightArrow,79},
  {KeyCode.Home,74},{KeyCode.End,77},{KeyCode.Delete,76},{KeyCode.KeypadEnter,88},
  {KeyCode.RightShift,229},{KeyCode.RightControl,228},{KeyCode.LeftAlt,226},{KeyCode.RightAlt,230},
  {KeyCode.C,6},{KeyCode.V,25},{KeyCode.X,27},{KeyCode.Z,29},
  {KeyCode.Alpha1,30},{KeyCode.Alpha2,31},
  {KeyCode.Alpha3,32},{KeyCode.Alpha4,33},{KeyCode.Alpha5,34},{KeyCode.Alpha6,35},
  {KeyCode.Alpha7,36},{KeyCode.Alpha8,37},{KeyCode.Alpha9,38},{KeyCode.Alpha0,39}
 };
 string? testDir;
 bool autoHost;
 float started;
 float roleReadyAt;
 bool triedHost,triedCreate,triedRole,testActivated,testShot;
 float nextTestStatus;
 public override void OnInitializeMelon()
 {
  Instance=this;
  try {
   if(!Environment.GetCommandLineArgs().Contains("-noauth") || !File.Exists(Path.Combine(AppContext.BaseDirectory,".anomaly_install.json")))
    throw new Exception("Launch the isolated offline-mod client with -noauth. The Steam client is not an install target.");
   Application.runInBackground=true;
   link=new SharedLink();
   testDir=Environment.GetEnvironmentVariable("SLCRAFT_CLIENT_TEST")??Environment.GetEnvironmentVariable("SLCRAFT_ENTITY_TEST"); started=Time.realtimeSinceStartup;
   autoHost=testDir!=null || Environment.GetEnvironmentVariable("SLCRAFT_LOCAL_HOST")=="1";
   smokeDir=Environment.GetEnvironmentVariable("SLCRAFT_SMOKE_TEST")??Environment.GetEnvironmentVariable("SLCRAFT_ENTITY_TEST");
   MelonLogger.Msg("SLcraft: client IPC ready; Minecraft original mesh/HUD protocol v11.");
  } catch(Exception e) { status=e.Message; MelonLogger.Error(e); }
 }
 public override void OnUpdate()
 {
  if(link==null) return;
  try {
   link.Heartbeat();
   if(autoHost) TestStartup();
   if(LocalHostAdmin.IsOwnOfflineHost) LocalHostAdmin.Ensure();
   state=link.PlayerState();
   if(Application.isFocused) {
    HandleRemoteAdminKey();
    if(Input.GetKeyDown(KeyCode.F8)) Toggle();
    if(Input.GetKeyDown(KeyCode.F10)) Release();
   }
   bool wasPaused=NativePaused;
   session.ObserveNativeMenu(NativeMenus.AnyVisible);
   if(NativePaused!=wasPaused) {
    link.InputEvent(6); repeatAt.Clear();
    status=NativePaused?"SL menu open | close it to return to Minecraft":"v0.2.6 | E inventory | / Minecraft commands | M Remote Admin | F10 release";
    MelonLogger.Msg("SLcraft native menu suspended="+NativePaused+"; connected="+Active);
   }
   var currentModule=ReferenceHub.LocalHub?.roleManager?.CurrentRole?.TryCast<FpcStandardRoleBase>()?.FpcModule;
   if(Active && !link.MinecraftAlive) { Release(); status="Minecraft process stopped; control released. F8: reconnect."; }
   if(Active && currentModule && (module==null || !module || currentModule.Pointer!=module.Pointer)) RebindRole(currentModule);
   else if(Active && NativePaused && currentModule && (currentModule.Motor.Position-feet).sqrMagnitude>1) RebindRole(currentModule);
   if(Active && !currentModule) { module=null; camera=null; status="Waiting for an SL first-person role; Minecraft remains connected."; }
   if(Active) {
    bool menu=state?.MenuOpen??false;
    if(menu || Time.unscaledTime>menuDeadline) awaitingMenu=false;
    if(Application.isFocused && !menu && (Input.GetKeyDown(KeyCode.E)||Input.GetKeyDown(KeyCode.T)||Input.GetKeyDown(KeyCode.Slash))) { awaitingMenu=true; menuDeadline=Time.unscaledTime+2; }
    if(!FreeCursor) { yaw+=Input.GetAxisRaw("Mouse X")*2; pitch=Mathf.Clamp(pitch-Input.GetAxisRaw("Mouse Y")*2,-89,89); }
    if(!NativePaused && module && link.MinecraftResponsive) ForwardInput(menu);
    collision.Tick(link,feet);
    ApplyCursor();
   }
   int width=Math.Clamp(Screen.width,1,1920),height=Math.Clamp(Screen.height,1,1080);
   link.HostState(Active,NativePaused || (Active && (!module || !link.MinecraftResponsive)),SceneManager.GetActiveScene().handle,epoch,-feet.x,feet.y,feet.z,yaw,pitch,teleport,width,height);
   renderer??=new WorldRenderer();
   renderer.Present(Active,session.ControlsMinecraft);
   nativeUi.Tick(Active);
   inventoryBlocker.Tick(Active);
   foreach(var packet in link.RenderMessages()) renderer.Accept(packet,state);
   renderer.FlushTextures();
   var overlay=link.Overlay(); if(overlay.HasValue) renderer.Accept(overlay.Value);
   SmokeChecks();
  } catch(Exception e) { status="SLcraft error: "+e.Message; MelonLogger.Error(e); Release(); }
 }
 void RebindRole(FirstPersonMovementModule current)
 {
  module=current; camera=ReferenceHub.LocalHub.PlayerCameraReference;
  feet=module.Motor.Position; safeFeet=feet;
  yaw=camera.eulerAngles.y; pitch=camera.eulerAngles.x; if(pitch>180) pitch-=360;
  originalCameraOffset=camera.position-feet;
  teleport++; collision.Rescan(); link!.InputEvent(6);
  MelonLogger.Msg("SLcraft: rebound changed native role without disconnecting Minecraft; teleport="+teleport);
 }
 void Toggle()
 {
  if(Active) {
   if(NativePaused) {
    NativeMenus.CloseAdmin(); Il2CppToggleableMenus.ToggleableMenuController.HideCurrent();
    session.ObserveNativeMenu(false); link?.InputEvent(6); status="Minecraft resumed | M: Remote Admin";
   } else Release();
   return;
  }
  if(link==null || !link.MinecraftAlive) { status="Start the SLcraft Minecraft client first."; return; }
  var hub=ReferenceHub.LocalHub;
  var role=hub?.roleManager?.CurrentRole?.TryCast<FpcStandardRoleBase>();
  if(role==null || !role.FpcModule) { status="A living first-person role is required. Host a local offline game first."; return; }
  if(!Il2CppMirror.NetworkServer.active) { status="This build supports the local offline host; remote servers require server cooperation."; return; }
  // F8 explicitly returns control from native menus. Clear their native state as
  // well as their visuals so an old console flag cannot immediately release us.
  Il2CppToggleableMenus.ToggleableMenuController.HideCurrent();
  var nativeConsole=Il2CppGameCore.Console.Singleton; if(nativeConsole) nativeConsole.IsEnabled=false;
  var nativeAdmin=Il2CppRemoteAdmin.RemoteAdminUIController.Singleton; if(nativeAdmin) nativeAdmin.IsEnabled=false;
  var nativeMenu=GameMenu.singleton; if(nativeMenu) nativeMenu.IsEnabled=false;
  module=role.FpcModule; camera=hub!.PlayerCameraReference;
  feet=module.Motor.Position; yaw=camera.eulerAngles.y;
  safeFeet=feet; originalCameraOffset=camera.position-feet;
  var unityCamera=camera.GetComponent<Camera>(); originalFov=unityCamera?unityCamera.fieldOfView:70;
  pitch=camera.eulerAngles.x; if(pitch>180) pitch-=360;
  teleport++; epoch++; collision.Reset(link,epoch);
  session.Begin();
  awaitingMenu=false;
  status="v0.2.6 | E: inventory | T or /: Minecraft chat | M: Remote Admin | ~: SL console | F10: release";
  MelonLogger.Msg("SLcraft activated at "+feet);
  var nearby=Physics.OverlapBox(feet,Vector3.one*3,Quaternion.identity,~0,QueryTriggerInteraction.Ignore);
  MelonLogger.Msg("SLcraft native colliders at spawn: "+nearby.Length+" "+string.Join("; ",nearby.Take(12).Select(c=>c?c.name:"null")));
 }
 void ForwardInput(bool menu)
 {
  if(link==null) return;
  if(!Application.isFocused) { if(wasFocused) link.InputEvent(6); wasFocused=false; return; }
  wasFocused=true;
  foreach(var key in keys) {
   if(Input.GetKeyDown(key.Key)) { link.InputEvent(1,key.Value,1); repeatAt[key.Key]=Time.unscaledTime+.4f; }
   else if(menu && repeatKeys.Contains(key.Key) && Input.GetKey(key.Key) && repeatAt.TryGetValue(key.Key,out var repeat) && Time.unscaledTime>=repeat) { link.InputEvent(1,key.Value,1); repeatAt[key.Key]=Time.unscaledTime+.05f; }
   if(Input.GetKeyUp(key.Key)) { link.InputEvent(1,key.Value,0); repeatAt.Remove(key.Key); }
  }
  for(int i=0;i<3;i++) {
   ushort code=(ushort)(i==0?1:i==1?3:2);
   if(Input.GetMouseButtonDown(i)) link.InputEvent(2,code,1);
   if(Input.GetMouseButtonUp(i)) link.InputEvent(2,code,0);
  }
  float scroll=Input.mouseScrollDelta.y; if(scroll!=0) link.InputEvent(3,0,(int)(scroll*120));
  if(menu) {
   var mouse=Input.mousePosition;
   link.InputEvent(4,0,(int)(mouse.x* Math.Min(Screen.width,1920)/Screen.width),(int)((Screen.height-mouse.y)*Math.Min(Screen.height,1080)/Screen.height));
   foreach(var rune in Input.inputString.EnumerateRunes()) if(!Rune.IsControl(rune)) link.InputEvent(5,0,rune.Value);
  }
 }
 public override void OnLateUpdate()
 {
  if(Active) ApplyCursor();
  nativeUi.Tick(Active);
  inventoryBlocker.Tick(Active);
  if(!session.ControlsMinecraft || link?.MinecraftResponsive!=true || module==null || !module || !camera || !state.HasValue || !state.Value.InWorld || state.Value.TeleportAck!=teleport) return;
  var mc=state.Value;
  var next=new Vector3(-(float)mc.X,(float)mc.Y,(float)mc.Z);
  if(!float.IsFinite(next.x)||!float.IsFinite(next.y)||!float.IsFinite(next.z)) {
   module.Motor.Position=safeFeet; feet=safeFeet;
   camera!.position=safeFeet+originalCameraOffset;
   Release(); status="Invalid coordinate received; returned to last grounded position. F8: retry."; return;
  }
  // Empty space is valid collision data. Do not pin the host at the map edge.
  if(!MovementPolicy.CanApply(mc.X,mc.Y,mc.Z,collision.SentRegions)) return;
  feet=next;
  if((mc.Flags&4)!=0) safeFeet=feet;
  module.Motor.Position=feet;
  camera!.position=new Vector3(-(float)mc.EyeX,(float)mc.EyeY,(float)mc.EyeZ);
  camera.rotation=Quaternion.Euler(pitch,yaw,0);
  var unityCamera=camera.GetComponent<Camera>(); if(unityCamera && mc.Fov>10 && mc.Fov<150) unityCamera.fieldOfView=mc.Fov;
  if(testDir!=null && renderer!=null && renderer.OverlayFrames>60 && collision.SentRegions>1 && !testShot) {
   testShot=true; Directory.CreateDirectory(testDir);
   File.WriteAllText(Path.Combine(testDir,"client-status.txt"),$"Minecraft linked; sections={renderer.SectionCount}; HUD frames={renderer.OverlayFrames}; collision regions={collision.SentRegions}; collision blocks={collision.LastSolidBlocks}; position={feet}");
  }
 }
 public override void OnGUI()
 {
  if(Active) renderer?.DrawOverlay();
  GUI.Label(new Rect(12,12,1000,30),"SLcraft: "+status);
 }
 public void Release()
 {
  if(Active) link?.InputEvent(6);
  if(camera) { var native=camera!.GetComponent<Camera>(); if(native && originalFov>0) native.fieldOfView=originalFov; }
  session.End(); awaitingMenu=false; repeatAt.Clear(); renderer?.SetHidden(); nativeUi.Tick(false); inventoryBlocker.Restore(); module=null; camera=null;
  Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
 }
 public bool FreeCursor => NativePaused||MinecraftMenuOpen||!Application.isFocused;
 public bool HandleRemoteAdminKey()
 {
  bool closing=NativeMenus.AdminVisible && Input.GetKeyDown(KeyCode.Escape);
  if((!Input.GetKeyDown(KeyCode.M) && !closing) || !OwnsMenuKeys) return false;
  if(adminKeyFrame==Time.frameCount) return true;
  var admin=Il2CppRemoteAdmin.RemoteAdminUIController.Singleton;
  if(!admin) return false;
  // M in a text field belongs to that field. Also suppress SL's normal M binding
  // while typing in Minecraft, where native UI has no knowledge of text focus.
  if(!closing && (MinecraftMenuOpen||NativeMenus.ConsoleVisible||NativeMenus.AdminTyping)) return true;
  adminKeyFrame=Time.frameCount;
  ToggleAdminUi();
  return true;
 }
 void ToggleAdminUi()
 {
  if(NativeMenus.AdminVisible) NativeMenus.CloseAdmin();
  else if(!NativeMenus.OpenAdmin()) { status="Remote Admin did not open; Minecraft remains connected."; MelonLogger.Warning(status); return; }
  session.ObserveNativeMenu(NativeMenus.AnyVisible);
  link?.InputEvent(6);
  renderer?.Present(Active,session.ControlsMinecraft);
  status=NativePaused?"SL Remote Admin | M / Esc: close and return to Minecraft":"Minecraft resumed | M: Remote Admin";
  MelonLogger.Msg("SLcraft M: adminVisible="+NativeMenus.AdminVisible+"; connected="+Active+"; paused="+NativePaused);
 }
 void SmokeChecks()
 {
  bool entityTest=Environment.GetEnvironmentVariable("SLCRAFT_ENTITY_TEST")!=null;
  if(smokeDir==null || smokeStage==6 || (smokeStage==3 && !entityTest) || !Active || state?.TeleportAck!=teleport || collision.LastSolidBlocks==0) return;
  if(smokeAt==0) { smokeAt=Time.unscaledTime+8; return; }
  if(Time.unscaledTime<smokeAt) return;
  Directory.CreateDirectory(smokeDir);
  void Report(string message) { File.AppendAllText(Path.Combine(smokeDir,"menu-smoke.txt"),message+Environment.NewLine); MelonLogger.Msg("SLcraft smoke "+message); }
  if(smokeStage==0) {
   Report($"ADMIN: native={ReferenceHub.LocalHub?.serverRoles?.RemoteAdmin}; loggedIn={Il2CppRemoteAdmin.RemoteAdminUIController.Singleton?.LoggedIn}; command={Il2CppGameCore.Console.Singleton.TypeCommand("adminme")}");
   Report($"RENDER: hasAtlas={renderer?.HasAtlas}; {renderer?.AtlasStatus}");
   smokeTeleport=teleport; ToggleAdminUi();
   Report($"OPEN: visible={NativeMenus.AdminVisible}; connected={Active}; paused={NativePaused}; inventory={Il2CppInventorySystem.GUI.InventoryGuiController.InventoryVisible}");
   smokeStage=1; smokeAt=Time.unscaledTime+3;
  } else if(smokeStage==1) {
   Report($"HOLD: visible={NativeMenus.AdminVisible}; cursor={Cursor.lockState}; connected={Active}; teleportUnchanged={teleport==smokeTeleport}");
   NativeMenus.CloseAdmin(); session.ObserveNativeMenu(NativeMenus.AnyVisible);
   smokeStage=2; smokeAt=Time.unscaledTime+3;
  } else if(smokeStage==2) {
   Report($"RESUME: controls={session.ControlsMinecraft}; connected={Active}; teleportUnchanged={teleport==smokeTeleport}; inventory={Il2CppInventorySystem.GUI.InventoryGuiController.InventoryVisible}"); smokeStage=3;
  } else if(smokeStage==3) {
   NativeMenus.OpenAdmin(); session.ObserveNativeMenu(true);
   ReferenceHub.LocalHub.roleManager.ServerSetRole(Il2CppPlayerRoles.RoleTypeId.Tutorial,Il2CppPlayerRoles.RoleChangeReason.RemoteAdmin,Il2CppPlayerRoles.RoleSpawnFlags.All);
   smokeStage=4; smokeAt=Time.unscaledTime+5;
  } else if(smokeStage==4) {
   Report($"ROLE CHANGE: connected={Active}; role={ReferenceHub.LocalHub.roleManager.CurrentRole.RoleTypeId}; admin={NativeMenus.AdminVisible}; ack={state?.TeleportAck==teleport}");
   ReferenceHub.LocalHub.roleManager.ServerSetRole(Il2CppPlayerRoles.RoleTypeId.ClassD,Il2CppPlayerRoles.RoleChangeReason.RemoteAdmin,Il2CppPlayerRoles.RoleSpawnFlags.All);
   NativeMenus.CloseAdmin(); session.ObserveNativeMenu(false);
   smokeStage=5; smokeAt=Time.unscaledTime+5;
  } else {
   Report($"ROLE RETURN: connected={Active}; controls={session.ControlsMinecraft}; role={ReferenceHub.LocalHub.roleManager.CurrentRole.RoleTypeId}; ack={state?.TeleportAck==teleport}"); smokeStage=6;
  }
 }
 public void RefreshNativeUi() => nativeUi.Tick(Active);
 public void HideNativeHotkeys(CanvasGroup group) { if(Active) inventoryBlocker.Hide(group); }
 public void ApplyCursor() {
  var desired=FreeCursor?CursorLockMode.None:CursorLockMode.Locked;
  if(Cursor.lockState!=desired) Cursor.lockState=desired;
  Cursor.visible=FreeCursor;
 }
 public override void OnSceneWasUnloaded(int index,string name) {
  Release(); nativeUi.Restore(); epoch++;
  // Minecraft sent its atlas before Facility loaded. The persistent renderer
  // must retain that atlas (and texture IDs) across native SL scene changes.
 }
 public override void OnDeinitializeMelon() { Release(); nativeUi.Restore(); renderer?.Dispose(); link?.Dispose(); Instance=null; }
 void TestStartup()
 {
  float elapsed=Time.realtimeSinceStartup-started;
  if(!triedHost && elapsed>5 && Il2CppGameCore.Console.Singleton) {
   triedHost=true; MelonLogger.Msg("SLcraft test host: "+Il2CppGameCore.Console.Singleton.TypeCommand("host"));
  }
  if(triedHost && !triedCreate && elapsed>35 && SceneManager.GetActiveScene().name=="NewMainMenu" && CustomNetworkManager.TypedSingleton) {
   triedCreate=true; MelonLogger.Msg("SLcraft test: creating local non-Steam host");
   CustomNetworkManager.TypedSingleton.NonsteamHost();
  }
  if(triedHost && elapsed>18 && SceneManager.GetActiveScene().name=="Facility" && ReferenceHub.LocalHub && Il2CppMirror.NetworkServer.active && !triedRole) {
   triedRole=true; ReferenceHub.LocalHub.roleManager.ServerSetRole(Il2CppPlayerRoles.RoleTypeId.ClassD,Il2CppPlayerRoles.RoleChangeReason.RemoteAdmin,Il2CppPlayerRoles.RoleSpawnFlags.All);
   roleReadyAt=elapsed+1;
   Il2CppGameCore.Console.DisplayCreateServer=false;
  }
  if(triedRole && elapsed>roleReadyAt && !testActivated && link!=null && link.MinecraftAlive) { Toggle(); testActivated=Active; }
  if(testDir!=null && elapsed>nextTestStatus) {
   nextTestStatus=elapsed+5; Directory.CreateDirectory(testDir!);
   File.WriteAllText(Path.Combine(testDir!,"progress.txt"),$"elapsed={elapsed}; scene={SceneManager.GetActiveScene().name}; mc={link?.MinecraftAlive}; host={Il2CppMirror.NetworkServer.active}; role={ReferenceHub.LocalHub?.roleManager?.CurrentRole?.RoleTypeId}; active={Active}; collision={collision.SentRegions}/{collision.LastSolidBlocks}; position={feet}; mcPosition={state?.X},{state?.Y},{state?.Z}; ack={state?.TeleportAck}/{teleport}; menu={state?.MenuOpen}; cursor={Cursor.lockState}; status={status}");
  }
 }
}

[HarmonyPatch(typeof(Il2CppCommandSystem.Commands.Console.AdminMeCommand),nameof(Il2CppCommandSystem.Commands.Console.AdminMeCommand.Execute))]
static class LocalAdminMePatch
{
 static bool Prefix(ref string response,ref bool __result) {
  if(!LocalHostAdmin.IsOwnOfflineHost) return true;
  __result=LocalHostAdmin.Ensure();
  response=__result?"SLcraft local host administrator access granted.":"SLcraft local host is still initializing; try adminme again.";
  return false;
 }
}

[HarmonyPatch(typeof(Il2CppToggleableMenus.ToggleableMenuController),nameof(Il2CppToggleableMenus.ToggleableMenuController.HandleBaseMenu))]
static class AdminMenuKeyPatch
{
 static bool Prefix(Il2CppToggleableMenus.ToggleableMenuBase menu) {
  var core=Core.Instance; if(core?.OwnsMenuKeys!=true) return true;
  if(menu.TryCast<Il2CppRemoteAdmin.RemoteAdminUIController>()!=null) return false;
  if(menu.TryCast<Il2CppInventorySystem.GUI.InventoryGuiController>()!=null && core.Active) return false;
  if(menu.TryCast<GameMenu>()!=null && (core.MinecraftMenuOpen || ((NativeMenus.AdminVisible || core.NativeMenuKeyHandledThisFrame) && Input.GetKeyDown(KeyCode.Escape)))) return false;
  return true;
 }
}

[HarmonyPatch(typeof(Il2CppToggleableMenus.ToggleableMenuController),nameof(Il2CppToggleableMenus.ToggleableMenuController.HandleHoldableMenu))]
static class InventoryHoldKeyPatch
{
 static bool Prefix(Il2CppToggleableMenus.ToggleableMenuBase menu) => Core.Instance?.Active!=true || menu.TryCast<Il2CppInventorySystem.GUI.InventoryGuiController>()==null;
}
[HarmonyPatch(typeof(Il2CppInventorySystem.GUI.InventoryGuiController),nameof(Il2CppInventorySystem.GUI.InventoryGuiController.CanInventoryBeDisplayed))]
static class InventoryVisibilityPatch
{
 static bool Prefix(ref bool __result) { if(Core.Instance?.Active!=true) return true; __result=false; return false; }
}
[HarmonyPatch(typeof(Il2CppInventorySystem.Hotkeys.HotkeyController),nameof(Il2CppInventorySystem.Hotkeys.HotkeyController.OnUpdate))]
static class InventoryHotkeyPatch
{
 static bool Prefix() => Core.Instance?.Active!=true;
}
[HarmonyPatch(typeof(Il2CppInventorySystem.Hotkeys.HotkeyHud),nameof(Il2CppInventorySystem.Hotkeys.HotkeyHud.Update))]
static class InventoryHotkeyHudPatch
{
 static void Postfix(Il2CppInventorySystem.Hotkeys.HotkeyHud __instance) { if(__instance._fadeGroup) Core.Instance?.HideNativeHotkeys(__instance._fadeGroup); }
}

[HarmonyPatch(typeof(Il2CppGameCore.RoundStart),nameof(Il2CppGameCore.RoundStart.Update))]
static class WaitingUiPatch
{
 static void Postfix() => Core.Instance?.RefreshNativeUi();
}

[HarmonyPatch(typeof(Il2CppCursorManagement.CursorManager),nameof(Il2CppCursorManagement.CursorManager.LateUpdate))]
static class CursorPatch
{
 static bool Prefix() {
  var core=Core.Instance; if(core?.Active!=true) return true;
  core.ApplyCursor(); return false;
 }
}
[HarmonyPatch(typeof(Il2CppCursorManagement.CursorManager),nameof(Il2CppCursorManagement.CursorManager.SetLockMode))]
static class CursorModePatch
{
 static void Prefix(ref CursorLockMode lockMode) {
  if(Core.Instance?.Active==true) lockMode=Core.Instance.FreeCursor?CursorLockMode.None:CursorLockMode.Locked;
 }
}

[HarmonyPatch(typeof(FpcMotor),nameof(FpcMotor.UpdatePosition))]
static class MovementPatch
{
 static bool Prefix(FpcMotor __instance,ref bool sendJump)
 {
  if(Core.Instance?.Active!=true || __instance.Hub!=ReferenceHub.LocalHub) return true;
  sendJump=false; return false;
 }
}
[HarmonyPatch(typeof(FpcMouseLook),nameof(FpcMouseLook.UpdateRotation))]
static class LookPatch
{
 static bool Prefix(FpcMouseLook __instance) => Core.Instance?.Active!=true || __instance._hub!=ReferenceHub.LocalHub;
}

