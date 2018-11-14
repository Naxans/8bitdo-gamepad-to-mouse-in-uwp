using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Gaming.Input;
using Windows.Graphics.Display;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Input.Preview.Injection;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace _8bitdo_gamepad
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        double x = 0;
        double y = 0;
        double Deadzone = 0.1;
        int DesignWidth = 1500;
        int DesignHeight = 1000;
        double ScaleWidth;
        double ScaleHeight;
        double ScreenWidth;
        double ScreenHeight;
        double scaleFactor;
        int DelayReadingGamepad = 50; //milliseconds

        Gamepad controller;
        GamepadReading reading;
        InjectedInputMouseInfo info1;
        InjectedInputMouseInfo down;
        //DispatcherTimer dispatcherTimer;
        //TimeSpan period = TimeSpan.FromMilliseconds(2);

        /// <summary>
        /// Allows a loop to execute asynchronously to the main part of the application.
        /// </summary>
        private Task _run;

        public MainPage()
        {
            //minimsize titlebar and taskbar to a small blue block when hoover top or bottm of the screen
            Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().FullScreenSystemOverlayMode = FullScreenSystemOverlayMode.Minimal;
            //  If you want to start the app in full screen mode just set the following enum:
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;
            //Othewise if you want to switch to full screen at runtime you need to use a different method:
            //   ApplicationView.GetForCurrentView().TryEnterFullScreenMode();

            GetCurrentDisplaySize();
            ScreenWidth = GetCurrentDisplaySize().Width;
            ScreenHeight = GetCurrentDisplaySize().Height;
            //PreviousScreenHeight = GetCurrentDisplaySize().Height; //de hoogte van het startscherm opslaan
            scaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel; //scalefactor nodig  voor schermen met hoge resolutie per inch zaols smartphones (desktop = 1, voor 6 inch smartphone 1920x1080 = 2.5)
            ScaleWidth = (double)ScreenWidth / (DesignWidth * scaleFactor);
            ScaleHeight = (double)ScreenHeight / (DesignHeight * scaleFactor);

            this.InitializeComponent();

            //dispatcherTimer = new DispatcherTimer();
            //dispatcherTimer.Interval = period;
            //dispatcherTimer.Tick += dispatcherTimer_Tick;
            //dispatcherTimer.Start();

            //public static event EventHandler<Gamepad> GamepadAdded
            Gamepad.GamepadAdded += Gamepad_GamepadAdded;
            //public static event EventHandler<Gamepad> GamepadRemoved
            Gamepad.GamepadRemoved += Gamepad_GamepadRemoved;

            //CoreWindow.GetForCurrentThread().KeyDown += mainpage_KeyDown;
            CoreWindow.GetForCurrentThread().KeyUp += mainpage_KeyUp;

                //ElementSoundPlayer.Play(ElementSoundKind.Focus);

            //show the button that has focus
            this.GotFocus += (object sender, RoutedEventArgs e) =>
            {
                FrameworkElement focus = FocusManager.GetFocusedElement() as FrameworkElement;
                Debug.WriteLine("got focus: " + focus.Name + " (" + focus.GetType().ToString() + ")");

            };

        }
        public static Size GetCurrentDisplaySize()
        {
            var displayInformation = DisplayInformation.GetForCurrentView();
            TypeInfo t = typeof(DisplayInformation).GetTypeInfo();
            var props = t.DeclaredProperties.Where(x => x.Name.StartsWith("Screen") && x.Name.EndsWith("InRawPixels")).ToArray();
            var w = props.Where(x => x.Name.Contains("Width")).First().GetValue(displayInformation);
            var h = props.Where(x => x.Name.Contains("Height")).First().GetValue(displayInformation);
            var size = new Size(System.Convert.ToDouble(w), System.Convert.ToDouble(h));
            switch (displayInformation.CurrentOrientation)
            {
                case DisplayOrientations.Landscape:
                case DisplayOrientations.LandscapeFlipped:
                    size = new Size(Math.Max(size.Width, size.Height), Math.Min(size.Width, size.Height));
                    break;
                case DisplayOrientations.Portrait:
                case DisplayOrientations.PortraitFlipped:
                    size = new Size(Math.Min(size.Width, size.Height), Math.Max(size.Width, size.Height));
                    break;
            }
            return size;
        }
        #region EventHandlers

        private async void Gamepad_GamepadAdded(object sender, Gamepad e)
        {
            e.HeadsetConnected += E_HeadsetConnected;
            e.HeadsetDisconnected += E_HeadsetDisconnected;
            e.UserChanged += E_UserChanged;
            await Log("Gamepad Added");
            this.controller = e;
            this._run = new Task(this.ReadingGamepad);
            this._run.Start();
        }

        private async void Gamepad_GamepadRemoved(object sender, Gamepad e)
        {
            await Log("Gamepad Removed");
        }
        private async void E_UserChanged(IGameController sender, Windows.System.UserChangedEventArgs args)
        {
            await Log("User changed");
        }

        private async void E_HeadsetDisconnected(IGameController sender, Headset args)
        {
            await Log("HeadsetDisconnected");
        }

        private async void E_HeadsetConnected(IGameController sender, Headset args)
        {
            await Log("HeadsetConnected");
        }

        #endregion

        //private void dispatcherTimer_Tick(object sender, object e)
        private async void ReadingGamepad()
        {
            //if (Gamepad.Gamepads.Count > 0)
            while (true)
            {
                if (controller == null || Gamepad.Gamepads.Count <= 0)
                {
                    break;
                }
                controller = Gamepad.Gamepads.First();

                reading = this.controller.GetCurrentReading();

                // UI and the task run at different levels.
                // Therefore, the dispatcher must input data
                // and the data from the ViewModel binding Async be treated.
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
                {
                    pbLeftThumbstickX.Value = reading.LeftThumbstickX;
                    pbLeftThumbstickY.Value = reading.LeftThumbstickY;
                    if (reading.LeftThumbstickX > 0.5)
                    {
                        ChangeVisibility(true, lbl8bitdoDPadRight); 
                    }
                    else
                    {
                        ChangeVisibility(false, lbl8bitdoDPadRight);
                    }
                    if (reading.LeftThumbstickX < -0.5)
                    {
                        ChangeVisibility(true, lbl8bitdoDPadLeft);
                    }
                    else
                    {
                        ChangeVisibility(false, lbl8bitdoDPadLeft);
                    }
                    if (reading.LeftThumbstickY > 0.5)
                    {
                        ChangeVisibility(true, lbl8bitdoDPadUp);
                    }
                    else
                    {
                        ChangeVisibility(false, lbl8bitdoDPadUp);
                    }
                    if (reading.LeftThumbstickY < -0.5)
                    {
                        ChangeVisibility(true, lbl8bitdoDPadDown);
                    }
                    else
                    {
                        ChangeVisibility(false, lbl8bitdoDPadDown);
                    }


                    pbRightThumbstickX.Value = reading.RightThumbstickX;
                    pbRightThumbstickY.Value = reading.RightThumbstickY;

                    pbLeftTrigger.Value = reading.LeftTrigger;
                    pbRightTrigger.Value = reading.RightTrigger;

                    //https://msdn.microsoft.com/en-us/library/windows/apps/windows.gaming.input.gamepadbuttons.aspx
                    ChangeVisibility(reading.Buttons.HasFlag(GamepadButtons.A), lblA);
                    ChangeVisibility(reading.Buttons.HasFlag(GamepadButtons.A), ell8bitdoB);
                    ChangeVisibility(reading.Buttons.HasFlag(GamepadButtons.B), lblB);
                    ChangeVisibility(reading.Buttons.HasFlag(GamepadButtons.B), ell8bitdoA);
                    ChangeVisibility(reading.Buttons.HasFlag(GamepadButtons.X), lblX);
                    ChangeVisibility(reading.Buttons.HasFlag(GamepadButtons.X), ell8bitdoY);
                    ChangeVisibility(reading.Buttons.HasFlag(GamepadButtons.Y), lblY);
                    ChangeVisibility(reading.Buttons.HasFlag(GamepadButtons.Y), ell8bitdoX);
                    ChangeVisibility(reading.Buttons.HasFlag(GamepadButtons.Menu), lblMenu);
                    ChangeVisibility(reading.Buttons.HasFlag(GamepadButtons.Menu), lbl8bitdoStart);
                    ChangeVisibility(reading.Buttons.HasFlag(GamepadButtons.DPadLeft), lblDPadLeft);
                    ChangeVisibility(reading.Buttons.HasFlag(GamepadButtons.DPadRight), lblDPadRight);
                    ChangeVisibility(reading.Buttons.HasFlag(GamepadButtons.DPadUp), lblDPadUp);
                    ChangeVisibility(reading.Buttons.HasFlag(GamepadButtons.DPadDown), lblDPadDown);
                    ChangeVisibility(reading.Buttons.HasFlag(GamepadButtons.View), lblView);
                    ChangeVisibility(reading.Buttons.HasFlag(GamepadButtons.View), lbl8bitdoSelect);
                    ChangeVisibility(reading.Buttons.HasFlag(GamepadButtons.RightThumbstick), ellRightThumbstick);
                    ChangeVisibility(reading.Buttons.HasFlag(GamepadButtons.LeftThumbstick), ellLeftThumbstick);
                    ChangeVisibility(reading.Buttons.HasFlag(GamepadButtons.LeftShoulder), rectLeftShoulder);
                    ChangeVisibility(reading.Buttons.HasFlag(GamepadButtons.LeftShoulder), rect8bitdoLS);
                    ChangeVisibility(reading.Buttons.HasFlag(GamepadButtons.RightShoulder), rectRightShoulder);
                    ChangeVisibility(reading.Buttons.HasFlag(GamepadButtons.RightShoulder), rect8bitdoRS);

                    //// prevent the cursor from going outside the window of the app
                    //if (x < 0) { x = 0; }
                    //if (x > ScreenWidth) { x = ScreenWidth; }
                    //if (y < 0) { y = 0; }
                    //if (y > ScreenHeight) { y = ScreenHeight; }

                    if (reading.LeftThumbstickY > Deadzone || reading.LeftThumbstickY < -Deadzone || reading.LeftThumbstickX > Deadzone || reading.LeftThumbstickX < -Deadzone)
                    {
                        //Debug.WriteLine("x= " + (int)x + " y= " + (int)y);
                        info1 = new InjectedInputMouseInfo();
                        info1.MouseOptions = InjectedInputMouseOptions.Move;
                        if ((reading.LeftThumbstickX > Deadzone || reading.LeftThumbstickX < -Deadzone) && x >= 0 && x <= ScreenWidth)
                        {
                            info1.DeltaX = (int)(reading.LeftThumbstickX * 10);
                            //Debug.WriteLine("info.DeltaX  = " + info1.DeltaX);
                        }
                        if ((reading.LeftThumbstickY > Deadzone || reading.LeftThumbstickY < -Deadzone) && y >= 0 && y <= ScreenHeight)
                        {
                            info1.DeltaY = (int)(-reading.LeftThumbstickY * 10);
                            //Debug.WriteLine("info.DeltaY = " + info1.DeltaY);
                        }
                        InputInjector inputInjector = InputInjector.TryCreate();
                        inputInjector.InjectMouseInput(new[] { info1 });
                    }
                });
                // delay while loop interval
                await Task.Delay(DelayReadingGamepad);
            }
        }

        private void ChangeVisibility(bool flag, UIElement elem)
        {
            if (flag)
            { elem.Visibility = Visibility.Visible; }
            else
            { elem.Visibility = Visibility.Collapsed; }
        }

        private async Task Log(String txt)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                txtEvents.Text = DateTime.Now.ToString("hh:mm:ss.fff ") + txt + "\n" + txtEvents.Text;
            }
            );
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("button1 ckicked");
        }
        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("button2 ckicked");
        }


        private async void mainpage_KeyUp(CoreWindow sender, KeyEventArgs args)
        {
            if (reading.Buttons.HasFlag(GamepadButtons.RightShoulder) == true)
            {
                var info = new InjectedInputMouseInfo();
                info.MouseOptions = InjectedInputMouseOptions.Wheel;
                unchecked
                {
                    info.MouseData = (uint)+50; //scroll up
                }
                InputInjector inputInjector = InputInjector.TryCreate();
                inputInjector.InjectMouseInput(new[] { info });
                Debug.WriteLine("wheel up");
            }
            else if (reading.Buttons.HasFlag(GamepadButtons.LeftShoulder) == true)
            {
                var info = new InjectedInputMouseInfo();
                info.MouseOptions = InjectedInputMouseOptions.Wheel;
                unchecked
                {
                    info.MouseData = (uint)-50; //scroll down
                }
                InputInjector inputInjector = InputInjector.TryCreate();
                inputInjector.InjectMouseInput(new[] { info });
                Debug.WriteLine("wheel down");
            }
            //else if (reading.Buttons.HasFlag(GamepadButtons.A) == true)
            //{
            //    var down = new InjectedInputMouseInfo();
            //    down.MouseOptions = InjectedInputMouseOptions.LeftDown;
            //    var up = new InjectedInputMouseInfo();
            //    up.MouseOptions = InjectedInputMouseOptions.LeftUp;
            //    InputInjector inputInjector = InputInjector.TryCreate();
            //    inputInjector.InjectMouseInput(new[] { down, up });
            //    Debug.WriteLine("A pressed");
            //}
            if (GamepadButtons.A == (reading.Buttons & GamepadButtons.A))
            {
                down = new InjectedInputMouseInfo();
                down.MouseOptions = InjectedInputMouseOptions.LeftDown;
                Debug.WriteLine("A pressed");
            }
            else if (GamepadButtons.None == (reading.Buttons & GamepadButtons.A) && down != null)
            {
                var up = new InjectedInputMouseInfo();
                up.MouseOptions = InjectedInputMouseOptions.LeftUp;
                InputInjector inputInjector = InputInjector.TryCreate();
                inputInjector.InjectMouseInput(new[] { down, up });
                Debug.WriteLine("A released");
                down = null;
            }
            await Task.Delay(0);
           
            ////if (args.VirtualKey == VirtualKey.GamepadB)
            ////{
            ////    args.Handled = false;
            ////}
        }
        private void Mainpage_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            //if (e.OriginalKey == VirtualKey.Space)
            //{
            Debug.WriteLine("Originalkey= " + e.OriginalKey);
            //}
            //if (e.Key == Windows.System.VirtualKey.Escape)
            //{
            // do something or... nothing
            //Debug.WriteLine(e.Handled);
            //e.Handled = true;
            //Debug.WriteLine(e.Handled);
            Debug.WriteLine("key= " + e.Key);
            //e.Handled = true;
            //}
        }

        private void Mainpage_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            try
            {
                PointerPoint point = e.GetCurrentPoint(Mainpage);
                x = e.GetCurrentPoint(Mainpage).Position.X;
                y = e.GetCurrentPoint(Mainpage).Position.Y;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void TextBlock_SelectionChanged(object sender, RoutedEventArgs e)
        {

        }

        //protected override void OnNavigatedTo(NavigationEventArgs e)
        //{
        //    base.OnNavigatedTo(e);
        //    UpdateKeyState();
        //    Window.Current.CoreWindow.KeyDown += KeyEventHandler;
        //    Window.Current.CoreWindow.KeyUp += KeyEventHandler;
        //}

        //protected override void OnKeyDown(KeyRoutedEventArgs e)
        //{
        //    // Override arrow key behaviors.
        //    if (
        //        e.Key == Windows.System.VirtualKey.Up ||
        //            e.Key == Windows.System.VirtualKey.Left ||
        //                e.Key == Windows.System.VirtualKey.Right ||
        //                    e.Key == Windows.System.VirtualKey.Down ||
        //                        e.Key == Windows.System.VirtualKey.Space ||
        //                            e.OriginalKey == Windows.System.VirtualKey.GamepadLeftThumbstickUp ||
        //                                e.OriginalKey == Windows.System.VirtualKey.GamepadRightThumbstickButton
        //                                )
        //    {
        //        //base.OnKeyDown(e);
        //        e.Handled = true;
        //        //your logic
        //    }

        //    else
        //    {
        //        //FocusManager.TryMoveFocus(FocusNavigationDirection.None);
        //    }

        //}
        //protected override void OnKeyUp(KeyRoutedEventArgs e)
        //{
        //    // Override arrow key behaviors.
        //    if (
        //        e.Key == Windows.System.VirtualKey.Up ||
        //            e.Key == Windows.System.VirtualKey.Left ||
        //                e.Key == Windows.System.VirtualKey.Right ||
        //                    e.Key == Windows.System.VirtualKey.Down ||
        //                        e.Key == Windows.System.VirtualKey.Space ||
        //                            e.OriginalKey == Windows.System.VirtualKey.GamepadLeftThumbstickUp ||
        //                                e.OriginalKey == Windows.System.VirtualKey.GamepadRightThumbstickButton
        //                                )
        //    {
        //        //base.OnKeyUp(e);
        //        e.Handled = true;
        //        //your logic
        //    }

        //    else
        //    {
        //        //FocusManager.TryMoveFocus(FocusNavigationDirection.None);
        //    }

        //}



    }
}
