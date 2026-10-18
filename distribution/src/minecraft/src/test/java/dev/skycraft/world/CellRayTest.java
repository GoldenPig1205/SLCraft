package dev.skycraft.world;
import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;

class CellRayTest {
 private static void hit(int expected,Integer actual) { assertNotNull(actual); assertEquals(expected,actual.intValue()); }
 @Test void wallInNegativeCoordinates() { assertEquals("wall",CellRay.first(-.5,1.5,.5,-5,1.5,.5,(x,y,z)->x==-3?"wall":null)); }
 @Test void downwardFloor() { hit(100,CellRay.first(.5,102,.5,.5,98,.5,(x,y,z)->y==100?y:null)); }
 @Test void nearestSurfaceWins() { hit(2,CellRay.first(.5,.5,.5,8,.5,.5,(x,y,z)->x==2||x==5?x:null)); }
 @Test void doesNotHitBeyondReach() { assertNull(CellRay.first(.5,.5,.5,2.9,.5,.5,(x,y,z)->x==3?x:null)); }
 @Test void negativeIntegerBoundary() { hit(-1,CellRay.first(0,.5,.5,-2,.5,.5,(x,y,z)->x==-1?x:null)); }
 @Test void zeroLengthIsFinite() { assertNull(CellRay.first(0,0,0,0,0,0,(x,y,z)->null)); }
 @Test void diagonalDoesNotStall() { hit(3,CellRay.first(.5,.5,.5,4,4,4,(x,y,z)->x==3&&y==3&&z==3?x:null)); }
}
