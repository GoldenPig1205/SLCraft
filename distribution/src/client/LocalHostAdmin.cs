using Il2Cpp;
using MelonLoader;

namespace SLcraft.Client;

// The isolated process hosts this game itself. Assign its local owner through
// SL's server permission API; this path is unavailable on a remote-server client.
public static class LocalHostAdmin
{
 static IntPtr preparedHub;
 static float nextRetry;
 public static bool IsOwnOfflineHost => Core.Instance!=null && Environment.GetCommandLineArgs().Contains("-noauth")
  && Il2CppMirror.NetworkServer.active && Il2CppMirror.NetworkClient.active
  && ReferenceHub.LocalHub && ReferenceHub.LocalHub.isLocalPlayer;
 public static bool Ensure()
 {
  if(UnityEngine.Time.unscaledTime<nextRetry) return false;
  try { return Configure(); }
  catch(Exception e) { nextRetry=UnityEngine.Time.unscaledTime+2; MelonLogger.Warning("SLcraft host permissions still initializing: "+e.Message); return false; }
 }
 public static void InitializePermissions()
 {
  if(Core.Instance==null || !Environment.GetCommandLineArgs().Contains("-noauth") || !Il2CppMirror.NetworkServer.active) return;
  // NonsteamHost skips dedicated-server startup, including the RA permission
  // handler. Keep its configuration entirely inside this isolated game copy.
  if(ServerStatic.PermissionsHandler==null) {
   var root=System.IO.Path.GetDirectoryName(Environment.ProcessPath)!;
   var folder=System.IO.Path.Combine(root,"SLcraft");
   System.IO.Directory.CreateDirectory(folder);
   var path=System.IO.Path.Combine(folder,"config_remoteadmin.txt");
   if(!System.IO.File.Exists(path))
    System.IO.File.Copy(System.IO.Path.Combine(root,"ConfigTemplates","config_remoteadmin.template.txt"),path);
   var config=new YamlConfig(path);
   YamlConfig shared=null!, members=null!;
   var handler=new PermissionsHandler(ref config,ref shared,ref members);
   ServerStatic.RolesConfig=config;
   ServerStatic.RolesConfigPath=path;
   ServerStatic.PermissionsHandler=handler;
   MelonLogger.Msg("SLcraft: initialized isolated local-host RA permission configuration.");
  }
 }
 static bool Configure()
 {
  if(!IsOwnOfflineHost) return false;
  var hub=ReferenceHub.LocalHub; var roles=hub.serverRoles;
  if(!roles) return false;
  InitializePermissions();
  if(preparedHub!=hub.Pointer || !roles.RemoteAdmin) {
   var group=new UserGroup { Name="slcraft-local-owner",BadgeText="SLcraft Host",BadgeColor="silver",Permissions=ulong.MaxValue,KickPower=255,RequiredKickPower=255,Cover=false,HiddenByDefault=false };
   roles.SetGroup(group,true);
   if(!roles.RemoteAdmin) return false;
   preparedHub=hub.Pointer;
   MelonLogger.Msg("SLcraft: local offline host administrator configured through ServerRoles.SetGroup.");
  }
  // Some client-host builds do not deliver their own TargetRPC back to the UI.
  // Reflect the permission state already granted by the authoritative local host.
  var admin=Il2CppRemoteAdmin.RemoteAdminUIController.Singleton;
  if(admin && !admin.LoggedIn && roles.RemoteAdmin) admin.SetRemoteAdminState(true);
  return roles.RemoteAdmin && roles.Permissions!=0;
 }
}

// QueryProcessor.Start builds the real command list and sender from this handler.
// Initializing only on the next frame leaves Start half-complete and its RPC
// subsequently disconnects the host when it receives a null command list.
[HarmonyLib.HarmonyPatch(typeof(Il2CppRemoteAdmin.QueryProcessor),nameof(Il2CppRemoteAdmin.QueryProcessor.Start))]
static class LocalPermissionStartupPatch
{
 static void Prefix() => LocalHostAdmin.InitializePermissions();
}
