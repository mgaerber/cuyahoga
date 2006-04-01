using System;
using System.Collections;
using System.Web;

using NHibernate;

using Castle.Services.Transaction;

using Cuyahoga.Core;
using Cuyahoga.Core.Domain;
using Cuyahoga.Core.Service;
using Cuyahoga.Core.Service.Files;
using Cuyahoga.Modules.Downloads.Domain;

namespace Cuyahoga.Modules.Downloads
{
	/// <summary>
	/// The Downloads Module provides file downloads for users.
	/// </summary>
	[Transactional]
	public class DownloadsModule : ModuleBase, INHibernateModule
	{
		private string _physicalDir;
		private bool _showPublisher;
		private bool _showDateModified;
		private bool _showNumberOfDownloads;
		private int _currentFileId;
		private DownloadsModuleActions _currentAction;
		private IFileService _fileService;

		#region properties

		/// <summary>
		/// The physical directory where the files are located.
		/// </summary>
		/// <remarks>
		/// If there is no setting for the physical directory, the default of
		/// Application_Root/files is used.
		///	</remarks>
		public string FileDir
		{
			get 
			{ 
				if (this._physicalDir == null)
				{
					this._physicalDir = HttpContext.Current.Server.MapPath("~/files");
					CheckPhysicalDirectory();
				}
				return this._physicalDir;
			}
			set 
			{ 
				this._physicalDir = value;
				CheckPhysicalDirectory();
			}
		}

		/// <summary>
		/// The Id of the file that is to be downloaded.
		/// </summary>
		public int CurrentFileId
		{
			get { return this._currentFileId; }
		}

		/// <summary>
		/// A specific action that has to be done by the module.
		/// </summary>
		public DownloadsModuleActions CurrentAction
		{
			get { return this._currentAction; }
		}

		/// <summary>
		/// Show the name of the user who published the file?
		/// </summary>
		public bool ShowPublisher
		{
			get { return this._showPublisher; }
		}

		/// <summary>
		/// Show the date and time when the file was last updated?
		/// </summary>
		public bool ShowDateModified
		{
			get { return this._showDateModified; }
		}

		/// <summary>
		/// Show the number of downloads?
		/// </summary>
		public bool ShowNumberOfDownloads
		{
			get { return this._showNumberOfDownloads; }
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="fileService">FileService dependency</param>
		public DownloadsModule(IFileService fileService)
		{
			this._fileService = fileService;
		}

		public override void ReadSectionSettings()
		{
			base.ReadSectionSettings ();
			// Set dynamic module settings
			string physicalDir = Convert.ToString(base.Section.Settings["PHYSICAL_DIR"]);
			if (physicalDir != String.Empty)
			{
				this._physicalDir = physicalDir;
				CheckPhysicalDirectory();
			}
			this._showPublisher = Convert.ToBoolean(base.Section.Settings["SHOW_PUBLISHER"]);
			this._showDateModified = Convert.ToBoolean(base.Section.Settings["SHOW_DATE"]);
			this._showNumberOfDownloads = Convert.ToBoolean(base.Section.Settings["SHOW_NUMBER_OF_DOWNLOADS"]);
		}

		/// <summary>
		/// Get the a file as stream.
		/// </summary>
		/// <returns></returns>
		public System.IO.Stream GetFileAsStream(File file)
		{
			if (file.IsDownloadAllowed(HttpContext.Current.User.Identity))
			{
				string physicalFilePath = System.IO.Path.Combine(this.FileDir, file.FilePath);
				if (System.IO.File.Exists(physicalFilePath))
				{
					return this._fileService.ReadFile(physicalFilePath);
				}
				else
				{
					throw new System.IO.FileNotFoundException("File not found", physicalFilePath);
				}
			}
			else
			{
				throw new AccessForbiddenException("The current user has no permission to download the file.");
			}
		}

		/// <summary>
		/// Retrieve the meta-information of all files that belong to this module.
		/// </summary>
		/// <returns></returns>
		public IList GetAllFiles()
		{
			string hql = "from File f where f.Section.Id = :sectionId order by f.DatePublished desc";
			IQuery q = base.NHSession.CreateQuery(hql);
			q.SetInt32("sectionId", base.Section.Id);

			try
			{
				return q.List();
			}
			catch (Exception ex)
			{
				throw new Exception("Unable to get Files for section: " + base.Section.Title, ex);
			}
		}

		/// <summary>
		/// Get the meta-information of a single file.
		/// </summary>
		/// <param name="fileId"></param>
		/// <returns></returns>
		public File GetFileById(int fileId)
		{
			try
			{
				return (File)base.NHSession.Load(typeof(File), fileId);
			}
			catch (Exception ex)
			{
				throw new Exception("Unable to get File with identifier: " + fileId.ToString(), ex);
			}
		}

		/// <summary>
		/// Insert or update the meta-information of a file.
		/// </summary>
		[Transaction(TransactionMode.Requires)]
		public virtual void SaveFile(File file)
		{
			base.NHSession.SaveOrUpdate(file);
		}

		/// <summary>
		/// Insert or update the meta-information of a file and update the file contents at the 
		/// same time in one transaction.
		/// </summary>
		/// <param name="file"></param>
		/// <param name="fileContents"></param>
		[Transaction(TransactionMode.RequiresNew)]
		public virtual void SaveFile(File file, System.IO.Stream fileContents)
		{
			// Save physical file.
			string physicalFilePath = System.IO.Path.Combine(this.FileDir, file.FilePath);
			this._fileService.WriteFile(physicalFilePath, fileContents);
			// Save meta-information.
			SaveFile(file);
		}

		/// <summary>
		/// Delete the meta-information of a file
		/// </summary>
		[Transaction(TransactionMode.RequiresNew)]
		public virtual void DeleteFile(File file)
		{
			// Delete physical file.
			string physicalFilePath = System.IO.Path.Combine(this.FileDir, file.FilePath);
			this._fileService.DeleteFile(physicalFilePath);
			// Delete meta information.
			base.NHSession.Delete(file);
		}

		/// <summary>
		/// Parse the pathinfo.
		/// </summary>
		protected override void ParsePathInfo()
		{
			base.ParsePathInfo();
			if (base.ModuleParams != null)
			{
				if (base.ModuleParams.Length == 2)
				{
					// First argument is the module action and the second is the Id of the file.
					try
					{
						this._currentAction = (DownloadsModuleActions)Enum.Parse(typeof(DownloadsModuleActions)
							, base.ModuleParams[0], true);
						this._currentFileId = Int32.Parse(base.ModuleParams[1]);
					}
					catch (ArgumentException ex)
					{
						throw new Exception("Error when parsing module action: " + base.ModuleParams[0], ex);
					}
					catch (Exception ex)
					{
						throw new Exception("Error when parsing module parameters: " + base.ModulePathInfo, ex);
					}
				}
			}
		}

		private void CheckPhysicalDirectory()
		{
			// Check existence
			if (! System.IO.Directory.Exists(this._physicalDir))
			{
				throw new Exception(String.Format("The Downloads module didn't find the physical directory {0} on the server.", this._physicalDir));
			}
		}
		}

	public enum DownloadsModuleActions
	{
		Download
	}
}
