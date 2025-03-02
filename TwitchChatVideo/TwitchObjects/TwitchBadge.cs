using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using TwitchChatVideo.Properties;

namespace TwitchChatVideo
{
    public class Badges
    {
        public const String BaseDir = "./badges/";
        private Dictionary<String, Gql.Badge> lookup;
        private Dictionary<String, Image> image_cache;
        private String id;

        public Badges(String id)
        {
            this.id = id;
            this.lookup = new Dictionary<string, Gql.Badge>();
            this.image_cache = new Dictionary<string, Image>();
            Directory.CreateDirectory(BaseDir + id);
        }

        public Badges(String id, Dictionary<String, Gql.Badge> lookup)
        {
            this.id = id;
            this.lookup = lookup;
            this.image_cache = new Dictionary<string, Image>();
            Directory.CreateDirectory(BaseDir + id);
        }

        private Badges() { }

        public static Badges SampleBadges = new Badges()
        {
            id = "",
            image_cache = new Dictionary<string, Image>()
            {
                {  "/broadcaster-1", Resources.broadcaster_1 },
                {  "/partner-1", Resources.partner_1 },
                {  "/subscriber-1", Resources.subscriber_1 },
                {  "/subscriber-3", Resources.subscriber_3 },
                {  "/subscriber-6", Resources.subscriber_6 },
                {  "/subscriber-12", Resources.subscriber_6 },
                {  "/moderator-1", Resources.moderator_1 },
                {  "/bits-1", Resources.bits_1 },
                {  "/bits-100", Resources.bits_100 },
                {  "/bits-1000", Resources.bits_1000 },
                {  "/bits-5000", Resources.bits_5000 },
                {  "/bits-10000", Resources.bits_10000 },
                {  "/bits-25000", Resources.bits_25000 },
                {  "/bits-50000", Resources.bits_50000 },
                {  "/bits-100000", Resources.bits_100000 },
                {  "/bits-charity-1", Resources.bits_charity_1 },
                {  "/bits-leader-1", Resources.bits_leader_1 },
                {  "/bits-turbo-1", Resources.turbo_1 },
                {  "/bits-premium-1", Resources.premium_1 },
                {  "/vip-1", Resources.vip_1 },
            },

        };

        public Image Lookup(String badgeID)
        {
            var concat = id + "/" + badgeID;

            if (image_cache.ContainsKey(concat))
            {
                return image_cache[concat];
            }

            if (!lookup.TryGetValue(badgeID, out var badge))
            {
                if (badgeID == "Ozs=")
                    return null;
                throw new ApplicationException($"Badge {badgeID} not found");
            }

            var local_path = BaseDir + concat + ".png";
            var img = TwitchDownloader.GetImage(local_path, badge.Image1x);

            image_cache[concat] = img;

            return img;
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
