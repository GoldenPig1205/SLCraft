package dev.skycraft.mixin;

import dev.skycraft.world.SkyCollision;
import net.minecraft.core.BlockPos;
import net.minecraft.world.entity.ai.navigation.GroundPathNavigation;
import net.minecraft.world.level.chunk.LevelChunk;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

@Mixin(GroundPathNavigation.class)
public abstract class GroundPathNavigationMixin {
 @Inject(method="findSurfacePosition",at=@At("HEAD"),cancellable=true)
 private void slcraft$surface(LevelChunk chunk,BlockPos target,int accuracy,CallbackInfoReturnable<BlockPos> cir) {
  if(!SkyCollision.active()) return;
  // Vanilla scans to the dimension bottom because the facility isn't a block.
  for(int dy:new int[]{0,-1,1,-2,2,-3,-4,-5,-6,-7,-8}) {
   var candidate=target.offset(0,dy,0);
   if(dev.skycraft.world.SkyPathing.supported(candidate)) { cir.setReturnValue(candidate); return; }
  }
 }
}
