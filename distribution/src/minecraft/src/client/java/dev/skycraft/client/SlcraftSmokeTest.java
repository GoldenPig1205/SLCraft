package dev.skycraft.client;

import dev.skycraft.world.SkyClip;
import net.minecraft.client.Minecraft;
import net.minecraft.world.InteractionHand;
import net.minecraft.world.item.BlockItem;
import net.minecraft.world.phys.BlockHitResult;
import net.minecraft.world.phys.HitResult;
import net.minecraft.core.BlockPos;
import net.minecraft.core.Direction;
import java.nio.file.Files;
import java.nio.file.Path;

/** Explicit, local development smoke check; inactive in normal launches. */
final class SlcraftSmokeTest {
 private static int ticks,stage,selected;
 private static BlockPos target;
 private static net.minecraft.world.level.block.state.BlockState before;
 private static net.minecraft.world.item.ItemStack saved;
 private static net.minecraft.world.level.block.Block expected;
 private static net.minecraft.world.phys.Vec3 start;
 private static boolean noGravity;
 static void tick(Minecraft mc) {
  String dir=System.getenv("SLCRAFT_SMOKE_TEST");
  if(dir==null || stage==5 || mc.player==null || mc.level==null || mc.getSingleplayerServer()==null || !SkyClient.sky().inGame() || SkyClient.sky().menuOpen()) return;
  if(++ticks<100) return;
  try {
   var player=mc.player; var server=mc.getSingleplayerServer();
   if(stage==0) {
    var from=player.getEyePosition(); var look=player.getLookAngle();
    var to=player.position().add(new net.minecraft.world.phys.Vec3(look.x,0,look.z).normalize().scale(4)).add(0,-1.5,0);
    var miss=BlockHitResult.miss(to,Direction.UP,BlockPos.containing(to));
    var hit=SkyClip.refine(from,to,miss,SkyClip.Use.PICK);
    if(hit.getType()!=HitResult.Type.BLOCK) { report(dir,"FAIL: facility floor ray missed"); stage=5; return; }
    selected=player.getInventory().getSelectedSlot();
    int slot=-1;
    for(int i=0;i<9;i++) if(player.getInventory().getItem(i).getItem() instanceof BlockItem) { slot=i; break; }
    if(slot<0) { report(dir,"FAIL: no block in hotbar for placement check"); stage=5; return; }
    player.getInventory().setSelectedSlot(slot);
    saved=player.getMainHandItem().copy(); expected=((BlockItem)saved.getItem()).getBlock();
    target=hit.getBlockPos(); before=mc.level.getBlockState(target);
    if(!before.isAir()) { report(dir,"FAIL: target already occupied"); stage=5; return; }
    var result=mc.gameMode.useItemOn(player,InteractionHand.MAIN_HAND,hit);
    report(dir,"USE: target="+target+" face="+hit.getDirection()+" result="+result);
    stage=1; ticks=0;
   } else if(stage==1 && ticks>=100) {
    var actual=server.overworld().getBlockState(target);
    report(dir,(actual.is(expected)?"PASS":"FAIL")+": server block after normal useItemOn = "+actual);
    var uuid=player.getUUID(); var restore=saved.copy(); var restorePos=target; var restoreState=before;
    server.execute(()->{ var sp=server.getPlayerList().getPlayer(uuid); if(sp!=null) sp.setItemInHand(InteractionHand.MAIN_HAND,restore); server.overworld().setBlockAndUpdate(restorePos,restoreState); });
    player.getInventory().setSelectedSlot(selected);
    stage=2; ticks=0;
   } else if(stage==2) {
    start=player.position(); noGravity=player.isNoGravity(); player.setNoGravity(true);
    var uuid=player.getUUID(); var destination=start.add(512,-64,512);
    server.execute(()->{ var sp=server.getPlayerList().getPlayer(uuid); if(sp!=null) { sp.setNoGravity(true); sp.teleportTo(destination.x,destination.y,destination.z); } });
    stage=3; ticks=0;
   } else if(stage==3) {
    var p=player.position(); var host=SkyClient.sky();
    boolean followed=Math.abs(host.x-p.x)<1.5 && Math.abs(host.y-p.y)<1.5 && Math.abs(host.z-p.z)<1.5;
    report(dir,(followed && p.distanceTo(start)>400?"PASS":"FAIL")+": outside facility host follows empty space; mc="+p+"; host="+host.x+","+host.y+","+host.z);
    var uuid=player.getUUID(); var original=start;
    server.execute(()->{ var sp=server.getPlayerList().getPlayer(uuid); if(sp!=null) { sp.teleportTo(original.x,original.y,original.z); sp.setNoGravity(noGravity); } });
    player.setNoGravity(noGravity); stage=4; ticks=0;
   } else if(stage==4) {
    var p=player.position(); var host=SkyClient.sky();
    report(dir,(Math.abs(host.x-p.x)<1.5 && Math.abs(host.y-p.y)<1.5 && Math.abs(host.z-p.z)<1.5?"PASS":"FAIL")+": returned from empty space without reconnecting");
    stage=5;
   }
  } catch(Exception e) { report(dir,"FAIL: "+e); stage=5; }
 }
 private static void report(String dir,String line) {
  dev.skycraft.SkyCraft.LOG.info("SLcraft smoke {}",line);
  try { Files.createDirectories(Path.of(dir)); Files.writeString(Path.of(dir,"placement-smoke.txt"),line+"\n",java.nio.file.StandardOpenOption.CREATE,java.nio.file.StandardOpenOption.APPEND); } catch(Exception ignored) {}
 }
}
