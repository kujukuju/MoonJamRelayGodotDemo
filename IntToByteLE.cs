using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Explicit)]
public struct IntToByteLE {
    [FieldOffset(0)]
    public int Value;

    [FieldOffset(0)]
    public byte B0;
    [FieldOffset(1)]
    public byte B1;
    [FieldOffset(2)]
    public byte B2;
    [FieldOffset(3)]
    public byte B3;
}
