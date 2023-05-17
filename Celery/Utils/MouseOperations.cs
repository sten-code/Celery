using Celery.CeleryAPI;
using System;
using System.Runtime.InteropServices;

namespace Celery.Utils
{
    public class MouseOperations
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(uint hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetForegroundWindow();

        [Flags]
        public enum MouseEventFlags
        {
            LeftDown = 0x00000002,
            LeftUp = 0x00000004,
            MiddleDown = 0x00000020,
            MiddleUp = 0x00000040,
            Move = 0x00000001,
            Absolute = 0x00008000,
            RightDown = 0x00000008,
            RightUp = 0x00000010
        }

        [DllImport("user32.dll", EntryPoint = "SetCursorPos")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out MousePoint lpMousePoint);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);
        //Mouse actions
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(uint hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }

        public static void SetCursorPosition(int x, int y)
        {
            SetCursorPos(x, y);
        }

        public static void SetCursorPosition(MousePoint point)
        {
            SetCursorPos(point.X, point.Y);
        }

        public static MousePoint GetCursorPosition()
        {
            var gotPoint = GetCursorPos(out MousePoint currentMousePoint);
            if (!gotPoint) { currentMousePoint = new MousePoint(0, 0); }
            return currentMousePoint;
        }

        public static void MouseEvent(MouseEventFlags value)
        {
            MousePoint position = GetCursorPosition();
            mouse_event((uint)value, (uint)position.X, (uint)position.Y, 0, 0);
        }

        public static void doMouse1Click()
        {
            //Call the imported function with the cursor's current position
            MousePoint position = GetCursorPosition();
            uint X = (uint)position.X;
            uint Y = (uint)position.Y;
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, X, Y, 0, 0);
        }

        public static void doMouse2Click()
        {
            //Call the imported function with the cursor's current position
            MousePoint position = GetCursorPosition();
            uint X = (uint)position.X;
            uint Y = (uint)position.Y;
            if (GetForegroundWindow() == (uint)Imports.FindWindow(null, "Roblox"))
                mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP, X, Y, 0, 0);
        }

        public static void doMouse1Down()
        {
            //Call the imported function with the cursor's current position
            MousePoint position = GetCursorPosition();
            uint X = (uint)position.X;
            uint Y = (uint)position.Y;
            if (GetForegroundWindow() == (uint)Imports.FindWindow(null, "Roblox"))
                mouse_event(MOUSEEVENTF_LEFTDOWN, X, Y, 0, 0);
        }

        public static void doMouse1Up()
        {
            //Call the imported function with the cursor's current position
            MousePoint position = GetCursorPosition();
            uint X = (uint)position.X;
            uint Y = (uint)position.Y;
            if (GetForegroundWindow() == (uint)Imports.FindWindow(null, "Roblox"))
                mouse_event(MOUSEEVENTF_LEFTUP, X, Y, 0, 0);
        }

        public static void doMouse2Down()
        {
            //Call the imported function with the cursor's current position
            MousePoint position = GetCursorPosition();
            uint X = (uint)position.X;
            uint Y = (uint)position.Y;
            if (GetForegroundWindow() == (uint)Imports.FindWindow(null, "Roblox"))
                mouse_event(MOUSEEVENTF_RIGHTDOWN, X, Y, 0, 0);
        }

        public static void doMouse2Up()
        {
            //Call the imported function with the cursor's current position
            MousePoint position = GetCursorPosition();
            uint X = (uint)position.X;
            uint Y = (uint)position.Y;
            if (GetForegroundWindow() == (uint)Imports.FindWindow(null, "Roblox"))
                mouse_event(MOUSEEVENTF_RIGHTUP, X, Y, 0, 0);
        }

        public static void mouseMoveRel(int x, int y)
        {
            GetWindowRect((uint)Imports.FindWindow(null, "Roblox"), out RECT rc);
            if (GetForegroundWindow() == (uint)Imports.FindWindow(null, "Roblox"))
                SetCursorPos(rc.Left + x, rc.Top + y);
        }

        public static void mouseMoveAbs(int x, int y)
        {
            if (GetForegroundWindow() == (uint)Imports.FindWindow(null, "Roblox"))
                SetCursorPos(x, y);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MousePoint
        {
            public int X;
            public int Y;

            public MousePoint(int x, int y)
            {
                X = x;
                Y = y;
            }
        }
    }

}
