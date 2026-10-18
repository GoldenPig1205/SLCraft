package dev.skycraft.mixin;

import dev.skycraft.world.SkyPathing;
import net.minecraft.core.BlockPos;
import net.minecraft.world.level.BlockGetter;
import net.minecraft.world.level.pathfinder.WalkNodeEvaluator;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

@Mixin(WalkNodeEvaluator.class)
public abstract class WalkNodeEvaluatorMixin {
 @Inject(method="getFloorLevel(Lnet/minecraft/world/level/BlockGetter;Lnet/minecraft/core/BlockPos;)D",at=@At("RETURN"),cancellable=true)
 private static void slcraft$floor(BlockGetter level,BlockPos pos,CallbackInfoReturnable<Double> cir) {
  cir.setReturnValue(SkyPathing.floor(pos,cir.getReturnValue()));
 }
}
