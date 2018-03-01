using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using AlienArc.Backup.Common;
using AlienArc.Backup.Common.Utilities;
using AlienArc.Backup.IO;

namespace AlienArc.Backup
{
	public class BackupManager : IBackupManager
    {
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
		    IBackupManagerSettings settings) 
		    : this(storageLocationFactory, backupIOFactory, backupIOFactory.GetBackupFile(catalogFilePath), settings) { }

	    public BackupManager(IStorageLocationFactory storageLocationFactory, 
		    IBackupIOFactory backupIOFactory, 
		    IBackupFile catalogFile, IBackupManagerSettings settings)
	    {
		    StorageLocationFactory = storageLocationFactory;
		    BackupIOFactory = backupIOFactory;
		    CatalogFile = catalogFile;
		    Settings = settings;

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
			    var newLocation = StorageLocationFactory.GetStorageLocation(location);

			    if (location.IsDefault)
			    {
				    DefaultLocation = newLocation;
			    }
				Locations.Add(newLocation);
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
		    var formatter = new BinaryFormatter();
		    using (var outStream = CatalogFile.Create())
		    {
			    formatter.Serialize(outStream, Catalog);
		    }
	    }

	    public void AddDirectoryToCatalog(IBackupDirectory directory)
	    {
		    Catalog.AddBackupDirectory(directory);
	    }

	    public void RunBackup()
	    {
		    var newBackupIndex = GetNewBackupIndex();

		    foreach (var location in Locations)
		    {
			    var previousBackupIndex = Catalog.GetMostRecentBackupIndex(location);
			    var filesToStore = GetUnstoredFiles(newBackupIndex, previousBackupIndex);
			    StoreBackupSets(location, filesToStore);
			    filesToStore.RootPath = location.RootPath;
			    Catalog.AddBackup(filesToStore);
		    }
		}

		public IEnumerable<IBackupIndex> GetBackups()
	    {
		    return Catalog.GetBackups();
	    }

	    public void AddStorageLocation(LocationInfo locationInfo)
	    {
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

	    public void RestoreBackupSet(IStorageLocation location, string backupSetPath, string destinationPath = null)
	    {
		    var backupSet = Catalog.GetBackupSet(location, backupSetPath);
		    var outPath = destinationPath ?? backupSetPath;
		    outPath = Path.Combine(outPath, backupSet.Root.Name);
		    var destination = BackupIOFactory.GetBackupDirectory(outPath);
		    if (!destination.Exists)
		    {
			    destination.Create();
		    }
		    RestoreBranch(backupSet.Root, backupSet.BasePath, outPath);
	    }

	    private void RestoreBranch(Branch root, string filePath, string destinationPath)
	    {
		    var outPath = destinationPath ?? filePath;
		    foreach (var subtree in root.Subtrees)
		    {
			    var newFilePath = Path.Combine(filePath, subtree.Name);
			    var newDestinationPath = Path.Combine(outPath, subtree.Name);
			    var destination = BackupIOFactory.GetBackupDirectory(newDestinationPath);
			    if (!destination.Exists)
			    {
					destination.Create();
			    }
			    RestoreBranch(subtree, newFilePath, destination.FullName);
		    }

		    foreach (var node in root.Nodes)
		    {
			    var fullDestinationPath = Path.Combine(outPath, node.Name);
			    RestoreFileToDisk(node.Hash, fullDestinationPath);
		    }
	    }

	    public bool RestoreFile(IStorageLocation location, string filePath, string destinationPath = null)
	    {
		    var outPath = destinationPath ?? filePath;
		    var hash = Catalog.GetFileHashFromPath(location, filePath);
		    RestoreFileToDisk(hash, outPath);
			return true;
	    }

		#region Private methods
		private void RestoreFileToDisk(byte[] fileHash, string destinationPath)
	    {
		    var newFile = BackupIOFactory.GetBackupFile(destinationPath);
		    using (var inStream = DefaultLocation.GetFile(fileHash))
		    using (var outStream = newFile.Create())
		    {
			    if (inStream == null) return;
			    inStream.CopyTo(outStream);
		    }
	    }

	    private void StoreBackupSets(IStorageLocation location, IBackupIndex filesToStore)
		{
			foreach (var backupSet in filesToStore.BackupSets)
			{
				var basePath = backupSet.BasePath;
				StoreBranch(location, basePath, backupSet.Root);
			}
		}

		private void StoreBranch(IStorageLocation location, string basePath, Branch root)
		{
			foreach (var subtree in root.Subtrees)
			{
				var newPath = Path.Combine(basePath, subtree.Name);
				StoreBranch(location, newPath, subtree);
			}

			foreach (var node in root.Nodes)
			{
				var filePath = Path.Combine(basePath, node.Name);
				node.BackedUp = location.StoreFile(filePath, node.Hash);				
			}
		}

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
				parentBranch.Nodes.Add(new Node(file.Name, hash));
			}

			return parentBranch;
		} 
		#endregion
	}
}
