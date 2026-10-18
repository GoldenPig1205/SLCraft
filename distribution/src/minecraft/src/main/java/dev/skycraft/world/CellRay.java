package dev.skycraft.world;

/** Finite-segment grid traversal, shared by the SL voxel surface pick. */
public final class CellRay {
 private CellRay() {}
 public interface Visitor<T> { T visit(int x,int y,int z); }
 public static <T> T first(double fx,double fy,double fz,double tx,double ty,double tz,Visitor<T> visitor) {
  int x=(int)Math.floor(fx),y=(int)Math.floor(fy),z=(int)Math.floor(fz);
  double dx=tx-fx,dy=ty-fy,dz=tz-fz;
  int sx=Double.compare(dx,0),sy=Double.compare(dy,0),sz=Double.compare(dz,0);
  double ax=dx==0?Double.POSITIVE_INFINITY:Math.abs(1/dx),ay=dy==0?Double.POSITIVE_INFINITY:Math.abs(1/dy),az=dz==0?Double.POSITIVE_INFINITY:Math.abs(1/dz);
  double nx=dx==0?Double.POSITIVE_INFINITY:(dx>0?x+1-fx:fx-x)*ax;
  double ny=dy==0?Double.POSITIVE_INFINITY:(dy>0?y+1-fy:fy-y)*ay;
  double nz=dz==0?Double.POSITIVE_INFINITY:(dz>0?z+1-fz:fz-z)*az;
  for(int i=0;i<4096;i++) {
   T hit=visitor.visit(x,y,z); if(hit!=null) return hit;
   double next=Math.min(nx,Math.min(ny,nz));
   if(!Double.isFinite(next)||next>1) return null;
   // Advance all tied axes so an exact grid corner never stalls the traversal.
   if(nx<=next+1e-12) { x+=sx; nx+=ax; }
   if(ny<=next+1e-12) { y+=sy; ny+=ay; }
   if(nz<=next+1e-12) { z+=sz; nz+=az; }
  }
  return null;
 }
}
