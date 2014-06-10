/**
* This source file is a part of the Dictionariosaur application.
* For full copyright and license information, please view the LICENSE file
* which should be distributed with this source code.
*
* @license MIT License
* @copyright Copyright (c) 2013, Steven Velozo
*/
using System;
using MutiUtility;

namespace MutiNetworking
{
	#region Telnet Server Event Arguments and Delegates
	/// <summary>
	/// This is the Incoming Command event argument for the Telnet Server
	/// </summary>
	public class TelnetServerCommandEventArgs : EventArgs
	{   
		private string _Command;
		private SocketClientBuffer _Socket;

		/// <summary>
		/// This takes the incoming data byte stream, converts it to a string and
		/// allows the clients to do whatever they need with it.
		/// </summary>
		/// <param name="IncomingData">The incoming byte buffer.</param>
		/// <param name="IncomingDataLength">The length of said byte buffer.</param>
		public TelnetServerCommandEventArgs(string pCommand, SocketClientBuffer pSocket)
		{
			//Set the IP
			this._Command = pCommand;
			//Set the socket.
			this._Socket = pSocket;
		}

		// Exposed Properties.
		public string Command
		{ 
			get { return this._Command; }
		}

		public SocketClientBuffer Socket
		{
			get { return this._Socket; }
		}
	}

	public delegate void TelnetServerCommandEventHandler(object pSender, TelnetServerCommandEventArgs pEventArgs);
	#endregion

	/// <summary>
	/// The Telnet Server Class
	/// </summary>
	public class TelnetServer
	{
		private SocketServer _SocketServer;

		//Standard server communication strings
		private string _TelnetPrompt;
		private string _TelnetGreeting;
		private string _NewLine = "\r\n";

		//Some server settings
		private bool _RemoteEcho;

		#region Data Access
		public long BytesSent
		{
			get { return this._SocketServer.TotalBytesSent; }
		}

		public long BytesReceived
		{
			get { return this._SocketServer.TotalBytesReceived; }
		}

		public bool Running
		{
			get { return this._SocketServer.IsRunning; }
		}
		#endregion

		#region Server Control
		public TelnetServer(int pTcpPort)
		{
			this._SocketServer = new SocketServer (pTcpPort);

			//Add the event handlers for the server
			this._SocketServer.WriteLog += new WriteLogEventHandler(ChainedWriteLog);
			this._SocketServer._ClientConnected += new SocketServerClientEventHandler (ServerClientConnected);
			this._SocketServer._DataRecieved += new SocketServerDataEventHandler(ServerDataRecieved);
			this._SocketServer._LineRecieved += new SocketServerDataEventHandler(ServerLineRecieved);
			this._SocketServer._NewLineRecieved += new SocketServerDataEventHandler(NewLineRecieved);

			//Set the default settings
			this.Prompt = "Telnet";
			this.Greeting = "Welcome to this unconfigured telnet server!";
			this._RemoteEcho = true;
		}

		~TelnetServer()
		{
			this._SocketServer = null;
		}

		public string Prompt
		{
			set { this._TelnetPrompt = string.Concat("[", value, "]:"); }
		}

		public string Greeting
		{
			set {this._TelnetGreeting = value; }
		}

		public void Start()
		{
			if (!this._SocketServer.IsRunning)
			{
				this.WriteToLog(string.Concat("Starting Telnet Server On Port # ", this._SocketServer.Port), 1);
				this._SocketServer.Start();
			}
		}

		public void Stop()
		{
			if (this._SocketServer.IsRunning)
			{
				this.WriteToLog(string.Concat("Halting Telnet Server"), 1);
				this._SocketServer.Stop();
			}
		}
		#endregion

		#region Server Communication and Event Functions
		public event TelnetServerCommandEventHandler CommandRecieved;

		//Event firing functions
		private void CommandIsRecieved (string pCommand, SocketClientBuffer pSocket)
		{
			TelnetServerCommandEventArgs tmpEventArgs = new TelnetServerCommandEventArgs (pCommand, pSocket);
			OnCommandRecieved (tmpEventArgs);
		}

		protected virtual void OnCommandRecieved(TelnetServerCommandEventArgs tmpEventArgs)
		{
			if (CommandRecieved != null)
				//Invoke the event delegate
				CommandRecieved (this, tmpEventArgs);
		}

		private void NewLineRecieved (object pSender, SocketServerDataEventArgs pEventArgs)
		{
			try
			{
				//Send the prompt.
				this.SendPrompt (pEventArgs.Socket);
			}
			catch
			{
			}
		}

		private void ServerLineRecieved (object pSender, SocketServerDataEventArgs pEventArgs)
		{
			//Some data is recieved (a line of it, in fact) .. Send it to the master control to parse it.
			CommandIsRecieved (pEventArgs.IncomingDataString, pEventArgs.Socket);
		}

		private void ServerDataRecieved (object pSender, SocketServerDataEventArgs pEventArgs)
		{
			//Some data is recieved -- echo it back.
			if (this._RemoteEcho)
			{
				//Don't echo it if it is a backspace and there is no buffer
				if (!((pEventArgs.Socket._LineBuffer.Length < 1) && (pEventArgs.IncomingDataBytes[0] == 127)))
					_SocketServer.SendData (pEventArgs.IncomingDataBytes, pEventArgs.Socket);
			}
		}

		private void ServerClientConnected (object pSender, SocketServerClientEventArgs pEventArgs)
		{
			//A client has connected.  Send the greeting and a prompt.
			_SocketServer.SendData (string.Concat(this._TelnetGreeting, this._NewLine), pEventArgs.Socket);
			this.SendPrompt (pEventArgs.Socket);
		}

		public void SendPrompt (SocketClientBuffer pSocket)
		{
			_SocketServer.SendData (string.Concat(this._NewLine, this._TelnetPrompt), pSocket);
		}

		public void SendAllPrompt ()
		{
			_SocketServer.SendAllDataPlusBuffer (string.Concat(this._NewLine, this._TelnetPrompt));
		}

		public void SendLineFeed (SocketClientBuffer pSocket)
		{
			_SocketServer.SendData (string.Concat(this._NewLine), pSocket);
		}

		public void SendData (string pData, SocketClientBuffer pSocket)
		{
			//Send some data back to the server
			_SocketServer.SendData (pData, pSocket);
		}

		public void SendAllData (string pData)
		{
			//Send some data back to the server
			_SocketServer.SendAllData (pData);
		}
		#endregion

		#region Log File Interface
		public event WriteLogEventHandler WriteLog;

		/// <summary>
		/// Write to the log file/display.
		/// </summary>
		/// <param name="pLogText">The text to be written.</param>
		/// <param name="pLogLevel">The numeric level of the log text.  In general, 0 is screen, 1 is both, 2 is file only.</param>
		private void WriteToLog(string pLogText, int pLogLevel)
		{
			WriteLogEventArgs tmpEventArgs = new WriteLogEventArgs(pLogText, pLogLevel);
			OnWriteLog (tmpEventArgs);
		}

		protected virtual void OnWriteLog(WriteLogEventArgs pEventArgs)
		{
			if (WriteLog != null)
				//Invoke the event delegate
				WriteLog (this, pEventArgs);
		}

		private void ChainedWriteLog(object pSender, WriteLogEventArgs pEventArgs)
		{
			//A shim function to chain log events from objects here to the main application's events.
			WriteLogEventArgs tmpEventArgs = new WriteLogEventArgs(string.Concat ("Telnet ", pEventArgs.LogText), pEventArgs.LogLevel);
			//Append some useful server information to the logged events
			OnWriteLog(tmpEventArgs);
		}
		#endregion
	}
}
