/**
* This source file is a part of the Dictionariosaur application.
* For full copyright and license information, please view the LICENSE file
* which should be distributed with this source code.
*
* @license MIT License
* @copyright Copyright (c) 2013, Steven Velozo
*
* Contained Classes:
*   WordList (public)
*   WordNode (private linked list node)
*/
using System;
using System.IO;
using System.Text.RegularExpressions;
using MutiDataStructures;

namespace MutiWordList
{
	/// <summary>
	/// A linked list of words with rapid searches and anagram building.
	/// 
	/// Also a good test of inheritance in Mono and (possibly?) interfaces.
	/// </summary>
	public class WordList : LinkedList
	{
		/// <summary>
		/// Load a file that is a list of words separated by carriage returns.
		/// </summary>
		/// <param name="pFileName">The File path and name that will be loaded.</param>
		/// <returns></returns>
		public bool LoadWordList (string pFileName)
		{
			try
			{
				string tmpInputLine = "";
				StreamReader tmpFileReader = new StreamReader (pFileName);
				while (tmpInputLine != null)
				{
					tmpInputLine = tmpFileReader.ReadLine();
					
					if (tmpInputLine != null)
						this.AddWord (tmpInputLine.Trim());
				}
				
				tmpFileReader.Close();
				
				return true;
			}
			catch
			{
				return false;
			}
		}
		
		public bool AddWord (string pWord)
		{
			WordData tmpWordData;
			tmpWordData = new WordData (pWord);
			this.AddNodeToList(tmpWordData);
			return true;
		}
		
		#region Custom Search Methods
		public bool FindFirstByWord (string pWord)
		{
			WordMatch tmpSearchCriteria = new WordMatch (pWord);
			
			if (this.LogAllSearches)
				this.WriteToLog("Find Word: " + tmpSearchCriteria.Word);
			
			return this.FindFirst(tmpSearchCriteria);
		}

		public bool FindNextByWord (string pWord)
		{
			WordMatch tmpSearchCriteria = new WordMatch (pWord);
			
			if (this.LogAllSearches)
				this.WriteToLog("Find Next Word: " + tmpSearchCriteria.Word);
			
			return this.FindNext(tmpSearchCriteria);
		}
		
		public bool FindFirstByAlphagram (string pWord)
		{
			AlphagramMatch tmpSearchCriteria = new AlphagramMatch (pWord);

			if (this.LogAllSearches)
				this.WriteToLog("Find Alphagram: " + tmpSearchCriteria.Alphagram + " (" + tmpSearchCriteria.Word + ")");
			
			return this.FindFirst(tmpSearchCriteria);
		}
					
		public bool FindNextByAlphagram (string pWord)
		{
			AlphagramMatch tmpSearchCriteria = new AlphagramMatch (pWord);
			
			if (this.LogAllSearches)
				this.WriteToLog("Find Alphagram: " + tmpSearchCriteria.Alphagram + " (" + tmpSearchCriteria.Word + ")");
			
			return this.FindNext(tmpSearchCriteria);
		}
		#endregion				
		
		#region Data Access Functions
		public string CurrentWord
		{
			get
			{
				if (this.Count > 0)
					return (CurrentNode.Data as WordData).Word;
				else
					return string.Empty;
			}
		}
		
		public string CurrentWordAlphagram
		{
			get
			{
				if (this.Count > 0)
					return (CurrentNode.Data as WordData).AlphagramWord;
				else
					return "";
			}
		}
		#endregion
	}
	
	/// <summary>
	/// The textual word comparison
	/// </summary>
	public class WordMatch : NodeMatch
	{
		private string _Word;
		
		private Regex _MatchFunction;
		
		public WordMatch (string pWord)
		{
			//Our words are upper case and have no spaces
			this._Word = pWord.Trim().ToUpper();
			
			//Now build the regular expression out of it
			this._MatchFunction = new Regex("\\b"+this._Word.Replace("*", ".*")+"\\b");
		}

		#region Data Access Functions
		public string Word
		{
			get { return this._Word; }
		}
		#endregion
		
		public override bool Match (DataNode pDataNode)
		{
			//See if it matches.  This is extremely straightforward.
			if (this._MatchFunction.IsMatch((pDataNode.Data as WordData).Word.ToString()))
				return true;
			else
				return false;
		}
	}

	/// <summary>
	/// The alphagram word comparison
	/// </summary>
	public class AlphagramMatch : NodeMatch
	{
		private string _Word;

		//The alphagram we are matching, and it's contents as a byte array
		private string _Alphagram;
		private Byte[] _AlphagramByteArray;
		
		public AlphagramMatch (string pWord)
		{
			//Our words are upper case and have no spaces
			this._Word = pWord.Trim().ToUpper();
			
			//Now build the alphagram
			this._Alphagram = this.GenerateAlphagram (this._Word);

			//Create the internal match byte array
			//This would seem like something that should be encapsulated in the class
			this._AlphagramByteArray = new Byte[this._Alphagram.Length];
			_AlphagramByteArray = System.Text.Encoding.ASCII.GetBytes (this._Alphagram);
		}

		#region Data Access Functions
		public string Word
		{
			get { return this._Word; }
		}
		
		public string Alphagram
		{
			get { return this._Alphagram; }
		}
		#endregion
		
		public override bool Match (DataNode pDataNode)
		{
			//This has been abstracted because the typecast was slowing things down a few ms
			return this.CompareStrings ((pDataNode.Data as WordData).Word.ToString());
		}
		
		private bool CompareStrings (string pWord)
		{
			//See if it matches.
			bool tmpMatch = true;
			
			//How far ahead of the comparison array the alphagram array is
			int tmpAlphagramArrayOffset = 0;

			try
			{
				//Create the comparison word's alphagram byte array
				Byte[] tmpComparisonArray = new Byte[pWord.Length];
				tmpComparisonArray = System.Text.Encoding.ASCII.GetBytes (this.GenerateAlphagram (pWord));
				
				if (tmpComparisonArray.Length > _AlphagramByteArray.Length)
				{
					//More characters in the comparator mean it's impossible to be an alphagram
					tmpMatch = false;
				}
				else
				{
					//Now walk through the comparison word array and make sure that there are at least as many of the characters in one as the other
					for (int tmpCounter = 0; (tmpCounter < tmpComparisonArray.Length) && tmpMatch; tmpCounter++)
					{
						//This will get a little bit tricky because we need to have an offset to skip extra characters in the alphagrambytearray
						
						//Incriment the alphagram offset until it either matches the character, overruns it or runs out of letters to check
						while (((tmpCounter + tmpAlphagramArrayOffset) < _AlphagramByteArray.Length) &&
						     	(_AlphagramByteArray[tmpCounter + tmpAlphagramArrayOffset] < tmpComparisonArray[tmpCounter]))
							//Incriment the offset
							tmpAlphagramArrayOffset++;
						
						//If we've run out of letters or the alphagram and the currentword don't match, we're SOL
						if (((tmpCounter + tmpAlphagramArrayOffset) >= _AlphagramByteArray.Length) || 
							 (_AlphagramByteArray[tmpCounter + tmpAlphagramArrayOffset] != tmpComparisonArray[tmpCounter]))
							tmpMatch = false;							
					}
				}
			}
			catch
			{
				//Ruh roh.  Problems!
				tmpMatch = false;
			}
			
			return tmpMatch;
		}

		#region String Functions
		private string GenerateAlphagram (string pWord)
		{
			// The string we'll return
			System.Text.StringBuilder tmpSortedString;
			
			tmpSortedString = new System.Text.StringBuilder();
			
			// The byte array we'll use to sort
			Array tmpStringByteArray = Array.CreateInstance(typeof(Byte), pWord.Length);
			
			// Now we need to assign FromString to the array
			tmpStringByteArray = System.Text.Encoding.ASCII.GetBytes (pWord);
			
			// Now sort it (good thing bytes are icomparible)
			Array.Sort(tmpStringByteArray);
			
			// Now convert it back to a unicode string.
			foreach (byte tmpByte in tmpStringByteArray)
				tmpSortedString.Append(Convert.ToChar(tmpByte));
			
			return tmpSortedString.ToString().Trim();
		}
		#endregion		
	}

	/// <summary>
	/// The little Word Data packet that we stuff in each list node
	/// </summary>
	public class WordData
	{
		private string _Word;
		private string _Alphagram;
		
		public WordData (string pWord)
		{
			this.Word = pWord;
		}
		
		public string Word
		{
			get { return this._Word; }
			set
			{
				//Set the word and then get the anagram word
				this._Word = value;
				
				this._Alphagram = GenerateAlphagram (this._Word);
			}
		}
		
		public string AlphagramWord
		{
			get { return this._Alphagram; }
		}
		
		#region String Functions
		private string GenerateAlphagram (string pWord)
		{
			// The string we'll return
			System.Text.StringBuilder SortedString;
			
			SortedString = new System.Text.StringBuilder();
			
			// The byte array we'll use to sort
			Array StringByteArray = Array.CreateInstance(typeof(Byte), pWord.Length);
			
			// Now we need to assign FromString to the array
			StringByteArray = System.Text.Encoding.ASCII.GetBytes (pWord);
			
			// Now sort it (good thing bytes are icomparible)
			Array.Sort(StringByteArray);
			
			// Now convert it back to a unicode string.
			foreach (byte tmpByte in StringByteArray)
				SortedString.Append(Convert.ToChar(tmpByte));
			
			return SortedString.ToString().Trim();
		}
		#endregion		
	}
}
