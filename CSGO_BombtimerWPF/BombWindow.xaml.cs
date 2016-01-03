using System;
using System.Text;
using System.Windows;
using CSGSI;
using CSGSI.Nodes;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Windows.Interop;

namespace CSGO_BombtimerWPF
{
    /// <summary>
    /// Interaction logic for BombWindow.xaml
    /// </summary>
    public partial class BombWindow : Window
    {
        public int bombTimer = 0;
        public int BOMB_SECS = 40;
        public DispatcherTimer t = new DispatcherTimer();
        public int timestamp = 0;
        public int calculateSeconds()

        {

            TimeSpan cac = DateTime.UtcNow - new DateTime(1970, 1, 1);
            int secondsSinceEpoch = (int)cac.TotalSeconds;

            return secondsSinceEpoch;

        }

        public BombWindow()
        {
            t.Tick += new EventHandler(OnTimedEvent);
            t.Interval = new TimeSpan(0, 0, 0, 0, 1);
            Loaded += BombWindow_Load;
            InitializeComponent();
        }

        private void BombWindow_Load(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => this.Visibility = Visibility.Hidden));

            GameStateListener gsl = new GameStateListener(3000);
            gsl.NewGameState += new NewGameStateHandler(OnNewGameState);

            //start listening on http://localhost:3000/
            if (gsl.Start())
            {
               // clocklabel.Content = "Listening..";
            }
            else
            {
                //Console.WriteLine("Error starting GSIListener.");
               // clocklabel.Content = "Error";
            }

            WindowInteropHelper wndHelper = new WindowInteropHelper(this);

            int exStyle = (int)GetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE);

            exStyle |= (int)ExtendedWindowStyles.WS_EX_TOOLWINDOW;
            SetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);
        }
        static bool IsPlanted = false;
        private void OnNewGameState(GameState gs)
        {
            if (!IsPlanted &&
               gs.Round.Phase == RoundPhase.Live &&
               gs.Round.Bomb == BombState.Planted &&
               gs.Previously.Round.Bomb == BombState.Undefined)
            {
                timestamp = Int32.Parse(gs.Provider.TimeStamp);
                t.Start();
                IsPlanted = true;
            }
            else if (IsPlanted && (gs.Round.Phase == RoundPhase.Over || gs.Round.Phase == RoundPhase.FreezeTime))
            {
                bombTimer = 0;
                t.Stop();
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => this.Visibility = Visibility.Hidden));
                IsPlanted = false;
            }

        }

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);

        public struct Rect
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }

        private void OnTimedEvent(Object source, EventArgs e)
        {
            bombTimer = (timestamp + BOMB_SECS) - calculateSeconds();
            if (bombTimer >= 0)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => clocklabel.Content = bombTimer.ToString()));
                if(bombTimer < 10)
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => clocklabel.Margin = new Thickness(17,7,0,0)));
                }
                else if(clocklabel.Margin == new Thickness(17,7,0,0))
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => clocklabel.Margin = new Thickness(9, 7, 0, 0)));
                }
            }
            if (GetActiveWindowTitle() == "Counter-Strike: Global Offensive")
            {
                this.Visibility = Visibility.Visible;
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => this.Visibility = Visibility.Visible));
                const int nChars = 256;
                StringBuilder Buff = new StringBuilder(nChars);
                IntPtr handle = GetForegroundWindow();
                Rect CSGORect = new Rect();
                GetWindowRect(handle, ref CSGORect);

                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => this.Left = CSGORect.Right - (this.Width + 50)));
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => this.Top = CSGORect.Top + 150 + this.Height));
            }
            else if(this.Visibility == Visibility.Visible)
            {
               Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => this.Visibility = Visibility.Hidden));
            }
        }

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        private string GetActiveWindowTitle()
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            return null;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Environment.Exit(1);
        }
        #region Window styles
        [Flags]
        public enum ExtendedWindowStyles
        {
            // ...
            WS_EX_TOOLWINDOW = 0x00000080,
            // ...
        }

        public enum GetWindowLongFields
        {
            // ...
            GWL_EXSTYLE = (-20),
            // ...
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        public static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            int error = 0;
            IntPtr result = IntPtr.Zero;
            // Win32 SetWindowLong doesn't clear error on success
            SetLastError(0);

            if (IntPtr.Size == 4)
            {
                // use SetWindowLong
                Int32 tempResult = IntSetWindowLong(hWnd, nIndex, IntPtrToInt32(dwNewLong));
                error = Marshal.GetLastWin32Error();
                result = new IntPtr(tempResult);
            }
            else
            {
                // use SetWindowLongPtr
                result = IntSetWindowLongPtr(hWnd, nIndex, dwNewLong);
                error = Marshal.GetLastWin32Error();
            }

            if ((result == IntPtr.Zero) && (error != 0))
            {
                throw new System.ComponentModel.Win32Exception(error);
            }

            return result;
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr IntSetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern Int32 IntSetWindowLong(IntPtr hWnd, int nIndex, Int32 dwNewLong);

        private static int IntPtrToInt32(IntPtr intPtr)
        {
            return unchecked((int)intPtr.ToInt64());
        }

        [DllImport("kernel32.dll", EntryPoint = "SetLastError")]
        public static extern void SetLastError(int dwErrorCode);
        #endregion
    }
}
