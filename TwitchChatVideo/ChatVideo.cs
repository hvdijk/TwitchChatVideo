using Accord.Video.FFMPEG;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static TwitchChatVideo.ChatHandler;

namespace TwitchChatVideo
{
    public class ChatVideo
    {
        public const string OutputDirectory = "./output/";
        public const string LogDirectory = "./logs/";
        public const int EmotePad = 3;
        public const int BadgePad = 3;
        public const int HorizontalPad = 5;
        public const int VerticalPad = 5;

        public const int FPS = 30;
        public const VideoCodec Codec = VideoCodec.H264;

        public string JsonInputFileName { get; internal set; }
        public string VideoOutputFileName { get; internal set; }
        public Color BGColor { get; internal set; }
        public Color ChatColor { get; internal set; }
        public int Width { get; internal set; }
        public int Height { get; internal set; }
        public string FontFamily { get; internal set; }
        public float FontSize { get; internal set; }
        public float LineSpacing { get; internal set; }
        public bool ShowBadges { get; internal set; }

        public ChatVideo(ViewModel vm)
        {
            JsonInputFileName = vm.FileName;
            VideoOutputFileName = Path.ChangeExtension(JsonInputFileName, ".mp4");
            LineSpacing = vm.LineSpacing;
            BGColor = vm.BGColor.ToDrawingColor();
            ChatColor = vm.ChatColor.ToDrawingColor();
            Width = (int)vm.Width;
            Height = (int)vm.Height;
            FontFamily = vm.FontFamily.ToString();
            FontSize = vm.FontSize;
            ShowBadges = vm.ShowBadges;
        }

        public async Task<bool> CreateVideoAsync(IProgress<VideoProgress> progress, CancellationToken ct)
        {
            return await Task.Run(async () =>
            {
                Gql.Query query;
                using (var f = new StreamReader(JsonInputFileName, Encoding.Default))
                using (var r = new JsonTextReader(f))
                {
                    query = JToken.ReadFrom(r).ToObject<Gql.Query>();
                }

                Bits bits = null; // await TwitchDownloader.DownloadBitsAsync(video.StreamerID, progress, ct);
                Badges badges = new Badges(query.Video.Owner.Id, query.Badges.ToDictionary(b => b.ID)); // await TwitchDownloader.DownloadBadgesAsync(video.StreamerID, progress, ct);
                var bttv = await BTTV.CreateAsync(query.Video.Owner.Id, progress, ct);
                FFZ ffz = null; // var ffz = await FFZ.CreateAsync(video.Streamer, progress, ct);

                if (ct.IsCancellationRequested)
                {
                    return false;
                }

                using (var chat_handler = new ChatHandler(this, bttv, ffz, badges, bits))
                {
                    try
                    {
                        var messages = new List<DrawableMessage>(query.Comments.Count);
                        try
                        {
                            foreach (var comment in query.Comments)
                            {
                                messages.Add(chat_handler.MakeDrawableMessage(comment));
                                progress?.Report(new VideoProgress(messages.Count, query.Comments.Count, VideoProgress.VideoStatus.Rendering));
                            }
                            var result = await WriteVideoFrames(VideoOutputFileName, messages, 0, query.Video.LengthInSeconds * FPS, progress, ct);
                            progress?.Report(new VideoProgress(1, 1, VideoProgress.VideoStatus.CleaningUp));
                            return result;
                        }
                        finally
                        {
                            foreach (var message in messages)
                                message.Dispose();
                        }
                    }
                    catch (Exception e)
                    {
                        using (StreamWriter w = File.AppendText("error.txt"))
                        {
                            w.WriteLine($"{DateTime.Now.ToLongTimeString()} : {e.ToString()}");
                        }

                        return false;
                    }
                }
            });
        }

        public async Task<bool> WriteVideoFrames(string path, List<DrawableMessage> messages, long start_frame, long end_frame, IProgress<VideoProgress> progress = null, CancellationToken ct = default(CancellationToken))
        {
            return await Task.Run(() =>
            {
                using (var writer = new VideoFileWriter())
                {
                    using (var bmp = new Bitmap(Width, Height))
                    {
                        var previousMessages = new List<DrawableMessage>(messages.Count);

                        writer.Open(path, Width, Height, FPS, Codec);
                        var bounds = new Rectangle(0, 0, Width, Height);

                        int next_message = 0;

                        for (long i = start_frame; i < end_frame; i++)
                        {
                            var time = TimeSpan.FromSeconds((double)i / FPS);

                            if (ct.IsCancellationRequested)
                            {
                                progress?.Report(new VideoProgress(0, 1, VideoProgress.VideoStatus.Idle));
                                return false;
                            }

                            progress?.Report(new VideoProgress(i, end_frame, VideoProgress.VideoStatus.Rendering));

                            if (next_message < messages.Count)
                            {
                                var message = messages[next_message];
                                if (message.Start.TotalSeconds * FPS <= i)
                                {
                                    previousMessages.Add(message);
                                    next_message++;
                                }
                            }

                            DrawFrame(bmp, previousMessages, time);

                            writer.WriteVideoFrame(bmp);
                        }

                        return true;
                    }
                }
            });


        }

        public static void DrawPreview(ViewModel vm, Bitmap bmp)
        {
            var chat = new ChatVideo(vm);

            var messages = ChatHandler.MakeSampleChat(chat);
            try
            {
                chat.DrawFrame(bmp, messages);
            }
            finally
            {
                foreach (var msg in messages)
                    msg.Dispose();
            }
        }

        /// <summary>
        /// Draws a list of chat messages on a supplied Bitmap
        /// </summary>
        public void DrawFrame(Bitmap bitmap, List<DrawableMessage> drawables, TimeSpan time = default(TimeSpan))
        {
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(BGColor);

                PointF point = new PointF(0, bitmap.Height - VerticalPad);

                for (int i = drawables.Count; --i >= 0;)
                {
                    var message = drawables[i];
                    point.Y -= message.MainBitmap.Height;
                    g.DrawImage(message.MainBitmap, point);
                    foreach (var eb in message.EmbeddedBitmaps)
                    {
                        eb.SetFrame(time);
                        g.DrawImage(eb.Image, point.X + eb.Point.X, point.Y + eb.Point.Y, eb.Image.Width, eb.Image.Height);
                    }
                    point.Y -= LineSpacing;
                    if (point.Y <= 0) break;
                }

                using (var brush = new SolidBrush(BGColor))
                {
                    g.FillRectangle(brush, new Rectangle(0, 0, Width, VerticalPad));
                    g.FillRectangle(brush, new Rectangle(0, 0, HorizontalPad, Height));
                    g.FillRectangle(brush, new Rectangle(Width - HorizontalPad, 0, Width, Height));
                    g.FillRectangle(brush, new Rectangle(0, Height - VerticalPad, Width, Height));
                }
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
