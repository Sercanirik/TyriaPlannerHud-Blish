using System;
using System.Runtime.InteropServices;
namespace TyriaPlanner.Hud.Ui
{
    public static class WindowsInput
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint   dwFlags;
            public uint   time;
            public IntPtr dwExtraInfo;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int    dx;
            public int    dy;
            public uint   mouseData;
            public uint   dwFlags;
            public uint   time;
            public IntPtr dwExtraInfo;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint   uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }
        [StructLayout(LayoutKind.Explicit)]
        private struct INPUT_UNION
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint        type;
            public INPUT_UNION u;
        }
        private const uint INPUT_KEYBOARD     = 1;
        private const uint KEYEVENTF_KEYUP    = 0x0002;
        private const uint KEYEVENTF_SCANCODE = 0x0008;
        private const ushort VK_RETURN  = 0x0D;
        private const ushort VK_CONTROL = 0x11;
        private const ushort VK_TAB     = 0x09;
        private const ushort VK_V       = 0x56;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
        private static void Stroke(ushort vk)
        {
            var inputs = new[]
            {
                Key(vk, false),
                Key(vk, true),
            };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
        public static void Enter()  => Stroke(VK_RETURN);
        public static void Tab()    => Stroke(VK_TAB);
        public static void CtrlV()
        {
            var inputs = new[]
            {
                Key(VK_CONTROL, false),
                Key(VK_V,       false),
                Key(VK_V,       true),
                Key(VK_CONTROL, true),
            };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
        private static INPUT Key(ushort vk, bool up)
        {
            return new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new INPUT_UNION
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = vk,
                        dwFlags = up ? KEYEVENTF_KEYUP : 0,
                    },
                },
            };
        }
    }
}
