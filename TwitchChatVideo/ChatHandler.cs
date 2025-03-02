using DirectN;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TwitchChatVideo.Gql;
using TwitchChatVideo.Properties;

namespace TwitchChatVideo
{
    public partial class ChatHandler : IDisposable
    {
        public IComObject<IDWriteFactory> DWriteFactory { get; }
        public IComObject<ID2D1Factory> D2D1Factory { get; }
        public IComObject<IDWriteTextFormat> TextFormat { get; }
        public Color ChatColor { get; }
        public Color BGColor { get; }
        public float Spacing { get; }
        public bool ShowBadges { get; }
        public float Width { get; }
        public BTTV BTTV { get; }
        public FFZ FFZ { get; }
        public Badges Badges { get; }
        public Bits Bits { get; }

        public ChatHandler(ChatVideo cv, BTTV bttv, FFZ ffz, Badges badges, Bits bits)
        {
            DWriteFactory = DWriteFunctions.DWriteCreateFactory();
            D2D1Factory  = D2D1Functions.D2D1CreateFactory();
            TextFormat = DWriteFactory.CreateTextFormat(cv.FontFamily, cv.FontSize * 96 / 72);
            ChatColor = cv.ChatColor;
            BGColor = cv.BGColor;
            Width = cv.Width;
            Spacing = cv.LineSpacing;
            ShowBadges = cv.ShowBadges;
            BTTV = bttv;
            FFZ = ffz;
            Badges = badges;
            Bits = bits;
        }

        public void Dispose()
        {
            TextFormat?.Dispose();
            D2D1Factory?.Dispose();
            DWriteFactory?.Dispose();
        }

        static readonly Regex Words = new Regex(@"\w+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public DrawableMessage MakeDrawableMessage(VideoComment comment)
        {
            var builder = new StringBuilder();
            Action<IDWriteTextLayout> OnCreateTextLayout = null;
            Action<IDWriteTextLayout, ID2D1DCRenderTarget> OnDrawTextLayout = null;

            var embeddedBitmaps = new List<DrawableMessage.EmbeddedImage>();

            foreach (var badge in comment.Message.UserBadges ?? Enumerable.Empty<Gql.Badge>())
            {
                var badgeImage = Badges.Lookup(badge.ID);
                if (badgeImage != null)
                {
                    var badgeStart = builder.Length;
                    builder.Append("*");
                    var badgeEnd = builder.Length;

                    var range = new DWRITE_TEXT_RANGE((uint)badgeStart, (uint)badgeEnd - (uint)badgeStart);

                    var embeddedBitmap = new DrawableMessage.EmbeddedImage(badgeImage);
                    embeddedBitmaps.Add(embeddedBitmap);

                    OnCreateTextLayout += tl =>
                    {
                        tl.SetInlineObject(embeddedBitmap, range).ThrowOnError();
                    };
                }
            }

            {
                var userStart = builder.Length;
                builder.Append(comment.Commenter.DisplayName);
                builder.Append(": ");
                var userEnd = builder.Length;

                var range = new DWRITE_TEXT_RANGE((uint)userStart, (uint)userEnd - (uint)userStart);

                OnCreateTextLayout += tl =>
                {
                    tl.SetFontWeight(DWRITE_FONT_WEIGHT.DWRITE_FONT_WEIGHT_BOLD, range);
                };

                if (comment.Message.UserColor != null)
                {
                    var color = Colors.GetCorrected(comment.Message.UserColor.Value, BGColor, comment.Commenter.DisplayName);

                    OnDrawTextLayout += (tl, rt) =>
                    {
                        using (var brush = rt.CreateSolidColorBrush<ID2D1SolidColorBrush>(new _D3DCOLORVALUE(color.ToArgb())))
                            tl.SetDrawingEffect(brush.Object, range);
                    };
                }
            }

            foreach (var fragment in comment.Message.Fragments ?? Enumerable.Empty<VideoComment.VideoCommentMessageFragment>())
            {
                var fragmentStart = builder.Length;
                builder.Append(fragment.Text);
                var fragmentEnd = builder.Length;

                if (fragment.Emote != null)
                {
                    var range = new DWRITE_TEXT_RANGE((uint)fragmentStart, (uint)fragmentEnd - (uint)fragmentStart);

                    var embeddedBitmap = new DrawableMessage.EmbeddedImage(TwitchEmote.GetEmote(fragment.Emote.Value.EmoteID));
                    embeddedBitmaps.Add(embeddedBitmap);

                    OnCreateTextLayout += tl =>
                    {
                        tl.SetInlineObject(embeddedBitmap, range);
                    };
                }
                else
                {
                    foreach (Match word in Words.Matches(fragment.Text))
                    {
                        var emote = BTTV?.GetEmote(word.Value) ?? FFZ?.GetEmote(word.Value);
                        if (emote != null)
                        {
                            var range = new DWRITE_TEXT_RANGE((uint)fragmentStart + (uint)word.Index, (uint)word.Length);

                            var embeddedBitmap = new DrawableMessage.EmbeddedImage(emote);
                            embeddedBitmaps.Add(embeddedBitmap);

                            OnCreateTextLayout += tl =>
                            {
                                tl.SetInlineObject(embeddedBitmap, range);
                            };
                        }
                    }
                }
            }

            var textLayout = DWriteFactory.CreateTextLayout(TextFormat, builder.ToString(), maxWidth: Width - 2 * ChatVideo.HorizontalPad);
            OnCreateTextLayout?.Invoke(textLayout.Object);
            textLayout.Object.GetMetrics(out var textMetrics).ThrowOnError();

            var mainBitmap = new Bitmap((int)Math.Ceiling(Width), (int)Math.Ceiling(textMetrics.height));

            var renderTargetProperties = default(D2D1_RENDER_TARGET_PROPERTIES);
            renderTargetProperties.pixelFormat.format = DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM;
            renderTargetProperties.pixelFormat.alphaMode = D2D1_ALPHA_MODE.D2D1_ALPHA_MODE_IGNORE;

            using (var renderTarget = D2D1Factory.CreateDCRenderTarget(renderTargetProperties))
            using (var graphics = Graphics.FromImage(mainBitmap))
            {
                var handle = graphics.GetHdc();
                try
                {
                    renderTarget.Object.BindDC(handle, new tagRECT(0, 0, mainBitmap.Width, mainBitmap.Height));
                    renderTarget.BeginDraw();
                    try
                    {
                        renderTarget.Clear(new _D3DCOLORVALUE(BGColor.ToArgb()));
                        using (var chatBrush = renderTarget.CreateSolidColorBrush(new _D3DCOLORVALUE(ChatColor.ToArgb())))
                        {
                            OnDrawTextLayout?.Invoke(textLayout.Object, renderTarget.Object);
                            renderTarget.DrawTextLayout(new D2D_POINT_2F(ChatVideo.HorizontalPad, 0), textLayout, chatBrush, D2D1_DRAW_TEXT_OPTIONS.D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT);
                        }
                    }
                    finally
                    {
                        renderTarget.EndDraw();
                    }
                }
                finally
                {
                    graphics.ReleaseHdc(handle);
                }
            }

            return new DrawableMessage
            {
                Start = TimeSpan.FromSeconds(comment.ContentOffsetSeconds),
                MainBitmap = mainBitmap,
                EmbeddedBitmaps = embeddedBitmaps,
            };
        }

        public static List<DrawableMessage> MakeSampleChat(ChatVideo cv)
        {
            using (var ch = new ChatHandler(cv, null, FFZ.SampleFFZ, Badges.SampleBadges, null))
            {
                return MakeSampleComments().ConvertAll(m => ch.MakeDrawableMessage(m));
            }
        }


        public static List<VideoComment> MakeSampleComments()
        {
            using (var f = new StreamReader(new MemoryStream(Resources.SampleChat), Encoding.Default))
            using (var r = new JsonTextReader(f))
            {
                return JToken.ReadFrom(r).ToObject<List<VideoComment>>();
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
