using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;


namespace homeKeyPress
{
    class keyMonitorClass : IDisposable
    {
        internal bool homeEndDetection;
        private IntPtr _windowsHookHandle;
        private IntPtr _user32LibraryHandle;
        private HookProc _hookProc;
        private DateTime lastCtrlPressed;
        private int keyPressTime = 5;
        private int keyPressCnt = 0;
        private bool ctrlReleaseMonitor = true;

        //
        public const int WH_KEYBOARD_LL = 13;
        public enum KeyboardState
        {
            KeyDown = 0x0100,
            KeyUp = 0x0101,
            SysKeyDown = 0x0104,
            SysKeyUp = 0x0105
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct LowLevelKeyboardInputEvent
        {
            /// <summary>
            /// A virtual-key code. The code must be a value in the range 1 to 254.
            /// </summary>
            public int VirtualCode;

            // EDT: added a conversion from VirtualCode to Keys.
            /// <summary>
            /// The VirtualCode converted to typeof(Keys) for higher usability.
            /// </summary>
            public Keys Key { get { return (Keys)VirtualCode; } }

            /// <summary>
            /// A hardware scan code for the key. 
            /// </summary>
            public int HardwareScanCode;

            /// <summary>
            /// The extended-key flag, event-injected Flags, context code, and transition-state flag. This member is specified as follows. An application can use the following values to test the keystroke Flags. Testing LLKHF_INJECTED (bit 4) will tell you whether the event was injected. If it was, then testing LLKHF_LOWER_IL_INJECTED (bit 1) will tell you whether or not the event was injected from a process running at lower integrity level.
            /// </summary>
            public int Flags;

            /// <summary>
            /// The time stamp stamp for this message, equivalent to what GetMessageTime would return for this message.
            /// </summary>
            public int TimeStamp;

            /// <summary>
            /// Additional information associated with the message. 
            /// </summary>
            public IntPtr AdditionalInformation;
        }








        /// <summary>
        /// 
        /// </summary>
        /// <param name="lpFileName"></param>
        /// <returns></returns>
        //
        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("USER32", SetLastError = true)]
        static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, int dwThreadId);

        [DllImport("USER32", SetLastError = true)]
        public static extern bool UnhookWindowsHookEx(IntPtr hHook);

        [DllImport("USER32", SetLastError = true)]
        static extern IntPtr CallNextHookEx(IntPtr hHook, int code, IntPtr wParam, IntPtr lParam);

        delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);


        /// <summary>
        /// Constructor for main object
        /// </summary>
        public keyMonitorClass()
        {
            homeEndDetection = false;
            //
            _windowsHookHandle = IntPtr.Zero;
            _user32LibraryHandle = IntPtr.Zero;
            _hookProc = LowLevelKeyboardProc; 

            _user32LibraryHandle = LoadLibrary("User32");
            if (_user32LibraryHandle == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new Win32Exception(errorCode, $"Failed to load library 'User32.dll'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
            }



            _windowsHookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc, _user32LibraryHandle, 0);
            if (_windowsHookHandle == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new Win32Exception(errorCode, $"Failed to adjust keyboard hooks for '{Process.GetCurrentProcess().ProcessName}'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
            }
        }

        ~keyMonitorClass ()
        {
            Dispose(false);
        }

        /// <summary>
        /// Clean up memory
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Turn on Home / End detection
        /// </summary>
        /// <param name="state"></param>
        internal void setHomeEndDetection(bool state)
        {
            homeEndDetection = state;
        }

        /// <summary>
        /// Unload DLL
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // because we can unhook only in the same thread, not in garbage collector thread
                if (_windowsHookHandle != IntPtr.Zero)
                {
                    if (!UnhookWindowsHookEx(_windowsHookHandle))
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        throw new Win32Exception(errorCode, $"Failed to remove keyboard hooks for '{Process.GetCurrentProcess().ProcessName}'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
                    }
                    _windowsHookHandle = IntPtr.Zero;

                    // ReSharper disable once DelegateSubtraction
                    _hookProc -= LowLevelKeyboardProc;
                }
            }

            if (_user32LibraryHandle != IntPtr.Zero)
            {
                if (!FreeLibrary(_user32LibraryHandle)) // reduces reference to library by 1.
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new Win32Exception(errorCode, $"Failed to unload library 'User32.dll'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
                }
                _user32LibraryHandle = IntPtr.Zero;
            }
        }

        public bool ctrlDetectoinActive()
        {
            if (DateTime.Now.Subtract(lastCtrlPressed).Seconds < keyPressTime) return true;
            return false;

        }


        /// <summary>
        /// Main key detect procedure with logic
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        public IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            var wparamTyped = wParam.ToInt32();
            var stopProcessing = false;
            if (Enum.IsDefined(typeof(KeyboardState), wparamTyped))
            {
                object o = Marshal.PtrToStructure(lParam, typeof(LowLevelKeyboardInputEvent));
                LowLevelKeyboardInputEvent p = (LowLevelKeyboardInputEvent)o;

                var key = (Keys)p.VirtualCode;

                if (homeEndDetection)
                {
                    if (DateTime.Now.Subtract(lastCtrlPressed).Seconds > keyPressTime)
                    {
                        if (Keyboard.IsKeyUp(Key.LeftCtrl))
                        {
                            ctrlReleaseMonitor = true;
                        }
                    }


                    //detekcja wcisniecia przycisku CTRL
                    if (key == Keys.LControlKey)
                    {
                        if ((DateTime.Now.Subtract(lastCtrlPressed).Seconds > keyPressTime) && (ctrlReleaseMonitor))
                        {
                            lastCtrlPressed = DateTime.Now;
                            keyPressCnt = 0;
                        }
                    }
                    //detekcja wcisniecia dowolnego innego klawisza w obserwowanym czasie
                    if (DateTime.Now.Subtract(lastCtrlPressed).Seconds < keyPressTime)
                    {
                        if (key != Keys.LControlKey)
                        {
                            keyPressCnt++;//licznik wcisniecia dowolnych klawiszy

                        }
                        if (keyPressCnt>1)
                        {
                            lastCtrlPressed = DateTime.MinValue;//wymuszone zatrzymanie obserwowania
                            ctrlReleaseMonitor = false;//ctrl+strzalka powoduje wyslanie pustego ctrl, nie wiem czemu, trzeba monitorowac puszczenie ctrl
                        }
                    }


                    //logika detekcji wcisniecia strzalki po ctrl
                    if (Keyboard.IsKeyUp(Key.LeftCtrl))
                    {
                        if ((DateTime.Now.Subtract(lastCtrlPressed).Seconds < keyPressTime)&&(keyPressCnt==1))
                        {

                            switch (key)
                            {
                                case Keys.Left:
                                    lastCtrlPressed = DateTime.MinValue;
                                    //send HOME
                                    sendHomeKey();
                                    stopProcessing = true;

                                    break;
                                case Keys.Right:
                                    lastCtrlPressed = DateTime.MinValue;
                                    //send HOME
                                    sendEndKey();
                                    stopProcessing = true;

                                    break;
                                default:
                                    lastCtrlPressed = DateTime.MinValue;
                                    break;


                            }
                        }

                    }


                }

            }


            if (stopProcessing) return (IntPtr)0x0001;
            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        /// <summary>
        /// Send HOME key
        /// </summary>
        private void sendHomeKey()
        {
            SendKeys.Send("{HOME}");
        }

        /// <summary>
        /// Send END key
        /// </summary>
        private void sendEndKey()
        {
            SendKeys.Send("{END}");
        }


    }
}
