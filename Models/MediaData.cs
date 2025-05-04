namespace Models
{
    public class MediaData
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Genre { get; set; }
        public List<Season> Seasons { get; set; } = new List<Season>();
    }
}



