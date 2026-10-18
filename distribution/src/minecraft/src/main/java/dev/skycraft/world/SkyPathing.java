package dev.skycraft.world;

import net.minecraft.core.BlockPos;
import net.minecraft.core.Direction;
import net.minecraft.world.level.pathfinder.PathType;

/** The same streamed geometry must inform mob navigation as well as physics. */
public final class SkyPathing {
 private SkyPathing() {}
 public static PathType classify(BlockPos pos, PathType vanilla) {
  if(vanilla!=PathType.OPEN) return vanilla; // retain actual blocks, fluids and hazards
  var shape=SkyCollision.shapeAt(pos);
  if(shape==null || shape.isEmpty()) return vanilla;
  // Quarter-block floor skins occupy the mob's feet cell, like a bottom slab.
  return shape.max(Direction.Axis.Y)<=0.5 ? PathType.WALKABLE : PathType.BLOCKED;
 }
 public static double floor(BlockPos pos,double vanilla) {
  var below=SkyCollision.shapeAt(pos.below());
  if(below!=null && !below.isEmpty()) vanilla=Math.max(vanilla,pos.getY()-1+below.max(Direction.Axis.Y));
  var here=SkyCollision.shapeAt(pos);
  if(here!=null && !here.isEmpty() && here.max(Direction.Axis.Y)<=0.5)
   vanilla=Math.max(vanilla,pos.getY()+here.max(Direction.Axis.Y));
  return vanilla;
 }
 public static boolean supported(BlockPos pos) {
  return SkyCollision.supportsFromBelow(pos) && classify(pos,PathType.OPEN)!=PathType.BLOCKED;
 }
}
