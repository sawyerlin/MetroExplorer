namespace MetroExplorer.DataSource.DataModels
{
    using System;
    using SQLite;

    [Table("MapLocations")]
    public class MapLocationModel : IEquatable<MapLocationModel>
    {
        #region Properties

        [PrimaryKey]
        public Guid Id { get; set; }

        [Indexed]
        public Guid MapId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Latitude { get; set; }

        public string Longitude { get; set; }

        #endregion

        public MapLocationModel() { }

        public MapLocationModel(string name, string description, string latitude, string longtitude)
        {
            Name = name;
            Description = description;
            Latitude = latitude;
            Longitude = longtitude;
        }

        public bool Equals(MapLocationModel other)
        {
            return Id.Equals(other.Id);
        }
    }
}