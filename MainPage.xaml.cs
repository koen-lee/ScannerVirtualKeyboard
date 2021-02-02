using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input.Preview.Injection;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using ZXing.Mobile;

namespace ScannerVirtualKeyboard
{
    public sealed partial class MainPage : Page
    {
        private readonly MobileBarcodeScanner _scanner;
        private readonly InputInjector _injector;

        public MainPage()
        {
            InitializeComponent();

            _injector = InputInjector.TryCreate();
            if (_injector == null)
            {
                Console.WriteLine("Can't work, no InputInjector available");
                Application.Current.Exit();
            }

            //Create a new instance of our scanner
            _scanner = new MobileBarcodeScanner(Dispatcher);
            _scanner.RootFrame = Frame;
            _scanner.Dispatcher = Dispatcher;

            //Tell our scanner to use the default overlay
            _scanner.UseCustomOverlay = false;
            //We can customize the top and bottom text of our default overlay
            _scanner.TopText = "Hold camera up to barcode";
            _scanner.BottomText = "Camera will automatically scan barcode\r\n\r\nPress the 'Back' button to Cancel";
            //Start scanning
            _scanner.AutoFocus();
            _scanner.Scan(GetOptions()).ContinueWith(t =>
              {
                  if (t.Result != null)
                      HandleScanResult(t.Result);
              });
        }

        private static MobileBarcodeScanningOptions GetOptions()
        {
            return new MobileBarcodeScanningOptions
            {
                CameraResolutionSelector = resolutions => 
                    resolutions.OrderBy(r=>Math.Abs(r.Width-640)).First(),
                
            };
        }

        async void HandleScanResult(ZXing.Result result)
        {
            if (result != null && !string.IsNullOrEmpty(result.Text))
            {
                var text = result.Text;

                Console.WriteLine("Found Barcode: " + text);
                await Minimize();
                Thread.Sleep(TimeSpan.FromMilliseconds(200));
                InjectText(text);
                //Application.Exit() waits for the application to get focus again, so is unusable.
                Process.GetCurrentProcess().Kill();
            }
        }

        private void InjectText(string text)
        {
            foreach (var code in text.Select(GetCodeForChar))
            {
                PressKey(code);
            }
            PressKey(CreateInfo(VirtualKey.Tab));
        }

        private void PressKey(InjectedInputKeyboardInfo keyDown)
        {
            var keyUp = new InjectedInputKeyboardInfo
            {
                KeyOptions = keyDown.KeyOptions | InjectedInputKeyOptions.KeyUp,
                ScanCode = keyDown.ScanCode,
                VirtualKey = keyDown.VirtualKey
            };
            _injector.InjectKeyboardInput(new[] { keyDown, keyUp });
        }

        private InjectedInputKeyboardInfo GetCodeForChar(char arg)
        {
            return new InjectedInputKeyboardInfo
            {
                ScanCode = arg,
                KeyOptions = InjectedInputKeyOptions.Unicode,
            };
        }

        private InjectedInputKeyboardInfo CreateInfo(VirtualKey key)
        {
            return new InjectedInputKeyboardInfo { VirtualKey = (ushort)(int)key };

        }
        private async Task Minimize()
        {
            IList<AppDiagnosticInfo> infos = await AppDiagnosticInfo.RequestInfoForAppAsync();
            IList<AppResourceGroupInfo> resourceInfos = infos[0].GetResourceGroups();
            await resourceInfos[0].StartSuspendAsync();
        }
    }
}
