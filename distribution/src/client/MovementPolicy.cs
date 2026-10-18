namespace SLcraft.Client;

public static class MovementPolicy
{
 public static bool CanApply(double x,double y,double z,int completedScans) => completedScans>0 && double.IsFinite(x) && double.IsFinite(y) && double.IsFinite(z);
}
