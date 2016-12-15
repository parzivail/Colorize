using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Newtonsoft.Json;
using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.Rendering;

namespace PdnColorize
{
    public class ColorizeEffect : Effect
    {
        private string paletteText;

        public ColorizeEffect()
            : base(StaticName, StaticIcon, "Colorizing", EffectFlags.SingleRenderCall)
        {
        }

        public static string StaticName => "Apply Palette";
        public static Bitmap StaticIcon => new Bitmap(typeof(ColorizeEffect), "icon.png");

        private void GetPalette()
        {
            paletteText = Clipboard.GetText();
        }

        protected override void OnSetRenderInfo(EffectConfigToken parameters, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            base.OnSetRenderInfo(parameters, dstArgs, srcArgs);

            var thread = new Thread(GetPalette);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        public override void Render(EffectConfigToken parameters, RenderArgs dstArgs, RenderArgs srcArgs,
            Rectangle[] rois, int startIndex, int length)
        {
            Dictionary<byte, ColorBgra> histogramDictionary;

            if (paletteText == null)
            {
                Decline("Clipboard does not contain text!");
                return;
            }

            try
            {
                histogramDictionary = JsonConvert.DeserializeObject<Dictionary<byte, ColorBgra>>(paletteText);
            }
            catch (Exception)
            {
                Decline("Clipboard contains text, but does not contain palette info!");
                return;
            }

            var gradient = new Gradient(histogramDictionary);

            for (var i = startIndex; i < startIndex + length; ++i)
            {
                var rect = rois[i];
                for (var y = rect.Top; y < rect.Bottom; ++y)
                    for (var x = rect.Left; x < rect.Right; ++x)
                    {
                        var colorHere = srcArgs.Surface.GetPoint(x, y);

                        dstArgs.Surface.SetPoint(x, y, gradient.GetPoint(colorHere.R));
                            // R will be fine because it's greyscale, all are the same anyway
                    }
            }
        }

        private void Decline(string reason)
        {
            var thread = new Thread(() => { MessageBox.Show(reason, StaticName, MessageBoxButtons.OK); });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }
    }

    public class Gradient
    {
        private readonly Dictionary<byte, ColorBgra> _map;

        public Gradient(IReadOnlyDictionary<byte, ColorBgra> histogramDictionary)
        {
            _map = new Dictionary<byte, ColorBgra>();

            for (var i = 0; i < 256; i++)
            {
                if (histogramDictionary.ContainsKey((byte) i))
                {
                    _map.Add((byte) i, histogramDictionary[(byte) i]);
                    continue;
                }

                int lower, higher;
                for (lower = i; lower >= 0; lower--)
                    if (histogramDictionary.ContainsKey((byte) lower))
                        break;
                for (higher = i; higher < histogramDictionary.Count; higher++)
                    if (histogramDictionary.ContainsKey((byte) higher))
                        break;

                var percent = (i - (float) lower)/(higher - (float) lower);
                var ca = histogramDictionary[(byte) lower];
                var cb = histogramDictionary[(byte) higher];

                var v = 1 - percent;
                var r = ca.R*percent + cb.R*v;
                var g = ca.G*percent + cb.G*v;
                var b = ca.B*percent + cb.B*v;

                _map.Add((byte) i, ColorBgra.FromBgr((byte) b, (byte) g, (byte) r));
            }
        }

        public ColorBgra GetPoint(byte colorHereR)
        {
            return _map[colorHereR];
        }
    }
}