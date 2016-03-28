using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
}
