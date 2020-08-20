using System.Runtime.InteropServices;

namespace AyeBlinkin.Serial
{
    internal class Message 
    {
        internal const byte INTERRUPT = 0xFF;
        internal static Command ExitSerialCom()         => new Command() { Raw = new byte[] { 0xFF, 0xFF, 0xFF }, Type = Type.Exit };
        internal static Command SetPattern(int value)   => new Command() { Raw = new byte[] { 0xFF, 0xFF, (byte)(value & 0xFF) }, Type = Type.Pattern };
        internal static Command GetPatterns()           => new Command() { Raw = new byte[] { 0xFF, 0xFE, 0x00 }, Type = Type.Init };
        internal static Command SetRed(int value)       => new Command() { Raw = new byte[] { 0xFF, 0x00, (byte)(value & 0xFF) }, Type = Type.Red };
        internal static Command SetGreen(int value)     => new Command() { Raw = new byte[] { 0xFF, 0x01, (byte)(value & 0xFF) }, Type = Type.Green };
        internal static Command SetBlue(int value)      => new Command() { Raw = new byte[] { 0xFF, 0x02, (byte)(value & 0xFF) }, Type = Type.Blue };
        internal static Command SetBright(int value)    => new Command() { Raw = new byte[] { 0xFF, 0x03, (byte)(value & 0xFF) }, Type = Type.Brightness };
        internal static Command Clear()                 => new Command() { Raw = new byte[] { 0xFF, 0x00, 0x00, 0xFF, 0x01, 0x00, 0xFF, 0x02, 0x00 }, Type = Type.Stream };

        [StructLayout(LayoutKind.Explicit)]
        internal struct RGB {
            [FieldOffset(0)]
            public byte R;
            [FieldOffset(1)]
            public byte G;
            [FieldOffset(2)]
            public byte B;
        }

        internal enum Type : byte {
            Brightness,
            Red,
            Green,
            Blue,
            Pattern,
            Stream, 
            Exit, 
            Init
        }

        internal struct Command {
            public Type Type;
            public byte[] Raw;
        }
    } 
}