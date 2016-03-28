using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;
using Windows.UI;

namespace PixelClockEditor
{
    public class PaletteItemViewModel
    {
        public PaletteItemViewModel(Color color)
        {
            this.Color = color;
        }

        public Color Color { get; }
        public ReactiveCommand<object> Command { get; set; }
    }
}
