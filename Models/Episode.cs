namespace Models
{
    public class Episode
    {
        public int EpisodeId { get; set; }
        public int SeasonId { get; set; }
        public int EpisodeNumber { get; set; }
        public string Title { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string Duration { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string Link { get; set; }
    }
}
