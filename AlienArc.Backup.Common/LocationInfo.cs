namespace AlienArc.Backup
{
	public class LocationInfo
	{
		public string Name { get; set; }
		public string Path { get; set; }
		public int Port { get; set; }
		public string TempStoragePath { get; set; }
		public StorageLocationType LocationType { get; set; }
		public bool IsDefault { get; set; }
	}
}