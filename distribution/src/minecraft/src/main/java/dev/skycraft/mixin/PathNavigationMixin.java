package dev.skycraft.mixin;

import dev.skycraft.world.SkyPathing;
import net.minecraft.core.BlockPos;
import net.minecraft.world.entity.ai.navigation.PathNavigation;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

@Mixin(PathNavigation.class)
public abstract class PathNavigationMixin {
 @Inject(method="getGroundY",at=@At("RETURN"),cancellable=true)
 private void slcraft$floor(net.minecraft.world.phys.Vec3 target,CallbackInfoReturnable<Double> cir) {
  // Vanilla skips its floor calculation when the supporting block is AIR.
  cir.setReturnValue(SkyPathing.floor(BlockPos.containing(target),cir.getReturnValue()));
 }
 @Inject(method="isStableDestination",at=@At("HEAD"),cancellable=true)
 private void slcraft$support(BlockPos pos,CallbackInfoReturnable<Boolean> cir) {
  if(SkyPathing.supported(pos)) cir.setReturnValue(true);
 }
}
