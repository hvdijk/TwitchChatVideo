using System.Drawing;
using System.IO;
using System.Net;
using System.Windows.Media.Imaging;

namespace TwitchChatVideo
{
    class TwitchDownloader
    {
        public static byte[] GetFile(string local_path, string url)
        {
            if (!File.Exists(local_path))
            {
                try
                {
                    var request = WebRequest.Create(url);
                    request.Timeout = 10000;
                    using (var response = request.GetResponse())
                    using (var responseStream = response.GetResponseStream())
                    using (var fileStream = File.OpenWrite(local_path))
                        responseStream.CopyTo(fileStream);
                }
                catch (WebException)
                {
                    return null;
                }
            }

            return File.ReadAllBytes(local_path);
        }

        public static Bitmap GetImage(string local_path, string url)
        {
            var bytes = GetFile(local_path, url);
            if (bytes == null) return null;

            return new Bitmap(new MemoryStream(bytes));
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
