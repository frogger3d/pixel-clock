using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Reactive.Linq;
using Microsoft.Maker.Firmata;
using Microsoft.Maker.RemoteWiring;
using Microsoft.Maker.Serial;
using ReactiveUI;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.UI;

namespace PixelClockEditor
{
    public class MainViewModel : ReactiveObject
    {
        private UwpFirmata firmata;
        private UsbSerial usbSerial;
        private RemoteDevice arduino;
        private string text;
        private string clockMode;
        private Color color;
        private PixelViewModel seletedPixel;

        public MainViewModel()
        {
            this.SetClockMode = ReactiveCommand.Create();
            this.SetClockMode.Subscribe(_ =>
            {
                int mode;
                if (int.TryParse(this.ClockMode, out mode))
                {
                    firmata.sendSysex((byte)'c', new byte[] { (byte)mode }.AsBuffer());
                    firmata.flush();
                }
            });

            this.SendColor = ReactiveCommand.Create();
            this.SendColor.Subscribe(_ =>
            {
                this.SendColorImpl(this.Color);
            });

            this.SetTime = ReactiveCommand.Create();
            this.SetTime.Subscribe(_ =>
            {

                var now = DateTime.Now;
                firmata.sendSysex((byte)'t', new byte[] { (byte)now.Hour, (byte)now.Minute, (byte)now.Second }.AsBuffer());
                firmata.flush();
            });

            this.Color = Colors.White;

            this.Pixels = Enumerable.Range(0, 64)
                .Select(i => new PixelViewModel() { PixelIndex = i.ToString() })
                .ToList();

            this.WhenAnyValue(v => v.Color)
                .Subscribe(c => 
                {
                    if (this.SelectedPixel != null)
                    {
                        this.SelectedPixel.Color = c;
                    }
                });

            this.WhenAnyValue(v => v.SelectedPixel)
                .Subscribe(p =>
                {
                    if (p != null)
                    {
                        p.Color = this.Color;
                    }
                });

            this.WhenAnyValue(v => v.Color)
                .Throttle(TimeSpan.FromSeconds(1))
                .ObserveOnDispatcher()
                .Subscribe(color =>
                {
                    if (Palette.All(p => p.Color != color))
                    {
                        while (this.Palette.Count > 7)
                        {
                            Palette.RemoveAt(7);
                        }
                        var vm = new PaletteItemViewModel(color);
                        vm.Command = ReactiveCommand.Create();
                        vm.Command.Subscribe(_ => this.Color = vm.Color);
                        Palette.Insert(0, vm);
                    }
                });

            this.Palette = new ReactiveList<PaletteItemViewModel>(
                Enumerable.Range(0, 8)
                    .Select(i =>
                    {
                        var vm = new PaletteItemViewModel(Colors.Black);
                        vm.Command = ReactiveCommand.Create();
                        vm.Command.Subscribe(_ => this.Color = vm.Color);
                        return vm;
                    }));
        }

        public string Text
        {
            get { return this.text; }
            set { this.RaiseAndSetIfChanged(ref this.text, value); }
        }

        public string ClockMode
        {
            get { return this.clockMode; }
            set { this.RaiseAndSetIfChanged(ref this.clockMode, value); }
        }

        public Color Color
        {
            get { return this.color; }
            set { this.RaiseAndSetIfChanged(ref this.color, value); }
        }

        public PixelViewModel SelectedPixel
        {
            get { return this.seletedPixel; }
            set { this.RaiseAndSetIfChanged(ref this.seletedPixel, value); }
        }

        public List<PixelViewModel> Pixels { get; set; }

        public ReactiveList<PaletteItemViewModel> Palette { get; set; }

        public ReactiveCommand<object> SendColor { get; }
        public ReactiveCommand<object> SetTime { get; }
        public ReactiveCommand<object> SetClockMode { get; private set; }

        public async void Initialize()
        {
            var deviceSelector = SerialDevice.GetDeviceSelector("COM4");
            var deviceInfos = await DeviceInformation.FindAllAsync(deviceSelector);
            var deviceInfo = deviceInfos.FirstOrDefault();
            if (deviceInfo != null)
            {
                this.firmata = new UwpFirmata();
                this.firmata.FirmataConnectionReady += Firmata_FirmataConnectionReady;
                this.firmata.FirmataConnectionFailed += Firmata_FirmataConnectionFailed;
                this.firmata.FirmataConnectionLost += Firmata_FirmataConnectionLost;
                this.arduino = new RemoteDevice(firmata);
                this.usbSerial = new UsbSerial(deviceInfo);

                this.firmata.begin(usbSerial);
                this.usbSerial.begin(9600, SerialConfig.SERIAL_8N1);
            }
        }

        private void Firmata_FirmataConnectionLost(string message)
        {
            this.Text = "Firmata connection lost " + message;
        }

        private void Firmata_FirmataConnectionFailed(string message)
        {
            this.Text = "Firmata connection failed " + message;
        }

        private void Firmata_FirmataConnectionReady()
        {
            this.Text = "Firmata connection ready";
        }

        private void SendColorImpl(Color color)
        {
            foreach (int pixel in Enumerable.Range(0, 64))
            {
                firmata.sendSysex((byte)'s', PixelMessage(pixel, color).AsBuffer());
                firmata.flush();
            }
        }

        public byte[] PixelMessage(int pixel, Color color)
        {
            return new byte[] { color.R, color.G, color.B, (byte)(pixel & 0xff), };
        }
    }

    public class PixelViewModel : ReactiveObject
    {
        private Color color;
        public Color Color
        {
            get { return this.color; }
            set { this.RaiseAndSetIfChanged(ref this.color, value); }
        }
        public string PixelIndex { get; set; }
    }
}
