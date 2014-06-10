/**
* This source file is a part of the Dictionariosaur application.
* For full copyright and license information, please view the LICENSE file
* which should be distributed with this source code.
*
* @license MIT License
* @copyright Copyright (c) 2013, Steven Velozo
*
* A base class for all List memory data structures.
* 
* Prototype and logging information that would be considered "standard" will start
* here.
* 
* This should allow us to reuse comparators for multiple data structures.
* 
* TODO: Consider putting saving and loading crap into this
*/
using System;
using MutiUtility;

namespace MutiDataStructures
{
	public class DataStructureBase : PassThroughLoggedClass
	{
		//This is so we can override the current node business
		protected DataNode _CurrentNode;

		//The overridable property
		public virtual DataNode CurrentNode
		{
			get { return this._CurrentNode; }
			set {/*Nothing now, but we want to be able to override it. */}
		}

		public long CurrentItemKey
		{
			get
			{
				if (this._NodeCount > 0)
					return this.CurrentNode.Index;
				else
					return 0;
			}
		}
		
		// The number of items in the data structure
		protected long _NodeCount;
		
		//Default statistics for a data structure
		protected long _SearchCount;			//Number of searches
		protected long _SearchMatchCount;		//Number of matches resulting from searches
		protected long _NavigationCount;		//Number of navigation calls made (movefirst, etc)
		//Extended statistics
		protected long _AddCount;				//Number of inserts
		protected long _DeleteCount;				//Number of deletes
		
		//Logging options
		protected bool _LogSearches;				//Time and log each search
		protected bool _LogNavigation;			//Log each navigation function (WAY HUGE log files)
		//Extended logging options
		protected bool _LogAdds;				//Log each add (can make log files HUGE)
		protected bool _LogDeletes;				//Log each delete operation
		

		// The Auto Incriment key
		protected long _LastGivenKey;

		public DataStructureBase ()
		{
			//Initialize the list count
			this._NodeCount = 0;

			//Reset the statistics
			this._SearchCount = 0;
			this._SearchMatchCount = 0;
			this._NavigationCount = 0;
			this._AddCount = 0;
			this._DeleteCount = 0;
			
			this._LogSearches = false;
			this._LogNavigation = false;
			this._LogAdds = false;
			this._LogDeletes = false;
			
			//The list is empty.  Zero everything out!
			this._LastGivenKey = 0;
		}
		
		#region Data Access Functions
		/// <summary>
		/// The list count
		/// </summary>
		public long Count
		{
			get { return this._NodeCount; }
		}
		
		public long StatisticSearchCount
		{
			get { return this._SearchCount; }
		}
		
		public long StatisticSearchMatchCount
		{
			get { return this._SearchMatchCount; }
		}
		
		public long StatisticNavigationCount
		{
			get { return this._NavigationCount; }
		}
		public long StatisticAddCount
		{
			get { return this._AddCount; }
		}
		
		public long StatisticDeleteCount
		{
			get { return this._DeleteCount; }
		}
		
		
		public bool LogAllSearches
		{
			get { return this._LogSearches; }
			set {this._LogSearches = value; }
		}
		
		public bool LogAllNavigation
		{
			get { return this._LogNavigation; }
			set {this._LogNavigation = value; }
		}
		
		public bool LogAllAdds
		{
			get { return this._LogAdds; }
			set {this._LogAdds = value; }
		}
		
		public bool LogAllDeletes
		{
			get { return this._LogDeletes; }
			set {this._LogDeletes = value; }
		}
		#endregion
	}

	public class DataNode
    {
        //The primary key
        private long _Index;
        
        //The data gram
        private object _Data;
        
        //The constructor
        public DataNode (object pNewDataGram)
        {
        	this._Data = pNewDataGram;
        }

        #region Data Access Functions
        /// <summary>
        /// The primary key for the list node.
        /// </summary>
        public long Index
        {
        	get { return this._Index; }
        	set {this._Index = value; }
        }
        
        public object Data
        {
        	get { return this._Data; }
        	set {this._Data = value; }
        }
        #endregion
    }

		
	public class NodeMatch
	{
		//The key to match
		private long _Index;
		
		public NodeMatch ()
		{
			//Do nothing
			this._Index = 0;
		}
		
		public NodeMatch (long pIndexToFind)
		{
			this._Index = pIndexToFind;
		}

		/// <summary>
		/// Compare two nodes.
		/// Return -1 for Left < Right
		/// Return 0 for  Left = Right
		/// Return 1 for  Left > Right
		/// </summary>
		/// <param name="LeftNodeValue">Left Comparator</param>
		/// <param name="RightNodeValue">Right Comparator</param>
		/// <returns>-1 for L less than R, 0 for L equal to R, 1 for L greater than R</returns>
		public virtual int Compare (DataNode pLeftNodeValue, DataNode pRightNodeValue)
		{
			if (pLeftNodeValue.Index < pRightNodeValue.Index)
				return -1;
			else if (pLeftNodeValue.Index == pRightNodeValue.Index)
				return 0;
			else
				return 1;
		}
		
		
		public virtual bool Match (DataNode pNodeToVerify)
		{
			//See if it matches.  This is extremely straightforward.
			if (pNodeToVerify.Index == this._Index)
				return true;
			else
				return false;
		}
	}
}
