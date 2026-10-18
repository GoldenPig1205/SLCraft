using Il2Cpp;
using Il2CppGameCore;
using Il2CppPlayerRoles.FirstPersonControl;
using UnityEngine;

namespace SLcraft.Client;

// A living local-host player is already in the facility. Hide the obsolete host
// prompt and waiting-room widgets; console and Remote Admin canvases stay intact.
public sealed class NativeUiBridge
{
 readonly Dictionary<GameObject,bool> hidden=new();
 public void Tick(bool active)
 {
  var hub=ReferenceHub.LocalHub;
  bool localGame=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="Facility" && Il2CppMirror.NetworkServer.active && hub && hub.roleManager?.CurrentRole?.TryCast<FpcStandardRoleBase>()!=null;
  if(!localGame) { Restore(); return; }
  Il2CppGameCore.Console.DisplayCreateServer=false;
  var manager=CustomNetworkManager.TypedSingleton;
  if(manager) {
   // DisplayCreateServer does not close an already opened network-manager popup.
   if(manager.createpop) manager.createpop.SetActive(false);
   if(manager.createPopForce) manager.createPopForce.SetActive(false);
   if(manager.loadingpop) manager.loadingpop.SetActive(false);
  }
  var round=RoundStart.singleton;
  if(!round) return;
  Hide(round.window);
  Hide(round.forceButton);
  if(round.startsIn) Hide(round.startsIn.gameObject);
  if(round.playersNumber) Hide(round.playersNumber.gameObject);
  if(round.loadingbar) Hide(round.loadingbar.gameObject);
  if(round._background) Hide(round._background.gameObject);
 }
 void Hide(GameObject? target)
 {
  if(!target) return;
  // A waiting-room reference can point at a shared UI ancestor. Never disable
  // an ancestor containing an interactive native console or Remote Admin.
  var admin=Il2CppRemoteAdmin.RemoteAdminUIController.Singleton;
  if(admin && admin.UIRoot && admin.UIRoot.transform.IsChildOf(target!.transform)) return;
  var console=Il2CppGameCore.Console.Singleton;
  if(console && console._targetRoot && console._targetRoot.transform.IsChildOf(target!.transform)) return;
  if(!hidden.ContainsKey(target!)) hidden[target!]=target!.activeSelf;
  if(target!.activeSelf) target.SetActive(false);
 }
 public void Restore()
 {
  foreach(var item in hidden) if(item.Key) item.Key.SetActive(item.Value);
  hidden.Clear();
 }
}
