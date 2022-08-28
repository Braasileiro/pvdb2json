﻿namespace pvdb2json.Models
{
    internal class SongInfo
    {
        public string? name { get; set; }

        public string? arranger { get; set; }

        public string? illustrator { get; set; }

        public string? lyrics { get; set; }

        public string? music { get; set; }

        public SongInfo()
        {
            this.name = null;
            this.arranger = null;
            this.illustrator = null;
            this.lyrics = null;
            this.music = null;
        }
    }
}
