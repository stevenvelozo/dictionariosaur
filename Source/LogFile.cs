/**
* This source file is a part of the Dictionariosaur application.
* For full copyright and license information, please view the LICENSE file
* which should be distributed with this source code.
*
* @license MIT License
* @copyright Copyright (c) 2013, Steven Velozo
*/
using System;
using System.IO;

namespace MutiUtility
{
	#region Event Arguments and Delegates
	/// <summary>
	/// This is the Write Log event arguments for logging events.
	/// </summary>
	public class WriteLogEventArgs : EventArgs 
	{   
		private string _LogText = "";
		private int _LogLevel = 0;

		/// <summary>
		/// This generates the log text template to be sent when the event fires.
		/// </summary>
		/// <param name="pLogText">The text to log.</param>
		/// <param name="pLogLevel">The way the text is expected to be used; 0 for display, 1 for display and file, 2 for file only.</param>
		public WriteLogEventArgs(string pLogText, int pLogLevel)
		{
			//Nothing fancy to be done, this is a data encapsulation class
			this._LogText = pLogText;
			this._LogLevel = pLogLevel;
		}

		// Properties.
		public string LogText
		{ 
			get { return this._LogText; }
		}

		public int LogLevel
		{
			get { return this._LogLevel; }
		}    
	}

	/// <summary>
	/// This is a delegate that offers interfaces to log files and rolling log displays.
	/// </summary>	
	public delegate void WriteLogEventHandler (object sender, WriteLogEventArgs e);
	#endregion

	#region Master Log File Class
	/// <summary>
	/// Base Main Rolling Log File class
	/// </summary>
	public class LogFile
	{
		private string _FileNamePreface;
		private string _FilePath;
		private string _FileName;

		//The date of the log file -- to be prepended to the file name.
		private System.DateTime _LogDate;

		private FileInfo _LogFileInfo;
		private StreamWriter _LogFileStreamWriter;
		private bool _LogFileOpen;
		
		private bool _WriteToConsole;

		#region Initializers
		public LogFile ()
		{
			this.InitializeFile(this.GetHashCode().ToString(), "");
		}
		
		public LogFile (string pFileNamePreface)
		{
			this.InitializeFile(pFileNamePreface, "");
		}

		/// <summary>
		/// Construct the Log File.
		/// </summary>
		/// <param name="pFileNamePreface">The preface text appended to the file name.</param>
		/// <param name="pFilePath">The location of the log file.</param>
		public LogFile(string pFileNamePreface, string pFilePath)
		{
			this.InitializeFile(pFileNamePreface, pFilePath);
		}

		private void InitializeFile(string FileNamePreface, string FilePath)
		{
			//Initialize the class information
			if (FilePath == "")
				this._FilePath = System.AppDomain.CurrentDomain.BaseDirectory;
			else
				this._FilePath = FilePath;

			this._FileNamePreface = FileNamePreface;
			
			this._LogFileOpen = false;
			
			this._WriteToConsole = false;
		}
		
		~LogFile()
		{
			//This is causing trouble.
			if (this._LogFileOpen)
				this.CloseLogFile();
		}
		#endregion
		
		#region Data Access
		public string LogFilePreface
		{
			get { return this._FileNamePreface; }
			set {this._FileNamePreface = value; }
		}
		
		public string LogFilePath
		{
			get { return this._FilePath; }
			set {this._FilePath = value; }
		}
		
		public bool EchoToConsole
		{
			get { return this._WriteToConsole; }
			set {this._WriteToConsole = value; }
		}
		#endregion
		
		/// <summary>
		/// Actually write the text to the log file.
		/// </summary>
		/// <param name="pLogText">The text to be written.</param>
		public void WriteLogFile(string pLogText)
		{
			try
			{
				if ((!this._LogFileOpen) || (System.DateTime.Now.Date != this._LogDate.Date))
					this.OpenLogFile();

				//Now write out the time stamp and then the log text to a line in the log file
				//System.Diagnostics.Debug.WriteLine(string.Concat("WROTE [", CurrentTime.Hour, ":", CurrentTime.Minute, ":", CurrentTime.Second, "]", LogText));
				this._LogFileStreamWriter.WriteLine(string.Concat("[", System.DateTime.Now.ToLongTimeString(), "]", pLogText));
				
				if (this._WriteToConsole)
					Console.WriteLine(string.Concat("[", System.DateTime.Now.ToLongTimeString(), "]", pLogText));

				//Since we send very little data, and the Stream Writer does not automatically
				//flush itself, we have to manually flush the stream after every write in order
				//to insure the lines will be written properly.
				this._LogFileStreamWriter.Flush();
			}
			catch
			{
			}
		}

		#region File Management
		/// <summary>
		/// Open the log file in the path with the specified name.
		/// </summary>
		private void OpenLogFile()
		{
			try
			{
				if (_LogFileOpen)
					this.CloseLogFile();

				//Set the log file date
				this._LogDate = System.DateTime.Now;
				//Mash together the file name from the date and prefix
				this._FileName = string.Concat(this._LogDate.Year, "-", this._LogDate.Month, "-", this._LogDate.Day, "-", this._FileNamePreface, ".log");
				//Open up a file information class
				this._LogFileInfo = new FileInfo(string.Concat(this._FilePath, this._FileName));
				//Now open up a stream writer to the opened file info class
				this._LogFileStreamWriter = this._LogFileInfo.AppendText();

				this._LogFileOpen = true;
			}
			catch
			{
			}
		}

		private void CloseLogFile()
		{
			try
			{
				this._LogFileStreamWriter.Close();
				this._LogFileOpen = false;
			}
			catch
			{
			}
        }
		#endregion
	}
	#endregion
	
	/// <summary>
	/// This class provides pass-through and shim event handlers for log files
	/// </summary>
	public class PassThroughLoggedClass
	{
		public event WriteLogEventHandler WriteLog;

		/// <summary>
		/// Write to the log file/display.
		/// </summary>
		/// <param name="pLogText">The text to be written.</param>
		/// <param name="pLogLevel">The numeric level of the log text.  In general, 0 is screen, 1 is both, 2 is file only.</param>
		protected virtual void WriteToLog(string pLogText, int pLogLevel)
		{
			WriteLogEventArgs e = new WriteLogEventArgs(pLogText, pLogLevel);

			this.OnWriteLog (e);
		}

		protected virtual void WriteToLog(string pLogText)
		{
			//Default log level is 1 (display and log to file)
			this.WriteToLog(pLogText, 1);
		}		


		protected virtual void OnWriteLog(WriteLogEventArgs pEventArgs)
		{
			if (this.WriteLog != null)
				//Invoke the event delegate
				WriteLog (this, pEventArgs);
		}

		protected virtual void ChainedWriteLog(object pSender, WriteLogEventArgs pEventArgs)
		{
			//A shim function to chain log events from objects here to the main application's events.
			this.OnWriteLog(pEventArgs);
		}
	}
	
	/// <summary>
	/// This class provides pass-through and logging facilities, for classes that need to
	/// both log data and pass the data through to the next class via events and shims
	/// </summary>
	public class MasterLoggedClass : PassThroughLoggedClass
	{
		private LogFile _LogFile;
		
		public MasterLoggedClass()
		{
			this._LogFile = new LogFile ();
		}
		
		#region Data Access
		/// <summary>
		/// The prefix to go before log files.
		/// Defaults to the classes unique hash code.
		/// </summary>
		protected string LogFilePrefix
		{
			set {this._LogFile.LogFilePreface = value; }
			get { return this._LogFile.LogFilePreface; }
		}
		
		/// <summary>
		/// The path that the log file resides in.
		/// Defaults to the application path.
		/// </summary>
		protected string LogFilePath
		{
			set {this._LogFile.LogFilePath = value; }
			get { return this._LogFile.LogFilePath; }
		}
		
		/// <summary>
		/// If this is true we will echo any logged events < 2 to the console
		/// </summary>
		protected bool LogFileToConsole
		{
			set {this._LogFile.EchoToConsole = value; }
			get { return this._LogFile.EchoToConsole; }
		}
		#endregion

		/// <summary>
		/// Write to the log file/display.
		/// </summary>
		/// <param name="pLogText">The text to be written.</param>
		/// <param name="pLogLevel">The numeric level of the log text.  In general, 0 is screen, 1 is both, 2 is file only.</param>
		protected override void WriteToLog(string pLogText, int pLogLevel)
		{
			WriteLogEventArgs tmpEventArgs = new WriteLogEventArgs(pLogText, pLogLevel);
			this.OnWriteLog (tmpEventArgs);
			//Write it to the textual log file if it is > 0
			if (tmpEventArgs.LogLevel > 0)
				this._LogFile.WriteLogFile (tmpEventArgs.LogText);
		}

		protected override void ChainedWriteLog(object pSender, WriteLogEventArgs pEventArgs)
		{
			//A shim function to chain log events from objects here to the main application's events.
			this.OnWriteLog(pEventArgs);
			//Write it to the textual log file if it is > 0
			if (pEventArgs.LogLevel > 0)
				this._LogFile.WriteLogFile (pEventArgs.LogText);
		}
	}
}
