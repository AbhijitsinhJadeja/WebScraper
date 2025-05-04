using Models;
using Npgsql;
using Serilog;

public class MediaRepository
{
    private readonly string _connectionString;
    private readonly ILogger _logger;
    public MediaRepository(string connectionString, ILogger logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }
    public void SaveMediaData(MediaData mediaData)
    {
        using (var connection = new NpgsqlConnection(_connectionString))
        {
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    string checkQuery = @"SELECT MediaId FROM Media WHERE Title = @Title";
                    var checkCmd = new NpgsqlCommand(checkQuery, connection, transaction);
                    checkCmd.Parameters.AddWithValue("Title", mediaData.Title);
                    var existingMediaId = checkCmd.ExecuteScalar();

                    if (existingMediaId != null)
                    {
                        _logger.Warning("Media with title '{Title}' already exists. Skipping.", mediaData.Title);
                        transaction.Rollback();  
                        return; 
                    }
                    _logger.Information("Saving media: {@Media}", mediaData);

                    string insertMediaQuery = @"INSERT INTO Media (Title, Description, Genre) 
                                            VALUES (@Title, @Description, @Genre) RETURNING MediaId";
                    var mediaCommand = new NpgsqlCommand(insertMediaQuery, connection, transaction);
                    mediaCommand.Parameters.AddWithValue("Title", mediaData.Title);
                    mediaCommand.Parameters.AddWithValue("Description", mediaData.Description);
                    mediaCommand.Parameters.AddWithValue("Genre", mediaData.Genre);

                    int mediaId = (int)mediaCommand.ExecuteScalar();

                    if (mediaData.Seasons == null || mediaData.Seasons.Count == 0)
                    {
                        Console.WriteLine("No seasons found, saving directly into Media.");
                        return; 
                    }

                    foreach (var season in mediaData.Seasons)
                    {
                        string insertSeasonQuery = @"INSERT INTO Seasons (MediaId, SeasonTitle, ReleaseDate)
                                                VALUES (@MediaId, @SeasonTitle, @ReleaseDate) RETURNING SeasonId";
                        var seasonCommand = new NpgsqlCommand(insertSeasonQuery, connection, transaction);
                        seasonCommand.Parameters.AddWithValue("MediaId", mediaId);
                        seasonCommand.Parameters.AddWithValue("SeasonTitle", season.SeasonTitle);
                        seasonCommand.Parameters.AddWithValue("ReleaseDate", DBNull.Value);

                        int seasonId = (int)seasonCommand.ExecuteScalar();

                        foreach (var episode in season.Episodes)
                        {
                            string insertEpisodeQuery = @"INSERT INTO Episodes (SeasonId, EpisodeTitle, Duration, EpisodeNumber, ReleaseDate, Description)
                                                    VALUES (@SeasonId, @EpisodeTitle, @Duration, @EpisodeNumber, @ReleaseDate, @Description)";
                            var episodeCommand = new NpgsqlCommand(insertEpisodeQuery, connection, transaction);
                            episodeCommand.Parameters.AddWithValue("SeasonId", seasonId);
                            episodeCommand.Parameters.AddWithValue("EpisodeTitle", episode.Title);
                            episodeCommand.Parameters.AddWithValue("Duration", episode.Duration);
                            episodeCommand.Parameters.AddWithValue("EpisodeNumber", episode.EpisodeNumber);
                            episodeCommand.Parameters.AddWithValue("ReleaseDate", DBNull.Value);
                            episodeCommand.Parameters.AddWithValue("Description", episode.Description);

                            episodeCommand.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    _logger.Error(ex, "Error occurred while saving media data: {@Media}", mediaData);
                    transaction.Rollback();
                }
            }
        }
    }
}