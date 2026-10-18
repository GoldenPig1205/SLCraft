package dev.skycraft.client;

import net.minecraft.client.Minecraft;
import net.minecraft.core.BlockPos;
import net.minecraft.world.level.block.Blocks;

/** Opt-in fixture for the isolated development world; never runs in a normal launch. */
final class SlcraftVisualTest {
 private static int ticks;
 private static boolean placed;
 static void tick(Minecraft minecraft) {
  if (System.getenv("SLCRAFT_CLIENT_TEST") == null || placed || !SkyClient.linked()
      || minecraft.player == null || minecraft.getSingleplayerServer() == null || !SkyClient.sky().inGame()) return;
  if (++ticks < 60) return;
  placed = true;
  var feet = minecraft.player.position();
  var target = feet.add(minecraft.player.getLookAngle().scale(3));
  var pos = new BlockPos((int)Math.floor(target.x), (int)Math.floor(feet.y), (int)Math.floor(target.z));
  var server = minecraft.getSingleplayerServer();
  server.execute(() -> {
   server.overworld().setBlockAndUpdate(pos, Blocks.CRAFTING_TABLE.defaultBlockState());
   server.overworld().setBlockAndUpdate(pos.above(), Blocks.TORCH.defaultBlockState());
   dev.skycraft.SkyCraft.LOG.info("SLcraft visual fixture placed original crafting table and torch at {}", pos);
  });
 }
}
