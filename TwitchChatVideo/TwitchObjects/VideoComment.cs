using System;
using System.Collections.Generic;
using System.Drawing;
using System.Security.RightsManagement;
using Newtonsoft.Json;

namespace TwitchChatVideo.Gql
{
    public struct Badge
    {
        [JsonProperty("id")]
        public string ID { get; set; }
        [JsonProperty("setID")]
        public string SetID { get; set; }
        [JsonProperty("version")]
        public string Version { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("image1x")]
        public string Image1x { get; set; }
        [JsonProperty("image2x")]
        public string Image2x { get; set; }
        [JsonProperty("image4x")]
        public string Image4x { get; set; }
        [JsonProperty("clickAction")]
        public string ClickAction { get; set; }
        [JsonProperty("clickURL")]
        public string ClickUrl { get; set; }
    }

    public struct Query
    {
        [JsonProperty("badges")]
        public List<Badge> Badges { get; set; }

        [JsonProperty("video")]
        public Video Video { get; set; }

        [JsonProperty("comments")]
        public List<VideoComment> Comments { get; set; }
    }

    public struct User
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("login")]
        public string Login { get; set; }
    }

    public struct Video
    {
        [JsonProperty("lengthSeconds")]
        public long LengthInSeconds { get; set; }
        [JsonProperty("owner")]
        public User Owner { get; set; }
    }

    public struct VideoComment
    {
        public struct EmbeddedEmote
        {
            [JsonProperty("id")]
            public string ID { get; set; }
            [JsonProperty("emoteID")]
            public string EmoteID { get; set; }
            [JsonProperty("from")]
            public int? From { get; set; }
        }
        public struct User
        {
            [JsonProperty("id")]
            public string ID { get; set; }
            [JsonProperty("login")]
            public string Login { get; set; }
            [JsonProperty("displayName")]
            public string DisplayName { get; set; }
        }
        public struct VideoCommentMessage
        {
            [JsonProperty("fragments")]
            public List<VideoCommentMessageFragment> Fragments { get; set; }
            [JsonProperty("userBadges")]
            public List<Badge> UserBadges { get; set; }
            [JsonProperty("userColor")]
            public Color? UserColor { get; set; }
        }

        public struct VideoCommentMessageFragment
        {
            [JsonProperty("emote")]
            public EmbeddedEmote? Emote { get; set; }
            [JsonProperty("mention")]
            public User? Mention { get; set; }
            [JsonProperty("text")]
            public string Text { get; set; }
        }

        [JsonProperty("id")]
        public string ID { get; set; }
        [JsonProperty("commenter")]
        public User Commenter { get; set; }
        [JsonProperty("contentOffsetSeconds")]
        public int ContentOffsetSeconds { get; set; }
        [JsonProperty("createdAt")]
        public DateTime? CreatedAt { get; set; }
        [JsonProperty("message")]
        public VideoCommentMessage Message { get; set; }
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
