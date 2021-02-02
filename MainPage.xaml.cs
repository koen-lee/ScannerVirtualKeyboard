using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Input.Preview.Injection;
using Windows.UI.ViewManagement;
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
            _scanner.BottomText = "The code will be typed in the active app";
            //Start scanning
            _scanner.AutoFocus();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await SwitchToCompact();
            await _scanner.Scan(GetOptions()).ContinueWith(t => HandleScanResult(t.Result));
        }


        async Task SwitchToCompact()
        {
            var preferences = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
            preferences.CustomSize = new Windows.Foundation.Size(200, 200);
            await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, preferences);
        }

        private static MobileBarcodeScanningOptions GetOptions()
        {
            return new MobileBarcodeScanningOptions
            {
                CameraResolutionSelector = resolutions =>
                    resolutions.OrderBy(r => Math.Abs(r.Width - 640)).First(),

            };
        }

        async void HandleScanResult(ZXing.Result result)
        {
            if (result != null && !string.IsNullOrEmpty(result.Text))
            {
                var text = result.Text;

                Console.WriteLine("Found Barcode: " + text);
                InjectText(text);
                await Task.Delay(TimeSpan.FromMilliseconds(100));
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
