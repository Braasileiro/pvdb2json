namespace pvdb2json.Models
{
    internal class Song
    {
        public int id { get; set; }
        
        public int? album { get; set; }
        
        public int? type { get; set; }
        
        public int? bpm { get; set; }
        
        public int? date { get; set; }
        
        public string? reading { get; set; }

        public SongInfo jp { get; set; }

        public SongInfo en { get; set; }

        public List<SongPerformer>? performers { get; set; }

        public Song(int id, int type)
        {
            this.id = id;
            this.album = null;
            this.type = type;
            this.bpm = null;
            this.date = null;
            this.reading = null;
            this.jp = new SongInfo();
            this.en = new SongInfo();
            this.performers = null;
        }
    }
}
