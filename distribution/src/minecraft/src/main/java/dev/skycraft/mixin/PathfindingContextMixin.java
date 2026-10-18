package dev.skycraft.mixin;

import dev.skycraft.world.SkyPathing;
import net.minecraft.core.BlockPos;
import net.minecraft.world.level.pathfinder.PathfindingContext;
import net.minecraft.world.level.pathfinder.PathType;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

@Mixin(PathfindingContext.class)
public abstract class PathfindingContextMixin {
 // Apply after the vanilla cache: collision streaming can change without a
 // Minecraft block update invalidating that cache.
 @Inject(method="getPathTypeFromState",at=@At("RETURN"),cancellable=true)
 private void slcraft$geometry(int x,int y,int z,CallbackInfoReturnable<PathType> cir) {
  cir.setReturnValue(SkyPathing.classify(new BlockPos(x,y,z),cir.getReturnValue()));
 }
}
