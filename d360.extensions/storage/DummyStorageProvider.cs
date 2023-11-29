using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace d360.extensions.storage
{
	public class DummyStorageProvider : IStorageProvider
	{
		public Task CreateFile(string folderName, string fileName, Stream file, string contentType = null, bool cache = true)
		{
			return Task.CompletedTask;
		}

		public Task CreateFile(string folderName, string fileName, string content, string contentType = null, bool cache = true)
		{
			return Task.CompletedTask;
		}

		public Task CreateFolder(string name)
		{
			return Task.CompletedTask;
		}

		public Task DeleteFile(string folderName, string fileName)
		{
			return Task.CompletedTask;
		}

		public Task<T> DeserializeJsonObjectFromBlobAsync<T>(string folderName, string fileName)
		{
			return Task.FromResult(default(T));
		}

		public Uri GetBaseUri(string folderName)
		{
			return new Uri("");
		}

		public string GetFileContentsAsString(string folderName, string fileName)
		{
			return string.Empty;
		}

		public Task<string> GetFileContentsAsString(string folderName, string fileName, Encoding encoding)
		{
			return Task.FromResult(string.Empty);
		}

		public Task<DateTime> GetFileLastModifiedDate(string folderName, string fileName)
		{
			return Task.FromResult(DateTime.MinValue);
		}

		public Task GetFileStream(string folderName, string fileName, Stream sr)
		{
			return Task.CompletedTask;
		}

		public List<string> ListFilenamesByPrefix(string folderName, string prefix)
		{
			return new List<string>();
		}

		public List<StorageFileInfo> ListFiles(string folderName)
		{
			return null;
		}

		public Task SerializeJsonObjectToBlobAsync(string folderName, string fileName, object obj)
		{
			return Task.CompletedTask;
		}
	}
}
