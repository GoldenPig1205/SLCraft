namespace SLcraft.Client;

// Native menus suspend presentation/input without disconnecting the Minecraft world.
public sealed class LinkSession
{
 public bool Connected { get; private set; }
 public bool NativeMenuOpen { get; private set; }
 public bool ControlsMinecraft => Connected && !NativeMenuOpen;
 public void Begin() { Connected=true; NativeMenuOpen=false; }
 public void ObserveNativeMenu(bool visible) => NativeMenuOpen=Connected && visible;
 public void End() { Connected=false; NativeMenuOpen=false; }
}
