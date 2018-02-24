using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using AlienArc.Backup.IO;

namespace AlienArc.Backup.Common.Utilities
{
    public static class Hasher
    {
	    static SHA1CryptoServiceProvider HashProvider { get; set; } = new SHA1CryptoServiceProvider();

	    public static byte[] GetFileHash(IBackupFile file)
	    {
		    using (var stream = file.OpenRead())
		    {
			    return GetFileHash(stream);
		    }
	    }

	    public static byte[] GetFileHash(Stream fileStream)
	    {
		    return HashProvider.ComputeHash(fileStream);
	    }

	    public static string GetDirectoryNameFromHash(byte[] hash)
	    {
		    return hash[0].ToString("X2");
	    }

	    public static string GetFileNameFromHash(byte[] hash)
	    {
		    return BitConverter.ToString(hash.Skip(1).ToArray()).Replace("-", string.Empty);
	    }
	}
}
