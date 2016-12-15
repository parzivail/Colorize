using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Newtonsoft.Json;
using PaintDotNet;
using PaintDotNet.Effects;

namespace PdnColorize
{
    public class GeneratePaletteEffect : Effect
    {
        public GeneratePaletteEffect()
            : base(StaticName, StaticIcon, "Colorizing", EffectFlags.SingleRenderCall)
        {
        }

        public static string StaticName => "Palette from Selection";
        public static Bitmap StaticIcon => new Bitmap(typeof(GeneratePaletteEffect), "icon.png");

        public override void Render(EffectConfigToken parameters, RenderArgs dstArgs, RenderArgs srcArgs,
            Rectangle[] rois, int startIndex, int length)
        {
            var histogramDictionary = new Dictionary<byte, ColorBgra>();

            var selectionRegion = EnvironmentParameters.GetSelection(srcArgs.Bounds);
            for (var i = startIndex; i < startIndex + length; ++i)
            {
                var rect = rois[i];
                for (var y = rect.Top; y < rect.Bottom; ++y)
                    for (var x = rect.Left; x < rect.Right; ++x)
                    {
                        var colorHere = srcArgs.Surface.GetPoint(x, y);

                        var mapped = (byte)((colorHere.R + colorHere.G + colorHere.B)/3f);
                        if (!histogramDictionary.ContainsKey(mapped))
                            histogramDictionary.Add(mapped, colorHere);
                    }
            }

            var sorted = from entry in histogramDictionary orderby entry.Key ascending select entry;

            lock (ColorizeEffect.sync)
            {
                Clipboard.SetText(JsonConvert.SerializeObject(sorted));
                MessageBox.Show("Copied palette data to clipboard!", StaticName, MessageBoxButtons.OK);
            }

//            var thread = new Thread(() => {
//                Clipboard.SetText(JsonConvert.SerializeObject(sorted));
//                MessageBox.Show("Copied palette data to clipboard!", StaticName, MessageBoxButtons.OK);
//            });
//            thread.SetApartmentState(ApartmentState.STA);
//            thread.Start();
        }
    }
}