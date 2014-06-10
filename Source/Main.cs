/**
* This source file is a part of the Dictionariosaur application.
* For full copyright and license information, please view the LICENSE file
* which should be distributed with this source code.
*
* @license MIT License
* @copyright Copyright (c) 2013, Steven Velozo
*
* Dictionariosaur is an application meant to provide dictionary tools for
* scrabble and anagramming.
*
* Searchable lists via telnet, http and GUI as well as (eventually) timer
* functions to allow game-timers.  Will require chat functions at that
* point as well.
*/
using System;
using System.Threading;
using MutiWordList;
using MutiDataStructures;
using MutiUtility;
using MutiNetworking;

namespace Dictionariosaur
{
	class MainClass
	{
		public static void Main(string[] pArguments)
		{
			//Instantiate our class that encapsulates everything into a thread
			MainApplication tmpWorkingApplicationThread;
			tmpWorkingApplicationThread = new MainApplication();
			// The number is the poll length in ms for each thread tick
			tmpWorkingApplicationThread.ThreadedTask(250);
		}
	}

	/// <summary>
	/// This class encapsulates all the structures, threads and data that is required to keep this
	/// program running.  It is segregated from the actual interface so we can easily compile it
	/// into a GUI application if we so desired.
	/// </summary>
	public class MainApplication : MasterLoggedClass
	{
		//This keeps a thread running as long as it is true.
		private bool _KeepRunning;

		// The server Log Files
		private LogFile _TelnetLogFile;

		//Telnet Server
		private TelnetServer _TelnetServer;

		//Word List
		private WordList _WordList;


		public MainApplication()
		{
			//Configure the main application log
			LogFilePrefix = "Application";
			LogFileToConsole = true;
			WriteToLog ("Loading and starting the application");

			//Configure the telnet server and the log file attached to it.
			_TelnetLogFile = new LogFile ("Telnet");
			_TelnetLogFile.EchoToConsole = true;
			_TelnetServer = new TelnetServer (9000);

			//Attach the Write Log event to our pass-through write log event handler
			_TelnetServer.WriteLog += new WriteLogEventHandler(ChainedTelnetWriteLog);
			//Attach the telnet server command recieved event to our handler
			_TelnetServer.CommandRecieved += new TelnetServerCommandEventHandler(TelnetCommandRecieved);

			InitializeTelnetServer();

			_WordList = new WordList();
			_WordList.WriteLog += new WriteLogEventHandler(ChainedWriteLog);
		}

		/// <summary>
		/// This is useful to keep the console application running.  Since everything is an
		/// asynchronous callback, this really is just here until the user presses "Ctrl + C".
		/// </summary>
		public void ThreadedTask(int pLatency)
		{
			_KeepRunning = true;
			//Start a thread that keeps the class alive.
			while(_KeepRunning)
				Thread.Sleep(pLatency);
			WriteToLog ("Unloading the application");
		}

		#region String Functions
		public string GenerateAlphagram (string pFromString)
		{
			// The string we'll return
			System.Text.StringBuilder tmpSortedString;

			tmpSortedString = new System.Text.StringBuilder();

			// The byte array we'll use to sort
			Array tmpStringByteArray = Array.CreateInstance(typeof(Byte), pFromString.Length);

			// Now we need to assign FromString to the array
			tmpStringByteArray = System.Text.Encoding.ASCII.GetBytes (pFromString);

			// Now sort it (good thing bytes are icomparible)
			Array.Sort(tmpStringByteArray);

			// Now convert it back to a unicode string.
			foreach (byte tmpByte in tmpStringByteArray)
			{
				tmpSortedString.Append(Convert.ToChar(tmpByte));
			}

			WriteToLog("Converted string ["+pFromString+"] to Alphagram ["+tmpSortedString.ToString().Trim()+"]");

			return tmpSortedString.ToString().Trim();
		}
		#endregion

		#region Telnet Server Interface
		/// <summary>
		/// Initialize the server prompt and greeting, and start the server.
		/// </summary>
		private void InitializeTelnetServer ()
		{
			//Set the prompt
			//Possibly this could contain the "timer" function of the GUI application this is a rewrite of
			_TelnetServer.Prompt = "Dictionariosaur";

			//Do our pretty Ascii telnet greeter
			string tmpTelnetGreeting = "";
			tmpTelnetGreeting = string.Concat(tmpTelnetGreeting, "MUTINATION\r\n");
			tmpTelnetGreeting = string.Concat(tmpTelnetGreeting, "----------------------------------------------------| Scrabble Dictionary |----\r\n");
			_TelnetServer.Greeting = tmpTelnetGreeting;

			_TelnetServer.Start();
		}

		/// <summary>
		/// This gets called whenever a command is recieved from
		/// </summary>
		/// <param name="pSender">The telnet server instance</param>
		/// <param name="pEventArgs">The event args, including the socket</param>
		private void TelnetCommandRecieved (object pSender, TelnetServerCommandEventArgs pEventArgs)
		{
			//A command has arrived from a telnet client.  Parse it.
			//Tokenize the command string
			string [] tmpCommandArguments = pEventArgs.Command.Split (' ');

			if (tmpCommandArguments.Length > 0)
			{
				switch (tmpCommandArguments[0].ToUpper())
				{
					case "ADD":
						if (tmpCommandArguments.Length > 1)
						{
							System.Text.StringBuilder tmpSearchString = new System.Text.StringBuilder();

							for (int tmpCounter = 1; tmpCounter < tmpCommandArguments.Length; tmpCounter++)
							{
								//Add back the spaces to make the search meaningful.
								if (tmpSearchString.Length > 0)
									tmpSearchString.Append(" ");

								tmpSearchString.Append(tmpCommandArguments[tmpCounter].ToString());
							}

							_TelnetServer.SendData ("Adding ["+tmpSearchString.ToString()+"] to the word list.", pEventArgs.Socket);
							_TelnetServer.SendLineFeed (pEventArgs.Socket);

							_WordList.AddWord(tmpSearchString.ToString());

							_TelnetServer.SendData ("There are now "+_WordList.Count+" items in the word list.", pEventArgs.Socket);
							_TelnetServer.SendLineFeed (pEventArgs.Socket);
						}
						else
						{
							_TelnetServer.SendData ("Use: ADD [word]", pEventArgs.Socket);
							_TelnetServer.SendLineFeed (pEventArgs.Socket);
						}
						break;

					case "AL":
					case "ALP":
					case "ALPH":
					case "ALPHA":
						if (tmpCommandArguments.Length > 1)
						{
							System.Text.StringBuilder tmpSearchString = new System.Text.StringBuilder();

							for (int tmpCounter = 1; tmpCounter < tmpCommandArguments.Length; tmpCounter++)
							{
								//Add back the spaces to make the search meaningful.
								if (tmpSearchString.Length > 0)
									tmpSearchString.Append(" ");

								tmpSearchString.Append(tmpCommandArguments[tmpCounter].ToString());
							}

							_TelnetServer.SendData ("Generating Alphagram from ["+tmpSearchString.ToString()+"]", pEventArgs.Socket);
							_TelnetServer.SendData (" resulting in ["+ GenerateAlphagram (tmpSearchString.ToString()) +"]", pEventArgs.Socket);
							_TelnetServer.SendLineFeed (pEventArgs.Socket);
							_TelnetServer.SendLineFeed (pEventArgs.Socket);
						}
						else
						{
							_TelnetServer.SendData ("Use: ALPHA [some text]", pEventArgs.Socket);
							_TelnetServer.SendLineFeed (pEventArgs.Socket);
						}
						break;

					case "CO":
					case "COU":
					case "COUN":
					case "COUNT":
						_TelnetServer.SendData ("There are "+_WordList.Count+" items in the word list.", pEventArgs.Socket);
						_TelnetServer.SendLineFeed (pEventArgs.Socket);
						break;

					case "EXIT":
						//Kill the socket
						pEventArgs.Socket.Kill();
						break;

					case "?":
					case "H":
					case "HE":
					case "HEL":
					case "HELP":
						SendTelnetHelp (pEventArgs);
						break;

					case "INFO":
						_TelnetServer.SendData ("         Linked List Information", pEventArgs.Socket);
						_TelnetServer.SendLineFeed (pEventArgs.Socket);
						_TelnetServer.SendData ("-------------------------------------------", pEventArgs.Socket);
						_TelnetServer.SendLineFeed (pEventArgs.Socket);
						_TelnetServer.SendData ("         List Count: "+_WordList.Count, pEventArgs.Socket);
						_TelnetServer.SendLineFeed (pEventArgs.Socket);
						if (_WordList.Count > 0)
						{
		  					_TelnetServer.SendData (String.Format("       Current Item: ({0}) {1} ... Alphagram[{2}]", _WordList.CurrentItemKey ,_WordList.CurrentWord, _WordList.CurrentWordAlphagram), pEventArgs.Socket);
							_TelnetServer.SendLineFeed (pEventArgs.Socket);
						}
						_TelnetServer.SendData ("          Add Count: "+_WordList.StatisticAddCount, pEventArgs.Socket);
						_TelnetServer.SendLineFeed (pEventArgs.Socket);
						_TelnetServer.SendData ("       Delete Count: "+_WordList.StatisticDeleteCount, pEventArgs.Socket);
						_TelnetServer.SendLineFeed (pEventArgs.Socket);
						_TelnetServer.SendData ("       Search Count: "+_WordList.StatisticSearchCount, pEventArgs.Socket);
						_TelnetServer.SendLineFeed (pEventArgs.Socket);
						_TelnetServer.SendData (" Search Match Count: "+_WordList.StatisticSearchMatchCount, pEventArgs.Socket);
						_TelnetServer.SendLineFeed (pEventArgs.Socket);
						_TelnetServer.SendData ("   Navigation Count: "+_WordList.StatisticNavigationCount, pEventArgs.Socket);
						_TelnetServer.SendLineFeed (pEventArgs.Socket);
						break;

					case "LS":
					case "LI":
					case "LIS":
					case "LIST":
						if (tmpCommandArguments.Length > 1)
						{
							System.Text.StringBuilder tmpSearchString = new System.Text.StringBuilder();

							for (int tmpCounter = 1; tmpCounter < tmpCommandArguments.Length; tmpCounter++)
							{
								//Add back the spaces to make the search meaningful.
								if (tmpSearchString.Length > 0)
									tmpSearchString.Append(" ");

								tmpSearchString.Append(tmpCommandArguments[tmpCounter].ToString());
							}

							SendTelnetWordList (pEventArgs, tmpSearchString.ToString().Trim());
						}
						else
						{
							_TelnetServer.SendData ("Use: LIST [option]", pEventArgs.Socket);
							_TelnetServer.SendLineFeed (pEventArgs.Socket);
							_TelnetServer.SendData ("     If you want a full list, use 'LIST ALL'", pEventArgs.Socket);
							_TelnetServer.SendLineFeed (pEventArgs.Socket);
						}
						break;

					case "LOAD":
						System.Text.StringBuilder tmpFileName = new System.Text.StringBuilder();

						if (tmpCommandArguments.Length < 2)
						{
							_TelnetServer.SendData ("No File Specified; Using 'Scrabble.txt'", pEventArgs.Socket);
							_TelnetServer.SendLineFeed (pEventArgs.Socket);

							tmpFileName.Append("Scrabble.txt");
						}
						else
						{

							for (int tmpCounter = 1; tmpCounter < tmpCommandArguments.Length; tmpCounter++)
							{
								//Add back the spaces to make the search meaningful.
								if (tmpFileName.Length > 0)
									tmpFileName.Append(" ");

								tmpFileName.Append(tmpCommandArguments[tmpCounter].ToString());
							}
						}

						_TelnetServer.SendData ("Loading ["+tmpFileName.ToString()+"] into the word list.", pEventArgs.Socket);
						_TelnetServer.SendLineFeed (pEventArgs.Socket);

						_WordList.LoadWordList (tmpFileName.ToString());

						_TelnetServer.SendData ("There are now "+_WordList.Count+" items in the word list.", pEventArgs.Socket);
						_TelnetServer.SendLineFeed (pEventArgs.Socket);
						break;

					case "QU":
					case "QUI":
						_TelnetServer.SendData ("You must fully type QUIT to close the program.", pEventArgs.Socket);
						_TelnetServer.SendLineFeed (pEventArgs.Socket);
						break;

					case "QUIT":
						//Kill the socket, close the application
						pEventArgs.Socket.Kill();
						_KeepRunning = false;
						break;

					case "SEARCH":
						if (tmpCommandArguments.Length > 1)
						{
							System.Text.StringBuilder tmpSearchString = new System.Text.StringBuilder();

							for (int tmpCounter = 1; tmpCounter < tmpCommandArguments.Length; tmpCounter++)
							{
								//Add back the spaces to make the search meaningful.
								if (tmpSearchString.Length > 0)
									tmpSearchString.Append(" ");

								tmpSearchString.Append(tmpCommandArguments[tmpCounter].ToString());
							}

							_TelnetServer.SendData ("Searching for word ["+tmpSearchString.ToString()+"] in the word list.", pEventArgs.Socket);
							_TelnetServer.SendLineFeed (pEventArgs.Socket);

							if (_WordList.FindFirstByWord (tmpSearchString.ToString()))
							{
								_TelnetServer.SendData ("Found a match!", pEventArgs.Socket);
								_TelnetServer.SendLineFeed (pEventArgs.Socket);
								_TelnetServer.SendData (String.Format(" {0}. {1}          Alphagram[{2}]", _WordList.CurrentItemKey ,_WordList.CurrentWord, _WordList.CurrentWordAlphagram), pEventArgs.Socket);
								_TelnetServer.SendLineFeed (pEventArgs.Socket);
							}
							else
							{
								_TelnetServer.SendData ("No matches!\n\r", pEventArgs.Socket);
								_TelnetServer.SendLineFeed (pEventArgs.Socket);
							}
						}
						else
						{
							_TelnetServer.SendData ("Use: WORD [number]", pEventArgs.Socket);
							_TelnetServer.SendLineFeed (pEventArgs.Socket);
						}
						break;

					case "SEARCHALPHA":
						if (tmpCommandArguments.Length > 1)
						{
							System.Text.StringBuilder tmpSearchString = new System.Text.StringBuilder();

							for (int tmpCounter = 1; tmpCounter < tmpCommandArguments.Length; tmpCounter++)
							{
								//Add back the spaces to make the search meaningful.
								if (tmpSearchString.Length > 0)
									tmpSearchString.Append(" ");

								tmpSearchString.Append(tmpCommandArguments[tmpCounter].ToString());
							}

							SendTelnetAlphaWordList (pEventArgs, tmpSearchString.ToString().Trim());
						}
						else
						{
							_TelnetServer.SendData ("Use: SEARCHALPHA [option]", pEventArgs.Socket);
							_TelnetServer.SendLineFeed (pEventArgs.Socket);
							_TelnetServer.SendData ("     If you want a full list, use 'LIST ALL'", pEventArgs.Socket);
							_TelnetServer.SendLineFeed (pEventArgs.Socket);
						}
						break;

					case "WORD":
						if (tmpCommandArguments.Length > 1)
						{
							long tmpIndexToFind = Convert.ToInt32(tmpCommandArguments[1]);
							_TelnetServer.SendData ("Searching for word #["+tmpIndexToFind.ToString()+"] in the word list.", pEventArgs.Socket);
							_TelnetServer.SendLineFeed (pEventArgs.Socket);

							if (_WordList.FindFirstByIndex (tmpIndexToFind))
							{
								_TelnetServer.SendData ("Found a match!", pEventArgs.Socket);
								_TelnetServer.SendLineFeed (pEventArgs.Socket);
								_TelnetServer.SendData (String.Format(" {0}. {1}          Alphagram[{2}]", _WordList.CurrentItemKey ,_WordList.CurrentWord, _WordList.CurrentWordAlphagram), pEventArgs.Socket);
								_TelnetServer.SendLineFeed (pEventArgs.Socket);
							}
							else
							{
								_TelnetServer.SendData ("No matches!\n\r", pEventArgs.Socket);
								_TelnetServer.SendLineFeed (pEventArgs.Socket);
							}
						}
						else
						{
							_TelnetServer.SendData ("Use: WORD [number]", pEventArgs.Socket);
							_TelnetServer.SendLineFeed (pEventArgs.Socket);
						}
						break;

					default:
						//Do nothing but be a little helpful on default.
						_TelnetServer.SendData ("What are you doing, Dave.\n\rType 'help' for a list of commands.", pEventArgs.Socket);
						_TelnetServer.SendLineFeed (pEventArgs.Socket);
						break;
				}
			}
		}

		private void SendTelnetHelp (TelnetServerCommandEventArgs e)
		{
			_TelnetServer.SendData ("Available Commands:", e.Socket);
			_TelnetServer.SendLineFeed (e.Socket);
			_TelnetServer.SendData ("--------------------------------------------------------------------------=====", e.Socket);
			_TelnetServer.SendLineFeed (e.Socket);
			_TelnetServer.SendData (" ADD [some word]       - Add the word [some word] to the list", e.Socket);
			_TelnetServer.SendLineFeed (e.Socket);
			_TelnetServer.SendData (" ALPHA [some text]     - Generate an alphagram of [some text]", e.Socket);
			_TelnetServer.SendLineFeed(e.Socket);
			_TelnetServer.SendData (" COUNT                 - Show the number of words in the list", e.Socket);
			_TelnetServer.SendLineFeed(e.Socket);
			_TelnetServer.SendData (" EXIT                  - Close the telnet session", e.Socket);
			_TelnetServer.SendLineFeed (e.Socket);
			_TelnetServer.SendData (" HELP                  - You're lookin' at it", e.Socket);
			_TelnetServer.SendLineFeed (e.Socket);
			_TelnetServer.SendData (" INFO                  - Some list statistics", e.Socket);
			_TelnetServer.SendLineFeed (e.Socket);
			_TelnetServer.SendData (" LIST [some pattern]   - List words in the list ('LIST ALL' for all words)", e.Socket);
			_TelnetServer.SendLineFeed (e.Socket);
			_TelnetServer.SendData ("                         Where <some pattern> is a pattern to list", e.Socket);
			_TelnetServer.SendLineFeed (e.Socket);
			_TelnetServer.SendData ("                         Wildcards are allowed (i.e. '*tion' or 'a*' or '*ire*'", e.Socket);
			_TelnetServer.SendLineFeed (e.Socket);
			_TelnetServer.SendData (" LOAD [some file]      - Load a file of CR separated words into the word list", e.Socket);
			_TelnetServer.SendLineFeed (e.Socket);
			_TelnetServer.SendData ("                         The default file is 'Scrabble.txt'", e.Socket);
			_TelnetServer.SendLineFeed (e.Socket);
			_TelnetServer.SendData (" QUIT                  - Close the telnet session and halt the application", e.Socket);
			_TelnetServer.SendLineFeed (e.Socket);
			_TelnetServer.SendData (" LOAD [some file]      - Load a file of CR separated words into the word list", e.Socket);
			_TelnetServer.SendLineFeed (e.Socket);
			_TelnetServer.SendData(" SEARCH [some word]    - Search the list of words for a word", e.Socket);
			_TelnetServer.SendLineFeed(e.Socket);
			_TelnetServer.SendData(" SEARCHALPHA [word]    - Search the list of words for a word by Alphagram", e.Socket);
			_TelnetServer.SendLineFeed(e.Socket);
			_TelnetServer.SendData(" WORD [number]         - Display the word at index # [number]", e.Socket);
			_TelnetServer.SendLineFeed (e.Socket);
			_TelnetServer.SendData ("==========================================================================-----", e.Socket);
			_TelnetServer.SendLineFeed (e.Socket);
		}

		private void SendTelnetAlphaWordList (TelnetServerCommandEventArgs pEventArgs, string pSearchString)
		{
			bool tmpListingCompleted = false;

			int tmpMatchCount = 0;

			MutiTimeSpan tmpSearchTimer = new MutiTimeSpan();

			if (_WordList.Count > 0)
			{
				_TelnetServer.SendData (" Word List:", pEventArgs.Socket);
				_TelnetServer.SendLineFeed (pEventArgs.Socket);
				_TelnetServer.SendData ("--- Number - Word ------------------------------- Alphagram -------------=====", pEventArgs.Socket);
				_TelnetServer.SendLineFeed (pEventArgs.Socket);

				tmpListingCompleted = !_WordList.FindFirstByAlphagram(pSearchString);

				while (!tmpListingCompleted)
				{
					//This display function is assuming that the words are no longer than 28 characters which is safe for scrabble words
					_TelnetServer.SendData (String.Format(" {0,8}.   {1,-32}     {2}", _WordList.CurrentItemKey ,_WordList.CurrentWord, _WordList.CurrentWordAlphagram), pEventArgs.Socket);
					_TelnetServer.SendLineFeed (pEventArgs.Socket);
					tmpMatchCount++;

					//Bail out of the listing if it's the end of the list
					tmpListingCompleted = _WordList.EOL;

					//This is to not display the last node if it isn't a match and we hit the EOL
					tmpListingCompleted = !_WordList.FindNextByAlphagram (pSearchString);
				}

				_TelnetServer.SendData ("==========================================================================-----", pEventArgs.Socket);
				_TelnetServer.SendLineFeed (pEventArgs.Socket);
				_TelnetServer.SendData (tmpMatchCount.ToString()+" Word(s) Matched the Pattern "+pSearchString, pEventArgs.Socket);
				_TelnetServer.SendLineFeed (pEventArgs.Socket);

				tmpSearchTimer.TimeStamp();

				_TelnetServer.SendData ("Effective Time To Search: "+tmpSearchTimer.TimeDifference.ToString()+"ms", pEventArgs.Socket);
				_TelnetServer.SendLineFeed (pEventArgs.Socket);
			}
			else
			{
				_TelnetServer.SendData ("The word list is empty.", pEventArgs.Socket);
				_TelnetServer.SendLineFeed (pEventArgs.Socket);
			}
		}

		private void SendTelnetWordList (TelnetServerCommandEventArgs pEventArgs, string pSearchText)
		{
			bool tmpSearchAll = false;
			bool tmpDoneListing = false;

			int tmpMatchCount = 0;

			MutiTimeSpan tmpSearchTimer = new MutiTimeSpan();

			if (pSearchText.ToUpper() == "ALL")
			{
				tmpSearchAll = true;
			}

			if (_WordList.Count > 0)
			{
				_TelnetServer.SendData (" Word List:", pEventArgs.Socket);
				_TelnetServer.SendLineFeed (pEventArgs.Socket);
				_TelnetServer.SendData ("--- Number - Word ------------------------------- Alphagram -------------=====", pEventArgs.Socket);
				_TelnetServer.SendLineFeed (pEventArgs.Socket);

				if (tmpSearchAll)
				{
					_WordList.MoveFirst();
				}
				else
				{
					tmpDoneListing = !_WordList.FindFirstByWord (pSearchText);
				}

				while (!tmpDoneListing)
				{
					//This display function is assuming that the words are no longer than 28 characters which is safe for scrabble words
					_TelnetServer.SendData (String.Format(" {0,8}.   {1,-32}     {2}", _WordList.CurrentItemKey ,_WordList.CurrentWord, _WordList.CurrentWordAlphagram), pEventArgs.Socket);
					_TelnetServer.SendLineFeed (pEventArgs.Socket);
					tmpMatchCount++;

					//Bail out of the listing if it's the end of the list
					tmpDoneListing = _WordList.EOL;

					if (tmpSearchAll)
					{
						_WordList.MoveNext();
					}
					else
					{
						//This is to not display the last node if it isn't a match and we hit the EOL
						tmpDoneListing = !_WordList.FindNextByWord (pSearchText);
					}
				}

				_TelnetServer.SendData ("==========================================================================-----", pEventArgs.Socket);
				_TelnetServer.SendLineFeed (pEventArgs.Socket);
				_TelnetServer.SendData (tmpMatchCount.ToString()+" Word(s) Matched the Pattern "+pSearchText, pEventArgs.Socket);
				_TelnetServer.SendLineFeed (pEventArgs.Socket);

				tmpSearchTimer.TimeStamp();

				_TelnetServer.SendData ("Effective Time To Search: "+tmpSearchTimer.TimeDifference.ToString()+"ms", pEventArgs.Socket);
				_TelnetServer.SendLineFeed (pEventArgs.Socket);
			}
			else
			{
				_TelnetServer.SendData ("The word list is empty.", pEventArgs.Socket);
				_TelnetServer.SendLineFeed (pEventArgs.Socket);
			}
		}
		#endregion

		#region Log File Custom Interfaces
		private void ChainedTelnetWriteLog(object pSender, WriteLogEventArgs pEventArgs)
		{
			//This shim adds the prefix "Telnet: " to before firing the chained log event
			WriteLogEventArgs tmpPrependedLogText = new WriteLogEventArgs ("Telnet: "+pEventArgs.LogText, pEventArgs.LogLevel);
			OnWriteLog(tmpPrependedLogText);

			//Write it to the telnet log file if it is > 0
			if (pEventArgs.LogLevel > 0)
				_TelnetLogFile.WriteLogFile (pEventArgs.LogText);
		}
		#endregion
	}
}
