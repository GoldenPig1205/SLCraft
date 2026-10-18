// Some SCP:SL-generated interop references expose the native NullableAttribute without CLR
// constructors. Keep the compiler's metadata attributes in this managed assembly instead.
namespace System.Runtime.CompilerServices;
[AttributeUsage(AttributeTargets.All, AllowMultiple=false)]
internal sealed class NullableAttribute : Attribute
{
 public NullableAttribute(byte value) { }
 public NullableAttribute(byte[] value) { }
}
[AttributeUsage(AttributeTargets.All, AllowMultiple=false)]
internal sealed class NullableContextAttribute : Attribute
{
 public NullableContextAttribute(byte value) { }
}
