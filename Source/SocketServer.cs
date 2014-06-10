/**
* This source file is a part of the Dictionariosaur application.
* For full copyright and license information, please view the LICENSE file
* which should be distributed with this source code.
*
* @license MIT License
* @copyright Copyright (c) 2013, Steven Velozo
*/
using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using MutiUtility;

namespace MutiNetworking
{
	#region Server Event Arguments and Delegates
	/// <summary>
	/// This is the Socket Data arrival event arguments.
	/// </summary>
	public class SocketServerDataEventArgs : EventArgs
	{   
		private int _IncomingDataLength;
		private byte [] _IncomingDataBytes;
		private string _IncomingDataString;
		private SocketClientBuffer _Socket;

		/// <summary>
		/// This takes the incoming data byte stream, converts it to a string and
		/// allows the clients to do whatever they need with it.
		/// </summary>
		/// <param name="pIncomingData">The incoming byte buffer.</param>
		/// <param name="pIncomingDataLength">The length of said byte buffer.</param>
		public SocketServerDataEventArgs(byte [] pIncomingData, int pIncomingDataLength, SocketClientBuffer pSocket)
		{
			//Set up the byte buffer first
			this._IncomingDataLength = pIncomingDataLength;
			this._IncomingDataBytes = pIncomingData;

			//Now encode the ascii string
			this._IncomingDataString = System.Text.Encoding.ASCII.GetString(pIncomingData, 0, pIncomingDataLength);

			//Now set the Socket it came in on
			this._Socket = pSocket;
		}

		public SocketServerDataEventArgs(string pIncomingLine, SocketClientBuffer pSocket)
		{
			//Set up the string first
			this._IncomingDataString = pIncomingLine;
			
			//Now the byte buffer
			this._IncomingDataBytes = System.Text.Encoding.ASCII.GetBytes (pIncomingLine);
			this._IncomingDataLength = this._IncomingDataBytes.Length;

			//Now set the Socket it came in on
			this._Socket = pSocket;
		}

		// Exposed Properties.
		public int IncomingDataLength
		{ 
			get { return this._IncomingDataLength; }
		}

		public byte [] IncomingDataBytes
		{
			get { return this._IncomingDataBytes; }
		}    

		public string IncomingDataString
		{
			get { return this._IncomingDataString; }
		}

		public SocketClientBuffer Socket
		{
			get { return this._Socket; }
		}
	}
	
	/// <summary>
	/// This is the client event arguments, holding IP address.
	/// </summary>
	public class SocketServerClientEventArgs : EventArgs
	{   
		private string _IPAddress;
		private SocketClientBuffer _Socket;

		/// <summary>
		/// This takes the incoming data byte stream, converts it to a string and
		/// allows the clients to do whatever they need with it.
		/// </summary>
		/// <param name="IncomingData">The incoming byte buffer.</param>
		/// <param name="IncomingDataLength">The length of said byte buffer.</param>
		public SocketServerClientEventArgs(string IPAddress, SocketClientBuffer Socket)
		{
			//Set the IP
			this._IPAddress = IPAddress;
			//Set the socket.
			this._Socket = Socket;
		}

		// Exposed Properties.
		public string IPAddress
		{ 
			get { return this._IPAddress; }
		}

		public SocketClientBuffer Socket
		{
			get { return this._Socket; }
		}
	}
	
	/// <summary>
	/// The default event handler for all socket server events not containing data
	/// </summary>
	public delegate void SocketServerDataEventHandler(object pSender, SocketServerDataEventArgs pEventArgs);
	public delegate void SocketServerClientEventHandler(object pSender, SocketServerClientEventArgs pEventArgs);
	#endregion

	/// <summary>
	/// A transparent, modular asynchronous socket class
	/// </summary>
	public class SocketServer
	{
		//Internal Server Properties
		private ArrayList _ClientsConnected;		//List of Client Connections
		private int _TcpPort;						//Port to connect on
		private Socket _ServerListener;				//The listening stream
		private bool _IsRunning;

		private long _BytesSent;
		private long _BytesReceived;

		public SocketServer(int pPort)
		{
			this._ClientsConnected = new ArrayList();
			this._TcpPort = pPort;
			this._IsRunning = false;

			this._BytesSent = 0;
			this._BytesReceived = 0;
		}

		public ArrayList ClientList
		{
			get { return this._ClientsConnected; }
		}

		#region Server Control
		public void Start()
		{
			try
			{
				if (!this._IsRunning)
				{
					// Determine the IPAddress of this machine
					IPAddress [] tmpLocalAddresses = null;
					String tmpHostName = "";
					try
					{
						tmpHostName = Dns.GetHostName();
						IPHostEntry tmpIpEntry = Dns.GetHostEntry(tmpHostName);
						tmpLocalAddresses = tmpIpEntry.AddressList;
					}
					catch
					{
						IPHostEntry tmpIpEntry = Dns.GetHostEntry("localhost");
						tmpLocalAddresses = tmpIpEntry.AddressList;
						//System.Diagnostics.Debug.WriteLine ("Error trying to get local address.");
					}


					// Verify we got an IP address. Tell the user if we did
					if(tmpLocalAddresses == null || tmpLocalAddresses.Length < 1)
						return;
					// Create the listener socket in this machines IP address
					_ServerListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
					_ServerListener.Bind(new IPEndPoint(IPAddress.Any, _TcpPort));
					_ServerListener.Listen(10);

					// Setup a callback to be notified of connection requests
					_ServerListener.BeginAccept(new AsyncCallback(this.OnConnectRequest), _ServerListener);

					//Fire the started event
					this.ServerHasStarted();

					this.WriteToLog(String.Concat("Listening on [", tmpHostName, "]", tmpLocalAddresses[0], ":", this._TcpPort), 1);
				}
			}
			catch
			{
				this.WriteToLog(String.Concat("Can't bind to port ", this._TcpPort, " probably because another server is hogging it."), 1);
			}
		}

		public void Stop()
		{
			if (this._IsRunning)
			{
				// Clean up before we go home
				_ServerListener.Close();
				GC.Collect();
				GC.WaitForPendingFinalizers();

				//Fire the server stopped event
				this.ServerHasStopped();
			}
		}

		public void SendData (byte[] pData, SocketClientBuffer pClient)
		{
			try
			{
				pClient.Sock.Send(pData);
				this._BytesSent += pData.Length;
			}
			catch
			{
				// If the send fails the close the connection
				//Raise the disconnected event
				this.ClientHasDisconnected (pClient.Sock.RemoteEndPoint.ToString(), pClient);
				this.WriteToLog (string.Concat("Client Disconnected from [", pClient.Sock.RemoteEndPoint, "]"), 2);

				//Kill the socket
				pClient.Sock.Close();
				_ClientsConnected.Remove(pClient);
				return;
			}
		}

		public void SendData (string pData, SocketClientBuffer pClient)
		{
			this.SendData(System.Text.Encoding.ASCII.GetBytes (pData), pClient);
		}

		public void SendDataPlusBuffer (byte[] pData, SocketClientBuffer pClient)
		{
			try
			{
				pClient.Sock.Send(pData);
				this._BytesSent += pData.Length;
				pClient.Sock.Send(System.Text.Encoding.ASCII.GetBytes(pClient._LineBuffer.ToString()));
				this._BytesSent += System.Text.Encoding.ASCII.GetBytes(pClient._LineBuffer.ToString()).Length;
			}
			catch
			{
				// If the send fails the close the connection
				//Raise the disconnected event
				this.ClientHasDisconnected (pClient.Sock.RemoteEndPoint.ToString(), pClient);
				this.WriteToLog (string.Concat("Client Disconnected from [", pClient.Sock.RemoteEndPoint, "]"), 2);

				//Kill the socket
				pClient.Sock.Close();
				_ClientsConnected.Remove(pClient);
				return;
			}
		}

		public void SendDataPlusBuffer (string pData, SocketClientBuffer pClient)
		{
			this.SendDataPlusBuffer(System.Text.Encoding.ASCII.GetBytes (pData), pClient);
		}

		public void SendAllData (byte[] pData)
		{
			// Send Data to all clients
			foreach(SocketClientBuffer Client in _ClientsConnected)
				SendData(pData, Client);
		}

		public void SendAllData (string pData)
		{
			//This will automagically convert strings to bytes
			this.SendAllData (System.Text.Encoding.ASCII.GetBytes(pData));
		}

		public void SendAllDataPlusBuffer (byte[] pData)
		{
			// Send Data to all clients, plus their incomplete input buffers
			foreach(SocketClientBuffer Client in _ClientsConnected)
				this.SendDataPlusBuffer(pData, Client);
		}

		public void SendAllDataPlusBuffer (string pData)
		{
			//This will automagically convert strings to bytes
			this.SendAllDataPlusBuffer (System.Text.Encoding.ASCII.GetBytes(pData));
		}
		#endregion

		#region Data Access
		public bool IsRunning
		{
			get { return this._IsRunning; }
		}

		public int Port
		{
			get { return this._TcpPort; }
		}

		public long TotalBytesSent
		{
			get { return this._BytesSent; }
		}

		public long TotalBytesReceived
		{
			get { return this._BytesReceived; }
		}
		#endregion

		#region Server Events
		//Server Event Definitions
		public event SocketServerClientEventHandler _ClientConnected;
		public event SocketServerClientEventHandler _ClientDisconnected;

		//This is fired whenever byte data is recieved
		public event SocketServerDataEventHandler _DataRecieved;

		//This is fired when textual lines are recieved
		public event SocketServerDataEventHandler _LineRecieved;

		//This is fired when tabs/newlines are recieved
		public event SocketServerDataEventHandler _TabRecieved;
		public event SocketServerDataEventHandler _BackspaceRecieved;
		public event SocketServerDataEventHandler _NewLineRecieved;

		//Server status events
		public event EventHandler _ServerStarted;
		public event EventHandler _ServerStopped;

		//Event firing functions
		private void ClientHasConnected (string pClientIP, SocketClientBuffer pSocket)
		{
			SocketServerClientEventArgs tmpEventArgs = new SocketServerClientEventArgs (pClientIP, pSocket);
			OnClientConnected (tmpEventArgs);
		}

		private void ClientHasDisconnected (string pClientIP, SocketClientBuffer pSocket)
		{
			SocketServerClientEventArgs tmpEventArgs = new SocketServerClientEventArgs (pClientIP, pSocket);
			OnClientDisconnected (tmpEventArgs);
		}

		private void DataWasRecieved (byte [] pData, int pDataLength, SocketClientBuffer pSocket)
		{
			SocketServerDataEventArgs tmpEventArgs = new SocketServerDataEventArgs (pData, pDataLength, pSocket);
			this._BytesReceived += pDataLength;
			OnDataRecieved (tmpEventArgs);
		}

		private void LineWasRecieved (string pLine, SocketClientBuffer pSocket)
		{
			SocketServerDataEventArgs tmpEventArgs = new SocketServerDataEventArgs (pLine, pSocket);
			OnLineRecieved (tmpEventArgs);
		}

		private void NewLineWasRecieved (SocketClientBuffer pSocket)
		{
			SocketServerDataEventArgs tmpEventArgs = new SocketServerDataEventArgs ("\n", pSocket);
			OnNewLineRecieved (tmpEventArgs);
		}

		private void TabWasRecieved (SocketClientBuffer pSocket)
		{
			SocketServerDataEventArgs tmpEventArgs = new SocketServerDataEventArgs ("\t", pSocket);
			OnTabRecieved (tmpEventArgs);
		}

		private void BackspaceWasRecieved (SocketClientBuffer pSocket)
		{
			SocketServerDataEventArgs tmpEventArgs = new SocketServerDataEventArgs (Convert.ToChar(127).ToString(), pSocket);
			OnBackspaceRecieved (tmpEventArgs);
		}

		private void ServerHasStarted ()
		{
			EventArgs tmpEventArgs = new EventArgs();
			OnServerStarted (tmpEventArgs);
		}

		private void ServerHasStopped ()
		{
			EventArgs tmpEventArgs = new EventArgs();

			OnServerStopped (tmpEventArgs);
		}

		//Protected event handling functions
		protected virtual void OnClientConnected(SocketServerClientEventArgs pEventArgs)
		{
			if (_ClientConnected != null)
				//Invoke the event delegate
				_ClientConnected (this, pEventArgs);
		}

		protected virtual void OnClientDisconnected(SocketServerClientEventArgs pEventArgs)
		{
			if (_ClientDisconnected != null)
				//Invoke the event delegate
				_ClientDisconnected (this, pEventArgs);
		}

		protected virtual void OnDataRecieved(SocketServerDataEventArgs pEventArgs)
		{
			if (_DataRecieved != null)
				//Invoke the event delegate
				_DataRecieved (this, pEventArgs);
		}

		protected virtual void OnLineRecieved(SocketServerDataEventArgs pEventArgs)
		{
			if (_LineRecieved != null)
				//Invoke the event delegate
				_LineRecieved (this, pEventArgs);
		}

		protected virtual void OnTabRecieved(SocketServerDataEventArgs pEventArgs)
		{
			if (_TabRecieved != null)
				//Invoke the event delegate
				_TabRecieved (this, pEventArgs);
		}
		
		protected virtual void OnNewLineRecieved(SocketServerDataEventArgs pEventArgs)
		{
			if (_NewLineRecieved != null)
				//Invoke the event delegate
				_NewLineRecieved (this, pEventArgs);
		}
		
		protected virtual void OnBackspaceRecieved(SocketServerDataEventArgs pEventArgs)
		{
			if (_BackspaceRecieved != null)
				//Invoke the event delegate
				_BackspaceRecieved (this, pEventArgs);
		}
		
		protected virtual void OnServerStarted(EventArgs pEventArgs)
		{
			if (_ServerStarted != null)
				//Invoke the event delegate
				_ServerStarted (this, pEventArgs);
		}
		
		protected virtual void OnServerStopped(EventArgs pEventArgs)
		{
			if (_ServerStopped != null)
				//Invoke the event delegate
				_ServerStopped (this, pEventArgs);
		}
		#endregion

		#region Asynchronous Threading Control
		/// <summary>
		/// Callback used when a client requests a connection. 
		/// </summary>
		/// <param name="pAsyncResult"></param>
		public void OnConnectRequest(IAsyncResult pAsyncResult)
		{
			//This juggles around the Socket to a new listener class using the AsyncState results
			//Accept the connection
			Socket tmpClientListener = (Socket)pAsyncResult.AsyncState;
			//Add it to our list
			NewConnection(tmpClientListener.EndAccept(pAsyncResult));
			//Set up another listener to accept more connections
			tmpClientListener.BeginAccept(new AsyncCallback(OnConnectRequest), tmpClientListener);
		}

		/// <summary>
		/// Add the given connection to our list of clients.
		/// </summary>
		/// <param name="pSocket">Connection socket</param>
		public void NewConnection(Socket pSocket)
		{
			// This will block accepting sockets until a client is shuffled to 
			// a new socket in the buffer.  This should stop any issues if
			// the server is DOS'd because of it's limit on connection
			// speed, however, load would be somewhat limited (i.e. you could
			// not have more than 1000 connections in a second).
			//
			// For my current uses of this library these limitations should be fine.
			SocketClientBuffer tmpClient = new SocketClientBuffer(pSocket);
			_ClientsConnected.Add(tmpClient);
			tmpClient._RemoteInformation = pSocket.RemoteEndPoint.ToString();
			this.WriteToLog (string.Concat("Client Connected from [", tmpClient._RemoteInformation, "]"), 1);
			tmpClient.Die += new SocketBufferEventHandler(ClientDied);

            //Raise the connection event
			this.ClientHasConnected(tmpClient.Sock.RemoteEndPoint.ToString(), tmpClient);
			tmpClient.SetupRecieveCallback(this);
		}

		public void ClientDied (object pSender, SocketBufferEventArgs pEventArgs)
		{
			try
			{
				_ClientsConnected.Remove(pEventArgs);
				this.WriteToLog (string.Concat("Client Disconnected from [", pEventArgs.Client.Sock.RemoteEndPoint, "]"), 2);
			}
			catch
			{
			}
		}

		/// <summary>
		/// Recieve the data, and handle it in the class.
		/// </summary>
		/// <param name="pAsyncResult"></param>
		public void OnRecievedData(IAsyncResult pAsyncResult)
		{
			SocketClientBuffer tmpClientBuffer = (SocketClientBuffer)pAsyncResult.AsyncState;

			byte [] tmpRecievedBuffer = tmpClientBuffer.GetRecievedData(pAsyncResult);

			// If no data was recieved then the connection is probably dead
			if(tmpRecievedBuffer.Length < 1)
			{
				//Raise the disconnected event
				if (tmpClientBuffer._CleanUp == true)
					this.ClientHasDisconnected (tmpClientBuffer._RemoteInformation, tmpClientBuffer);
				else
				{
					this.ClientHasDisconnected (tmpClientBuffer.Sock.RemoteEndPoint.ToString(), tmpClientBuffer);
					//Kill the socket
					tmpClientBuffer.Sock.Close();
					this._ClientsConnected.Remove(tmpClientBuffer);
				}

				this.WriteToLog (string.Concat("Client Disconnected from [", tmpClientBuffer._RemoteInformation, "]"), 2);
				
				return;
			}
			else
			{
				//Raise the recieved data event
				this.DataWasRecieved(tmpRecievedBuffer, tmpRecievedBuffer.Length, tmpClientBuffer);

				//Walk through the bytes that just came through and such.
				//This could be optional?  We may not need this granularity on incoming data.
				foreach (byte tmpByte in tmpRecievedBuffer)
				{
					switch (tmpByte) 
					{
						case 10:
							//A CR was recieved
							if (tmpClientBuffer._LineBuffer.Length > 0)
							{
								string tmpBuffer = tmpClientBuffer._LineBuffer.ToString();

								//Clear the linebuffer
								tmpClientBuffer._LineBuffer.Remove(0, tmpClientBuffer._LineBuffer.Length);

								//Now fire the event
								this.LineWasRecieved(tmpBuffer, tmpClientBuffer);
							}
							this.NewLineWasRecieved(tmpClientBuffer);
							tmpClientBuffer._InputTouched = true;
							break;

						case 127:
							//A Backspace was recieved, remove the end character
							if (tmpClientBuffer._LineBuffer.Length > 0)
							{
								tmpClientBuffer._LineBuffer.Remove (tmpClientBuffer._LineBuffer.Length - 1, 1);
								this.BackspaceWasRecieved(tmpClientBuffer);
							}
							break;

						default:
							//See if it's an "ok" ascii character
							if ((tmpByte >  13) && (tmpByte < 147) && (tmpClientBuffer._LineBuffer.Length < 300))
							{
								//Append it to our line buffer
								tmpClientBuffer._LineBuffer.Append (Convert.ToChar (tmpByte));
							}
							break;
					}
				}
			}

			tmpClientBuffer.SetupRecieveCallback(this);
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

		protected virtual void OnWriteLog(WriteLogEventArgs tmpEventArgs)
		{
			if (WriteLog != null)
				//Invoke the event delegate
				WriteLog (this, tmpEventArgs);
		}
		#endregion
	}

	#region Custom Client Buffer Class
	#region Client Buffer Event Arguments
	public class SocketBufferEventArgs : EventArgs 
	{   
		private SocketClientBuffer _Client;
        
		public SocketBufferEventArgs(SocketClientBuffer pClient)
		{
			//Nothing fancy to be done, this is a data encapsulation class
			this._Client = pClient;
		}

		// Properties.
		public SocketClientBuffer Client
		{
			get { return this._Client; }
		}
	}

	/// <summary>
	/// This is a delegate that offers interfaces to log files and rolling log displays.
	/// </summary>	
	public delegate void SocketBufferEventHandler (object pSender, SocketBufferEventArgs pEventArgs);
	#endregion

	/// <summary>
	/// Class holding the socket and buffers for Client socket connection
	/// </summary>
	public class SocketClientBuffer
	{
		private Socket _ClientSocket;						// Connection to the client

		private byte[] _ByteBuffer = new byte[100];			// Receive data buffer
		public System.Text.StringBuilder _LineBuffer = new System.Text.StringBuilder();

		public String _RemoteInformation = "";

		public bool _InputTouched;							//This is set to true whenever a data line is recieved
		public bool _SendingData;

		public bool _CleanUp;								//This turns the class into a timebomb

		/// <summary>
		/// Client Socket Buffer
		/// </summary>
		/// <param name="sock">Client Socket that is recieving data.</param>
		public SocketClientBuffer(Socket pNewSocket)
		{
			_ClientSocket = pNewSocket;
			_SendingData = false;
			_CleanUp = false;
		}

		public Socket Sock
		{
			get { return _ClientSocket; }
		}

		//These two interact to allow a socket to kill itself.
		//Hari kari style.
		public event SocketBufferEventHandler Die;

		public void Kill()
		{
			try
			{
				if (_SendingData)
				{
					this._CleanUp = true;
				}
				else
				{
					this._CleanUp = true;

					SocketBufferEventArgs e = new SocketBufferEventArgs(this);

					this.OnKill(e);

					this.Sock.Close();
				}
			}
			catch
			{
			}
		}

		protected virtual void OnKill(SocketBufferEventArgs pEventArgs)
		{
			if (this.Die != null)
				this.Die(this, pEventArgs);
		}

		/// <summary>
		/// Setup the callback for recieved data and loss of connection
		/// </summary>
		public void SetupRecieveCallback(SocketServer pSocket)
		{
			try
			{
				AsyncCallback tmpRecieveData = new AsyncCallback(pSocket.OnRecievedData);
				_ClientSocket.BeginReceive(_ByteBuffer, 0, _ByteBuffer.Length, SocketFlags.None, tmpRecieveData, this);
			}
			catch
			{
				System.Diagnostics.Debug.WriteLine("Recieve callback setup failed!");
			}
		}

		/// <summary>
		/// Setup the callback for recieved data and loss of connection
		/// </summary>
		public void SendData(byte[] pData)
		{
			try
			{
				this._SendingData = true;

				AsyncCallback tmpSendData = new AsyncCallback(this.OnSentData);

				this.Sock.BeginSend(pData, 0, pData.Length, SocketFlags.None, tmpSendData, this);
			}
			catch
			{
				System.Diagnostics.Debug.WriteLine("Send callback setup failed!");
			}
		}


		public void OnSentData(IAsyncResult pAsyncResult)
		{
			this._SendingData = false;

			//Kill the socket if it is slated for cleanup.
			if (this._CleanUp)
				this.Kill();
		}


		/// <summary>
		/// Data has been recieved -- return it a buffer at a time.
		/// </summary>
		/// <param name="pAsyncResult"></param>
		/// <returns>Array of bytes containing the received data</returns>
		public byte [] GetRecievedData(IAsyncResult pAsyncResult)
		{
			int tmpBytesRecieved = 0;
			try
			{
				tmpBytesRecieved = _ClientSocket.EndReceive(pAsyncResult);
			}
			catch
			{
			}
			byte [] tmpBytesReturned = new byte[tmpBytesRecieved];
			Array.Copy(_ByteBuffer, tmpBytesReturned, tmpBytesRecieved);
			return tmpBytesReturned;
		}
	}
	#endregion
}
