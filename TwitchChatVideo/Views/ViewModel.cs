﻿using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using static TwitchChatVideo.VideoProgress;
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;

namespace TwitchChatVideo
{
    public class ViewModel : INotifyPropertyChanged
    {
        private string fileName;
        private uint width;
        private uint height;
        private FontFamily font_family;
        private float font_size;
        private Color bg_color;
        private Color chat_color;
        private bool show_badges;
        private float spacing;
        private bool running;
        private long total;
        private long progress;
        private VideoStatus status;
        private Visibility update_available;
        private bool allow_interaction;

        private CancellationTokenSource CancellationTokenSource { get; set; }

        public long Total { get => total; set => Set(ref total, value, false); }
        public long Progress { get => progress; set => Set(ref progress, value, false); }
        public VideoStatus Status { get => status; set => Set(ref status, value, false); }
        public String FileName { get => fileName; set => Set(ref fileName, value, false); }

        public uint Width { get => width; set => Set(ref width, Math.Min(3000, value)); }
        public uint Height { get => height; set => Set(ref height, Math.Min(3000, value)); }
        public FontFamily FontFamily { get => font_family; set => Set(ref font_family, value); }
        public float FontSize { get => font_size; set => Set(ref font_size, value); }
        public Color BGColor { get => bg_color; set => Set(ref bg_color, value); }
        public Color ChatColor { get => chat_color; set => Set(ref chat_color, value); }
        public bool ShowBadges { get => show_badges; set => Set(ref show_badges, value); }
        public float LineSpacing { get => spacing; set => Set(ref spacing, value); }
        public bool Running { get => running; set => Set(ref running, value); }
        public Visibility UpdateVisibility { get => update_available; set => Set(ref update_available, value); }
        public bool AllowInteraction { get => allow_interaction; set => Set(ref allow_interaction, value); }

        public ICommand CancelVideo { get; }
        public ICommand MakeVideo { get; }
        public ICommand MakePreviewWindow { get; }
        public ICommand SelectVideo { get; }

        public BitmapSource PreviewImage {
            get {
                using (var bmp = new Bitmap((int)Width, (int)Height))
                {
                    ChatVideo.DrawPreview(this, bmp);
                    var hbmp = bmp.GetHbitmap();
                    var img_source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hbmp, IntPtr.Zero, Int32Rect.Empty,
                    BitmapSizeOptions.FromWidthAndHeight((int)Width, (int)Height));
                    NativeMethods.DeleteObject(hbmp);
                    return img_source;
                }
            }
        }

        public class DelegateCommand : ICommand
        {

            private Func<object, bool> can_execute;
            private Action<object> execute;
            public event EventHandler CanExecuteChanged;

            public DelegateCommand(Action<object> execute) :
                this(execute, null)
            { }

            public DelegateCommand(Action<object> exe, Func<object, bool> ce)
            {
                execute = exe ?? throw new ArgumentNullException(nameof(exe));
                can_execute = ce;
            }

            public bool CanExecute(object param)
            {
                return can_execute?.Invoke(param) ?? true;
            }

            public void Execute(object param) => execute(param);

            public void RaiseCanExecuteChanged(EventArgs e)
            {
                CanExecuteChanged?.Invoke(this, e);
            }
        }

        public ViewModel()
        {
            var settings = Settings.Load();
            AllowInteraction = true;
            Width = settings.Width;
            Height = settings.Height;
            LineSpacing = settings.LineSpacing;
            FontFamily = settings.FontFamily;
            FontSize = settings.FontSize;
            BGColor = settings.BGColor;
            ChatColor = settings.ChatColor;
            ShowBadges = settings.ShowBadges;

            Total = 1;
            Progress = 0;

            SelectVideo = new DelegateCommand(ExecuteSelectVideo);
            MakeVideo = new DelegateCommand(ExecuteMakeVideo);
            CancelVideo = new DelegateCommand(ExecuteCancelVideo);
            MakePreviewWindow = new DelegateCommand(ExecuteMakePreviewWindow);
            Application.Current.MainWindow.Closing += new CancelEventHandler(SaveSettings);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string property_name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property_name));
        }

        protected void Set<T>(ref T field, T value, bool update_image = true, [CallerMemberName] string propertyName = null)
        {
            field = value;
            OnPropertyChanged(propertyName);
            if (update_image)
            {
                OnPropertyChanged("PreviewImage");
            }
        }

        private void ExecuteCancelVideo(object arg)
        {
            CancellationTokenSource?.Cancel();
        }

        private async void ExecuteMakeVideo(object arg)
        {
            Running = true;
            var c = new ChatVideo(this);
            using (var source = new CancellationTokenSource()) {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                CancellationTokenSource = source;
                var progress = new Progress<VideoProgress>(x =>
                {
                    Progress = x.Progress;
                    Total = x.Total;
                    Status = x.Status;
                });

                if(await c.CreateVideoAsync(progress, source.Token))
                {
                    sw.Stop();
                    var elapsed = sw.Elapsed;
                    FileName = "";
                    MessageBox.Show(String.Format("Video completed in {0:00}:{1:00}:{2:00}!", elapsed.Hours, elapsed.Minutes, elapsed.Seconds));
                }

                Progress = 0;
                Total = 1;
                Status = VideoStatus.Idle;
                CancellationTokenSource = null;
                Running = false;
            }
        }

        private void ExecuteSelectVideo(object arg)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "JSON files (*.json)|*.json";
            if (dialog.ShowDialog() == true)
            {
                FileName = dialog.FileName;
            }
        }

        private void ExecuteMakePreviewWindow(object arg)
        {
            new PreviewWindow(this).Show();
        }

        private void SaveSettings(object sender, CancelEventArgs e)
        {
            Settings.Save(this);
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
