package dev.skycraft.client;

import dev.skycraft.world.SkyCollision;
import dev.skycraft.world.SkyPathing;
import net.minecraft.client.Minecraft;
import net.minecraft.core.BlockPos;
import net.minecraft.core.particles.ParticleTypes;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.world.entity.EntitySpawnReason;
import net.minecraft.world.entity.EntityTypes;
import net.minecraft.world.entity.Mob;
import net.minecraft.world.phys.AABB;
import net.minecraft.world.phys.Vec3;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.List;

/** Opt-in navigation/texture fixture. Uses real AI paths on native SL geometry. */
final class SlcraftEntityTest {
 private static int ticks,stage,walkTicks;
 private static Mob walker;
 private static Vec3 start,destination;
 private static BlockPos blast;
 private static int blastTicks,stonesBefore;
 private static final java.util.Map<BlockPos,net.minecraft.world.level.block.state.BlockState> restoreBlocks=new java.util.HashMap<>();
 private static final List<Mob> owned=new ArrayList<>();
 static void register() {
  net.fabricmc.fabric.api.event.lifecycle.v1.ServerLifecycleEvents.SERVER_STOPPING.register(server->{
   for(var entity:owned) entity.discard(); owned.clear();
   for(var entry:restoreBlocks.entrySet()) server.overworld().setBlock(entry.getKey(),entry.getValue(),2|16);
  });
 }
 static void tick(Minecraft mc) {
  var dir=System.getenv("SLCRAFT_ENTITY_TEST"); var server=mc.getSingleplayerServer();
  if(dir==null || server==null || mc.player==null || !SkyClient.sky().inGame() || SkyClient.sky().menuOpen()) return;
  var uuid=mc.player.getUUID();
  server.execute(()->{
   try {
    var player=server.getPlayerList().getPlayer(uuid); if(player==null) return;
    if(++ticks<700) return; // native role-change smoke has finished
    var world=server.overworld();
    if(stage==0) {
     var candidates=new ArrayList<Vec3>(); var origin=player.blockPosition();
     for(int dx=-5;dx<=5;dx++) for(int dz=-5;dz<=5;dz++) for(int dy=-2;dy<=1;dy++) {
      var pos=origin.offset(dx,dy,dz);
      if(!SkyCollision.isKnown(pos.getX(),pos.getY(),pos.getZ()) || !SkyPathing.supported(pos) || !world.getBlockState(pos).isAir()) continue;
      double y=SkyPathing.floor(pos,pos.getY()-1);
      var p=new Vec3(pos.getX()+.5,y+.01,pos.getZ()+.5);
      if(world.noCollision(new AABB(p.x-.45,p.y,p.z-.45,p.x+.45,p.y+1.95,p.z+.45))) candidates.add(p);
     }
     walker=EntityTypes.COW.create(world,EntitySpawnReason.COMMAND);
     if(walker==null || candidates.size()<2) { report(dir,"FAIL: native floor candidates="+candidates.size()); stage=4; return; }
     boolean found=false;
     for(var a:candidates) {
      walker.setPos(a.x,a.y,a.z); walker.setOnGround(true);
      for(var b:candidates) {
       if(a.distanceTo(b)<2 || a.distanceTo(b)>4 || Math.abs(a.y-b.y)>.6) continue;
       boolean clear=true;
       for(int step=1;step<=20;step++) { var p=a.lerp(b,step/20d); if(!world.noCollision(new AABB(p.x-.45,p.y,p.z-.45,p.x+.45,p.y+1.95,p.z+.45))) { clear=false; break; } }
       if(!clear) continue;
       var path=walker.getNavigation().createPath(BlockPos.containing(b),0);
       if(path!=null && path.canReach()) { start=a; destination=b; found=true; break; }
      }
      if(found) break;
     }
     if(!found) { report(dir,"FAIL: native floor path not found; candidates="+candidates.size()); stage=4; return; }
     world.addFreshEntity(walker); owned.add(walker);
     walker.removeAllGoals(goal->true);
     walker.getGoalSelector().addGoal(0,new net.minecraft.world.entity.ai.goal.Goal() {
      { setFlags(java.util.EnumSet.of(net.minecraft.world.entity.ai.goal.Goal.Flag.MOVE)); }
      public boolean canUse() { return walker.position().distanceTo(destination)>.3; }
      public void start() { navigate(); }
      public void tick() { if(walker.getNavigation().isDone()) navigate(); }
     });
     navigate();
     var villager=EntityTypes.VILLAGER.create(world,EntitySpawnReason.COMMAND);
     if(villager!=null) {
      var view=player.position().add(player.getLookAngle().scale(2));
      var p=candidates.stream().min(java.util.Comparator.comparingDouble(v->v.distanceToSqr(view))).orElse(candidates.get(0));
      villager.setPos(p.x,p.y,p.z); world.addFreshEntity(villager); owned.add(villager);
     }
     report(dir,"PATH: start="+start+" destination="+destination+"; nativeShapes="+SkyCollision.blockCount());
     stage=1;
    } else if(stage==1) {
     walkTicks++;
     if(walkTicks%30==0) {
      var path=walker.getNavigation().getPath(); var move=walker.getMoveControl();
      report(dir,"WALK: pos="+walker.position()+" ground="+walker.onGround()+" velocity="+walker.getDeltaMovement()+" speed="+walker.getSpeed()+" wanted="+new Vec3(move.getWantedX(),move.getWantedY(),move.getWantedZ())+" path="+(path==null?"null":path.getNextNodeIndex()+"/"+path.getNodeCount()+" "+(path.isDone()?"done":path.getNextNodePos())));
     }
     if(walkTicks%10==0 && walker.position().distanceTo(destination)>.5) navigate();
     if(walkTicks>=120) {
      double moved=walker.position().distanceTo(start);
      report(dir,(moved>1.5 && walker.position().distanceTo(destination)<.6 && Math.abs(walker.getY()-start.y)<2?"PASS":"FAIL")+": actual mob navigation displacement="+moved+"; position="+walker.position());
      stage=2;
     }
    } else if(stage==2) {
     var center=player.blockPosition().offset(0,0,4);
     for(int dx=-2;dx<=2;dx++) for(int dz=-2;dz<=2;dz++) {
      put(world,center.offset(dx,0,dz),net.minecraft.world.level.block.Blocks.STONE.defaultBlockState());
      put(world,center.offset(dx,1,dz),Math.abs(dx)==2 || Math.abs(dz)==2 ? net.minecraft.world.level.block.Blocks.GLASS.defaultBlockState() : net.minecraft.world.level.block.Blocks.WATER.defaultBlockState());
     }
     blast=player.blockPosition().offset(11,2,0);
     stonesBefore=stones(world,blast);
     put(world,blast,net.minecraft.world.level.block.Blocks.OAK_PLANKS.defaultBlockState());
     var tnt=new net.minecraft.world.entity.item.PrimedTnt(world,blast.getX()+.5,blast.getY()+1,blast.getZ()+.5,null);
     tnt.setNoGravity(true); tnt.setDeltaMovement(Vec3.ZERO); tnt.setFuse(40); world.addFreshEntity(tnt);
     report(dir,"TNT: started real explosion; destruction="+dev.skycraft.world.SkyDig.destruction+"; stoneBefore="+stonesBefore);
     stage=3;
    } else if(stage==3 && ++blastTicks>=100) {
     int after=stones(world,blast);
     report(dir,(after==stonesBefore && world.getBlockState(blast).isAir()?"PASS":"FAIL")+": TNT breaks placed Minecraft planks without generating native terrain; stoneAfter="+after);
     stage=4;
    }
    for(var entity:owned) entity.getLookControl().setLookAt(player,30,30);
    if(ticks%10==0) world.sendParticles(ParticleTypes.FLAME,player.getX(),player.getY()+1,player.getZ()+2,12,.7,.5,.7,.01);
   } catch(Exception e) { report(dir,"FAIL: "+e); stage=4; }
  });
 }
 private static void navigate() {
  // The coordinate overload uses reach accuracy 1: stopping one node short is
  // normal vanilla behavior, not a collision stall. Test the complete path.
  var path=walker.getNavigation().createPath(BlockPos.containing(destination),0);
  if(path!=null) walker.getNavigation().moveTo(path,1);
 }
 private static void put(ServerLevel level,BlockPos pos,net.minecraft.world.level.block.state.BlockState state) {
  restoreBlocks.putIfAbsent(pos.immutable(),level.getBlockState(pos)); level.setBlock(pos,state,2|16);
 }
 private static int stones(ServerLevel level,BlockPos center) {
  int count=0;
  for(var pos:BlockPos.betweenClosed(center.offset(-6,-6,-6),center.offset(6,6,6))) {
   var state=level.getBlockState(pos);
   if(state.equals(dev.skycraft.world.SkyDig.blockFor(level,pos,dev.skycraft.link.Proto.DIG_STONE))) count++;
  }
  return count;
 }
 private static void report(String dir,String line) {
  dev.skycraft.SkyCraft.LOG.info("SLcraft entities {}",line);
  try { Files.createDirectories(Path.of(dir)); Files.writeString(Path.of(dir,"entity-smoke.txt"),line+"\n",java.nio.file.StandardOpenOption.CREATE,java.nio.file.StandardOpenOption.APPEND); } catch(Exception ignored) {}
 }
}
