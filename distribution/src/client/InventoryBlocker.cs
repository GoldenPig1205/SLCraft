using Il2CppInventorySystem.GUI;
using UnityEngine;

namespace SLcraft.Client;

public sealed class InventoryBlocker
{
 readonly Dictionary<CanvasGroup,(float alpha,bool blocks,bool interactive)> groups=new();
 Il2CppInventorySystem.Inventory? heldInventory;
 ushort previousSerial;
 public void Tick(bool connected)
 {
  if(!connected) { Restore(); return; }
  var local=Il2Cpp.ReferenceHub.LocalHub?.inventory;
  if(local && Il2CppMirror.NetworkServer.active) {
   if(heldInventory?.Pointer!=local.Pointer) {
    heldInventory=local; previousSerial=local.CurItem.SerialNumber;
   }
   if(local.CurItem.SerialNumber!=0) local.ServerSelectItem(0);
  }
  var inventory=InventoryGuiController.Singleton;
  if(!inventory) return;
  if(inventory.IsEnabled) inventory.IsEnabled=false;
  if(InventoryGuiController.InventoryVisible) InventoryGuiController.InventoryVisible=false;
  var next=inventory._toggleablePart;
  if(!next) return;
  Hide(next);
 }
 public void Hide(CanvasGroup group) {
  if(!group) return;
  if(!groups.ContainsKey(group)) groups[group]=(group.alpha,group.blocksRaycasts,group.interactable);
  group.alpha=0; group.blocksRaycasts=false; group.interactable=false;
 }
 public void Restore() {
  foreach(var item in groups) if(item.Key) { item.Key.alpha=item.Value.alpha; item.Key.blocksRaycasts=item.Value.blocks; item.Key.interactable=item.Value.interactive; } groups.Clear();
  if(heldInventory && Il2CppMirror.NetworkServer.active && Il2Cpp.ReferenceHub.LocalHub?.inventory?.Pointer==heldInventory!.Pointer && previousSerial!=0) heldInventory.ServerSelectItem(previousSerial);
  heldInventory=null; previousSerial=0;
 }
}
