using Il2Cpp;
using Il2CppRemoteAdmin;
using Il2CppToggleableMenus;
using UnityEngine;

namespace SLcraft.Client;

public static class NativeMenus
{
 static bool Visible(SimpleToggleableMenu? menu) => menu && menu!.IsEnabled && menu._targetRoot && menu._targetRoot.activeInHierarchy;
 public static bool AdminVisible {
  get { var admin=RemoteAdminUIController.Singleton; return admin && admin.IsEnabled && admin.UIRoot && admin.UIRoot.activeInHierarchy && admin.RACanvas && admin.RACanvas.gameObject.activeInHierarchy && admin.RACanvas.enabled; }
 }
 public static bool AnyVisible => AdminVisible || Visible(Il2CppGameCore.Console.Singleton) || Visible(GameMenu.singleton);
 public static bool AdminTyping => AdminVisible && RemoteAdminUIController.Singleton.IsAnyInputFieldFocused();
 public static bool ConsoleVisible => Visible(Il2CppGameCore.Console.Singleton);
 public static bool OpenAdmin()
 {
  var admin=RemoteAdminUIController.Singleton;
  if(!admin) return false;
  LocalHostAdmin.Ensure();
  // These are UI visibility changes only. LoggedIn, server permissions and the
  // password/login response remain under SCP:SL's normal authentication code.
  ToggleableMenuController.ForceCurrent(admin.Cast<IRegisterableMenu>());
  admin.IsEnabled=true;
  admin.OnToggled();
  if(admin.UIRoot) admin.UIRoot.SetActive(true);
  if(admin.RACanvas) { admin.RACanvas.gameObject.SetActive(true); admin.RACanvas.enabled=true; }
  if(!admin.LoggedIn) {
   if(admin.RootLogin) admin.RootLogin.SetActive(true);
   if(admin.RootPanel) admin.RootPanel.SetActive(false);
   if(admin.RootTbra) admin.RootTbra.SetActive(false);
  }
  EnableAncestors(admin.UIRoot?.transform);
  EnableAncestors(admin.RACanvas?.transform);
  MelonLoader.MelonLogger.Msg("SLcraft RA visibility: "+Describe(admin));
  return AdminVisible;
 }
 static void EnableAncestors(Transform? node) { for(int i=0;node && i<32;i++,node=node!.parent) if(!node!.gameObject.activeSelf) node.gameObject.SetActive(true); }
 static string Describe(RemoteAdminUIController admin) {
  var chain=new List<string>();
  for(Transform? t=admin.UIRoot?.transform;t && chain.Count<12;t=t!.parent) chain.Add(t!.name+":"+t.gameObject.activeSelf);
  return $"flag={admin.IsEnabled}; root={admin.UIRoot?.activeInHierarchy}; canvas={admin.RACanvas?.enabled}; loggedIn={admin.LoggedIn}; path="+string.Join("/",chain);
 }
 public static void CloseAdmin()
 {
  var admin=RemoteAdminUIController.Singleton; if(!admin) return;
  admin.IsEnabled=false; admin.OnToggled();
  if(admin.UIRoot) admin.UIRoot.SetActive(false);
  if(admin.RACanvas) admin.RACanvas.enabled=false;
  if(ToggleableMenuController._currentMenu?.Pointer==admin.Pointer) ToggleableMenuController.HideCurrent();
 }
}
