using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using DirectN;

namespace TwitchChatVideo
{
    public partial class ChatHandler
    {
        public class DrawableMessage : IDisposable
        {
            public class EmbeddedImage : IDWriteInlineObject
            {
                public EmbeddedImage(Image image)
                {
                    Image = image ?? throw new ArgumentNullException(nameof(image));

                    foreach (var dimension in Image.FrameDimensionsList)
                    {
                        if (dimension != FrameDimension.Time.Guid)
                            continue;

                        const int FrameDelayPropId = 0x5100;
                        var FrameDelayBytes = image.GetPropertyItem(FrameDelayPropId).Value;
                        Debug.Assert(FrameDelayBytes.Length % 4 == 0);
                        FrameDelay = new TimeSpan[FrameDelayBytes.Length / 4];
                        for (int i = 0; i < FrameDelay.Length; ++i)
                        {
                            FrameDelay[i] = TimeSpan.FromMilliseconds(10 * BitConverter.ToInt32(FrameDelayBytes, i * 4));
                            TotalFrameDelay += FrameDelay[i];
                        }
                        break;
                    }
                }

                public Image Image { get; }
                private TimeSpan[] FrameDelay { get; }
                public TimeSpan TotalFrameDelay { get; }

                public PointF Point { get; private set; }

                public void SetFrame(TimeSpan time)
                {
                    if (TotalFrameDelay == default(TimeSpan)) return;
                    time = TimeSpan.FromTicks(time.Ticks % TotalFrameDelay.Ticks);
                    for (int i = 0; ; ++i)
                    {
                        if (time < FrameDelay[i])
                        {
                            Image.SelectActiveFrame(FrameDimension.Time, i);
                            return;
                        }

                        time -= FrameDelay[i];
                    }
                }

                HRESULT IDWriteInlineObject.Draw(IntPtr clientDrawingContext, IDWriteTextRenderer renderer, float originX, float originY, bool isSideways, bool isRightToLeft, object clientDrawingEffect)
                {
                    using (new ComObject<IDWriteTextRenderer>(renderer))
                    {
                        Point = new PointF(originX, originY);
                    }

                    return 0;
                }

                HRESULT IDWriteInlineObject.GetMetrics(out DWRITE_INLINE_OBJECT_METRICS metrics)
                {
                    var size = Image.Size;

                    metrics = default(DWRITE_INLINE_OBJECT_METRICS);
                    metrics.width = size.Width + ChatVideo.BadgePad;
                    metrics.height = size.Height;
                    metrics.baseline = size.Height / 2.0f + 5;

                    return 0;
                }

                HRESULT IDWriteInlineObject.GetOverhangMetrics(out DWRITE_OVERHANG_METRICS overhangs)
                {
                    overhangs = default(DWRITE_OVERHANG_METRICS);
                    overhangs.right = -ChatVideo.BadgePad;

                    return 0;
                }

                HRESULT IDWriteInlineObject.GetBreakConditions(out DWRITE_BREAK_CONDITION breakConditionBefore, out DWRITE_BREAK_CONDITION breakConditionAfter)
                {
                    breakConditionBefore = DWRITE_BREAK_CONDITION.DWRITE_BREAK_CONDITION_CAN_BREAK;
                    breakConditionAfter = DWRITE_BREAK_CONDITION.DWRITE_BREAK_CONDITION_CAN_BREAK;

                    return 0;
                }
            }

            public Bitmap MainBitmap { get; internal set; }
            public List<EmbeddedImage> EmbeddedBitmaps { get; internal set; }
            public TimeSpan Start { get; internal set; }

            public void Dispose()
            {
                MainBitmap?.Dispose();
            }
        }
    }
}

/*
    Twitch Chat Video

    Copyright (C) 2019 Cair

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
