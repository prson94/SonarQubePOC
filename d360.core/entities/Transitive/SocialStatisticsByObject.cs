namespace d360.core.entities
{
    /// <summary>
    /// Loaded from the stored procedure: tile.GetSocialStatisticsByObject
    /// </summary>
    public class SocialStatisticsByObject
    {
        public int FollowerCount { get; set; }

        public int CommentCount { get; set; }

        public int CommentCountLast48Hours { get; set; }
    }
}
