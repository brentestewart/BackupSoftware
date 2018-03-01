namespace AlienArc.Backup.Common
{
	public interface IStorageLocationFactory
	{
		IStorageLocation GetStorageLocation(LocationInfo locationInfo);
	}
}