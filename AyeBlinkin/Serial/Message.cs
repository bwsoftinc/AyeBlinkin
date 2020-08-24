namespace AyeBlinkin.Serial
{
    internal class Message 
    {
        internal const byte INTERRUPT = 0xFF;
        internal static Command ExitSerialCom()         => new Command() { Raw = new byte[] { 0xFF, 0xFF, 0xFF }, Type = Type.Exit };
        internal static Command SetPattern(int value)   => new Command() { Raw = new byte[] { 0xFF, 0xFF, (byte)(value & 0xFF) }, Type = Type.Pattern };
        internal static Command StreamStart()           => new Command() { Raw = new byte[] { 0xFF, 0xFF, 0xF0 }, Type = Type.StreamStart };
        internal static Command StreamEnd()             => new Command() { Raw = new byte[] { 0xFF, 0xFF, 0xFF }, Type = Type.StreamEnd };
        internal static Command GetPatterns()           => new Command() { Raw = new byte[] { 0xFF, 0xFE, 0x00 }, Type = Type.Init };
        internal static Command SetRed(int value)       => new Command() { Raw = new byte[] { 0xFF, 0x00, (byte)(value & 0xFF) }, Type = Type.Red };
        internal static Command SetGreen(int value)     => new Command() { Raw = new byte[] { 0xFF, 0x01, (byte)(value & 0xFF) }, Type = Type.Green };
        internal static Command SetBlue(int value)      => new Command() { Raw = new byte[] { 0xFF, 0x02, (byte)(value & 0xFF) }, Type = Type.Blue };
        internal static Command SetBright(int value)    => new Command() { Raw = new byte[] { 0xFF, 0x03, (byte)(value & 0xFF) }, Type = Type.Brightness };
        internal static Command Clear()                 => new Command() { Raw = new byte[] { 0xFF, 0x00, 0x00, 0xFF, 0x01, 0x00, 0xFF, 0x02, 0x00 }, Type = Type.Stream };

        internal enum Type : byte {
            Brightness,
            Red,
            Green,
            Blue,
            Pattern,
            Stream, 
            StreamStart,
            StreamEnd,
            Exit, 
            Init
        }

        internal struct Command {
            public Type Type;
            public byte[] Raw;
        }
    } 
}