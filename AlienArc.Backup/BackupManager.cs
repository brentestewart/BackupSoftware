using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using AlienArc.Backup.Common;
using AlienArc.Backup.Common.Utilities;
using AlienArc.Backup.IO;
using TcpCommunications.Core;

namespace AlienArc.Backup
{
	public class BackupManager : IBackupManager
    {
		protected ILogger Logger { get; set; }
		protected ICatalog Catalog { get; set; }
	    protected IStorageLocationFactory StorageLocationFactory { get; }
	    protected IBackupIOFactory BackupIOFactory { get; }
	    protected IBackupFile CatalogFile { get; set; }
		protected List<IStorageLocation> Locations { get; set; } = new List<IStorageLocation>();
		protected IStorageLocation DefaultLocation { get; set; }
		protected IBackupManagerSettings Settings { get; set; }

	    public BackupManager(IStorageLocationFactory storageLocationFactory, 
		    IBackupIOFactory backupIOFactory, 
		    string catalogFilePath,
		    IBackupManagerSettings settings, ILogger logger) 
		    : this(storageLocationFactory, backupIOFactory, backupIOFactory.GetBackupFile(catalogFilePath), settings, logger) { }

	    public BackupManager(IStorageLocationFactory storageLocationFactory, 
		    IBackupIOFactory backupIOFactory, 
		    IBackupFile catalogFile, IBackupManagerSettings settings,
		    ILogger logger)
	    {
		    StorageLocationFactory = storageLocationFactory;
		    BackupIOFactory = backupIOFactory;
		    CatalogFile = catalogFile;
		    Settings = settings;
		    Logger = logger;

		    LoadSettings(settings);

		    if (catalogFile.Exists)
		    {
			    OpenCatalog(catalogFile);
		    }
		    else
		    {
			    Catalog = new Catalog();
		    }
		}

	    private void LoadSettings(IBackupManagerSettings settings)
	    {
		    foreach (var location in settings.Locations)
		    {
				AddStorageLocation(location);
		    }
	    }

		public void OpenCatalog(IBackupFile catalogFile)
	    {
			var formatter = new BinaryFormatter();
			using (var inStream = catalogFile.OpenRead())
		    {
			    Catalog = (Catalog) formatter.Deserialize(inStream);
		    }
	    }

	    public void SaveCatalog()
	    {
			Logger.LogDebug($"Saving Catalog - {CatalogFile.FullName}");
		    var formatter = new BinaryFormatter();
		    using (var outStream = CatalogFile.Create())
		    {
			    formatter.Serialize(outStream, Catalog);
		    }
	    }

	    public void AddDirectoryToCatalog(IBackupDirectory directory)
	    {
			Logger.LogDebug($"Adding Backup Directory {directory.FullName}");
		    Catalog.AddBackupDirectory(directory);
	    }

	    #region Backup
		public async Task<bool> RunBackup()
		{
			var results = true;
		    var newBackupIndex = GetNewBackupIndex();

		    foreach (var location in Locations)
		    {
			    Logger.LogDebug($"Running backup to location - {location.RootPath}");
			    if (location.LocationType == StorageLocationType.Remote)
			    {
				    await location.Connect();
			    }

			    var previousBackupIndex = Catalog.GetMostRecentBackupIndex(location);
			    var filesToStore = GetUnstoredFiles(newBackupIndex, previousBackupIndex);
			    var success = await StoreBackupSets(location, filesToStore);
			    if (success)
			    {
				    filesToStore.RootPath = location.RootPath;
				    Catalog.AddBackup(filesToStore);
			    }
			    else
			    {
				    results = false;
			    }
		    }

			return results;
		}

	    private async Task<bool> StoreBackupSets(IStorageLocation location, IBackupIndex filesToStore)
	    {
		    var results = true;
		    foreach (var backupSet in filesToStore.BackupSets)
		    {
			    var basePath = backupSet.BasePath;
			    var success = await StoreBranch(location, basePath, backupSet.Root);
			    if (!success) results = false;
		    }

		    return results;
	    }

	    private async Task<bool> StoreBranch(IStorageLocation location, string basePath, Branch root)
	    {
		    var results = true;
		    foreach (var subtree in root.Subtrees)
		    {
			    var newPath = Path.Combine(basePath, subtree.Name);
			    var success = await StoreBranch(location, newPath, subtree);
			    if (!success) results = false;
		    }

		    foreach (var node in root.Nodes)
		    {
			    var filePath = Path.Combine(basePath, node.Name);
			    Logger.LogDebug($"Storing file - {filePath}");
			    node.BackedUp = await location.StoreFile(filePath, node.Hash);
			    if (!node.BackedUp) results = false;
		    }

		    return results;
	    }
	    #endregion

		public IEnumerable<IBackupIndex> GetBackups()
	    {
		    return Catalog.GetBackups();
	    }

	    public void AddStorageLocation(LocationInfo locationInfo)
	    {
		    Logger.LogDebug($"Adding Location {locationInfo.Path}");
			var storageLocation = StorageLocationFactory.GetStorageLocation(locationInfo);
			Locations.Add(storageLocation);
		    if (locationInfo.IsDefault) DefaultLocation = storageLocation;
	    }

	    public void RemoveStorageLocation(LocationInfo locationInfo)
	    {
		    var storageLocation = StorageLocationFactory.GetStorageLocation(locationInfo);
		    Locations.Remove(storageLocation);
	    }

	    public List<IStorageLocation> GetLocations()
	    {
		    return Locations;
	    }

	    #region Restore
		public async Task<bool> RestoreBackupSet(IStorageLocation location, string backupSetPath, string destinationPath = null)
	    {
		    Logger.LogDebug($"Restoring BackupSet {backupSetPath}");
		    var backupSet = Catalog.GetBackupSet(location, backupSetPath);
		    var outPath = destinationPath ?? backupSetPath;
		    outPath = Path.Combine(outPath, backupSet.Root.Name);
		    var destination = BackupIOFactory.GetBackupDirectory(outPath);
		    if (!destination.Exists)
		    {
			    destination.Create();
		    }
		    return await RestoreBranch(backupSet.Root, backupSet.BasePath, outPath);
	    }

	    private async Task<bool> RestoreBranch(Branch root, string filePath, string destinationPath)
	    {
		    var success = true;
		    var outPath = destinationPath ?? filePath;
		    Logger.LogDebug($"Restoring Branch {outPath}");
		    foreach (var subtree in root.Subtrees)
		    {
			    var newFilePath = Path.Combine(filePath, subtree.Name);
			    var newDestinationPath = Path.Combine(outPath, subtree.Name);
			    var destination = BackupIOFactory.GetBackupDirectory(newDestinationPath);
			    if (!destination.Exists)
			    {
					destination.Create();
			    }
			    var results = await RestoreBranch(subtree, newFilePath, destination.FullName);
			    if (!results) success = false;
		    }

		    foreach (var node in root.Nodes)
		    {
			    var fullDestinationPath = Path.Combine(outPath, node.Name);
			    Logger.LogDebug($"Restoring File {fullDestinationPath}");
			    var results = await RestoreFileToDisk(node, fullDestinationPath);
			    if (!results) success = false;
		    }

		    return success;
	    }

	    public async Task<bool> RestoreFile(IStorageLocation location, string filePath, string destinationPath = null)
	    {
		    var outPath = destinationPath ?? filePath;
		    var hash = Catalog.GetFileNodeFromPath(location, filePath);
		    return await RestoreFileToDisk(hash, outPath);
	    }

		private async Task<bool> RestoreFileToDisk(Node node, string destinationPath)
	    {
		    var success = false;
			try
			{
				var newFile = BackupIOFactory.GetBackupFile(destinationPath);
				var tempFilePath = await DefaultLocation.GetFile(node.Hash);

				var restoredFile = new FileInfo(newFile.FullName);
				using (var inStream = File.OpenRead(tempFilePath))
				using (var outStream = newFile.Create())
				{
					if (inStream == null) return false;
					inStream.CopyTo(outStream);
				}

				restoredFile.CreationTime = node.CreationTime;
				restoredFile.LastWriteTime = node.ModifiedTime;
				restoredFile.Attributes = node.FileAttributes;
				restoredFile.IsReadOnly = node.ReadOnly;

				using (var verifyStream = newFile.OpenRead())
				{
					var verifyHash = Hasher.GetFileHash(verifyStream);
					success = verifyHash.SequenceEqual(node.Hash);
				}

			}
			catch (Exception ex)
			{
				return false;
			}
		    return success;
	    }
	    #endregion

		#region Private methods
		private IBackupIndex GetUnstoredFiles(IBackupIndex existingIndex, IBackupIndex previousIndex)
		{
			if (previousIndex == null) return existingIndex;

			var diffIndex = new BackupIndex();
			var allPreviousFileHashes = previousIndex.GetAllNodeHashes();

			foreach (var existingBackupSet in existingIndex.BackupSets)
			{
				var diffBackupSet = diffIndex.AddBackupSet(existingBackupSet.BasePath, existingBackupSet.Root.Name);
				DiffBackupSets(existingBackupSet.Root, diffBackupSet.Root, allPreviousFileHashes);
			}

			return diffIndex;
		}

		private void DiffBackupSets(Branch existingRoot, Branch diffRoot, HashSet<byte[]> allPreviousNodeHashes)
		{
			foreach (var currentSubtree in existingRoot.Subtrees)
			{
				var newDiffRoot = diffRoot.AddSubtree(currentSubtree.Name);
				DiffBackupSets(currentSubtree, newDiffRoot, allPreviousNodeHashes);
				if (newDiffRoot.Nodes.Count == 0 && newDiffRoot.Subtrees.Count == 0)
				{
					diffRoot.DeleteSubtree(newDiffRoot);
				}
			}

			foreach (var existingNode in existingRoot.Nodes)
			{
				if (!allPreviousNodeHashes.Contains(existingNode.Hash))
				{
					diffRoot.Nodes.Add(existingNode);
				}
			}
		}

		private IBackupIndex GetNewBackupIndex()
		{
			var newBackupIndex = new BackupIndex();
			var directoriesToBackup = Catalog.GetDirectories();
			foreach (var directory in directoriesToBackup)
			{
				var backupDirectory = BackupIOFactory.GetBackupDirectory(directory);
				var newBranch = new Branch(backupDirectory.Name);
				var root = GetTree(backupDirectory, newBranch);
				var newBackupSet = new BackupSet(backupDirectory.FullName, backupDirectory.Name) { Root = root };
				newBackupIndex.BackupSets.Add(newBackupSet);
			}

			return newBackupIndex;
		}

		private Branch GetTree(IBackupDirectory directory, Branch parentBranch)
		{
			foreach (var currentDirectory in directory.GetDirectories())
			{
				var newBranch = new Branch(currentDirectory.Name);
				parentBranch.Subtrees.Add(GetTree(currentDirectory, newBranch));
			}

			foreach (var file in directory.GetFiles())
			{
				var hash = Hasher.GetFileHash(file);
				parentBranch.Nodes.Add(new Node(file, hash));
			}

			return parentBranch;
		} 
		#endregion
	}
}
