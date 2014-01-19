namespace MetroExplorer.DataSource.DataModels
{
    using System;
    using SQLite;

    [Table("MapLocationFolders")]
    public class MapLocationFolderModel
    {
        [PrimaryKey]
        public Guid Id { get; set; }

        [Indexed]
        public Guid MapLocationId { get; set; }

        public string Token { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }
    }
}
