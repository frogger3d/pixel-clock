using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Maker.Firmata;
using Microsoft.Maker.RemoteWiring;
using Microsoft.Maker.Serial;
using Windows.ApplicationModel;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PixelClockEditor
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private UwpFirmata firmata;
        private UsbSerial usbSerial;
        private RemoteDevice arduino;

        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (DesignMode.DesignModeEnabled) return;
            this.Initialize();
        }

        private void redclick(object sender, RoutedEventArgs e)
        {
            setcolor(Colors.DarkRed);
        }

        private void greenclick(object sender, RoutedEventArgs e)
        {
            setcolor(Colors.DarkGreen);
        }

        private void blueclick(object sender, RoutedEventArgs e)
        {
            setcolor(Colors.DarkBlue);
        }

        private void BlackClick(object sender, RoutedEventArgs e)
        {
            setcolor(Colors.Black);
        }

        private void setcolor(Color color)
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

        private void SetClockMode(object sender, RoutedEventArgs e)
        {
            int mode;
            if (int.TryParse(this.clockMode.Text, out mode))
            {
                firmata.sendSysex((byte)'c', new byte[] { (byte)mode }.AsBuffer());
                firmata.flush();
            }
        }

        private void SetTime(object sender, RoutedEventArgs e)
        {
            var now = DateTime.Now;
            firmata.sendSysex((byte)'t', new byte[] { (byte)now.Hour, (byte)now.Minute, (byte)now.Second }.AsBuffer());
            firmata.flush();
        }

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
            this.rcvdText.Text = "Firmata connection lost " + message;
        }

        private void Firmata_FirmataConnectionFailed(string message)
        {
            this.rcvdText.Text = "Firmata connection failed " + message;
        }

        private void Firmata_FirmataConnectionReady()
        {
            this.rcvdText.Text = "Firmata connection ready";
        }
    }
}
