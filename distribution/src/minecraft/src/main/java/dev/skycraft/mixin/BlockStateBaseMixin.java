package dev.skycraft.mixin;

import dev.skycraft.world.SkyCollision;
import net.minecraft.core.BlockPos;
import net.minecraft.core.Direction;
import net.minecraft.world.level.BlockGetter;
import net.minecraft.world.level.block.SupportType;
import net.minecraft.world.level.block.state.BlockBehaviour;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

/**
 * Skyrim ground and walls hold things up: torches, lanterns, rails, carpets and the like can be
 * placed on terrain and against walls, and stay there.
 */
@Mixin(BlockBehaviour.BlockStateBase.class)
public abstract class BlockStateBaseMixin {
 // Navigation and MoveControl query a block's shape directly for floor height
 // and jumping. A hook only in BlockCollisions fixes walking physics but leaves
 // those AI queries treating SL steps and furniture as air.
 @Inject(method="getCollisionShape(Lnet/minecraft/world/level/BlockGetter;Lnet/minecraft/core/BlockPos;)Lnet/minecraft/world/phys/shapes/VoxelShape;",at=@At("RETURN"),cancellable=true)
 private void slcraft$directShape(BlockGetter level,BlockPos pos,CallbackInfoReturnable<net.minecraft.world.phys.shapes.VoxelShape> cir) {
  var nativeShape=SkyCollision.shapeAt(pos); var vanilla=cir.getReturnValue();
  if(nativeShape!=null && nativeShape!=vanilla)
   cir.setReturnValue(vanilla.isEmpty()?nativeShape:net.minecraft.world.phys.shapes.Shapes.or(vanilla,nativeShape));
 }
 @Inject(method="getCollisionShape(Lnet/minecraft/world/level/BlockGetter;Lnet/minecraft/core/BlockPos;Lnet/minecraft/world/phys/shapes/CollisionContext;)Lnet/minecraft/world/phys/shapes/VoxelShape;",at=@At("RETURN"),cancellable=true)
 private void slcraft$contextShape(BlockGetter level,BlockPos pos,net.minecraft.world.phys.shapes.CollisionContext context,CallbackInfoReturnable<net.minecraft.world.phys.shapes.VoxelShape> cir) {
  slcraft$directShape(level,pos,cir);
 }
	@Inject(
		method = "isFaceSturdy(Lnet/minecraft/world/level/BlockGetter;Lnet/minecraft/core/BlockPos;Lnet/minecraft/core/Direction;Lnet/minecraft/world/level/block/SupportType;)Z",
		at = @At("HEAD"),
		cancellable = true
	)
	private void skycraft$skyrimIsSturdy(BlockGetter level, BlockPos pos, Direction direction, SupportType type, CallbackInfoReturnable<Boolean> cir) {
		if (!((BlockBehaviour.BlockStateBase) (Object) this).isAir()) {
			return;
		}
		boolean sturdy = direction == Direction.UP ? SkyCollision.supportsFromBelow(pos.above()) : SkyCollision.solidFraction(pos) >= 0.4F;
		if (sturdy) {
			cir.setReturnValue(true);
		}
	}
}
