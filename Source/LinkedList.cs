/**
* This source file is a part of the Dictionariosaur application.
* For full copyright and license information, please view the LICENSE file
* which should be distributed with this source code.
*
* @license MIT License
* @copyright Copyright (c) 2013, Steven Velozo
*
* A derivable linked list implimented to test out inheritance and speed theirin.
* 
* TODO: Thread safe iterators
* TODO: Auto Sorted lists by key, may require comparison stuff in case search doesn't use Sorted parameter
* TODO: Dynamic searching methods, taking into account list sort status
*/
using System;
using MutiUtility;

namespace MutiDataStructures
{
	public class LinkedListBase : DataStructureBase
	{
		// The list head and tail
		protected ListNode ListHead;
		protected ListNode ListTail;

		#region Data Access Functions
		public bool EOL
		{
			get
			{
				if ((this._NodeCount > 0) && (this.CurrentNode.Index == this.ListTail.Index))
					return true;
				else
					return false;
			}
		}				
		#endregion
		
		#region Linked List Navigation Functions
		public bool MoveFirst ()
		{
			try
			{
				if (_NodeCount > 0)
				{
					//Set the current node to the list head
					this.CurrentNode = this.ListHead;

					//Update the statistics
					this._NavigationCount++;
					
					if (this._LogNavigation)
						this.WriteToLog ("Nav: [Move First] in "+this.Count.ToString()+" items");
					
					return true;
				}
				else
				{
					return false;
				}
			}
			catch
			{
				//Eventually we may want to set up a log file chain for errors
				return false;
			}
		}

		public bool MovePrevious ()
		{
			try
			{
				if ((_NodeCount > 0) && (this.CurrentNode.Index != this.ListHead.Index))
				{
					//Set the current node to the previous node
					this.CurrentNode = (this.CurrentNode as ListNode).PreviousNode;

					//Update the statistics
					this._NavigationCount++;
					
					if (this._LogNavigation)
						this.WriteToLog ("Nav: [Move Previous] in "+this.Count.ToString()+" items");
					
					return true;
				}
				else
				{
					return false;
				}
			}
			catch
			{
				//Eventually we may want to set up a log file chain for errors
				return false;
			}
		}

		public bool MoveNext ()
		{
			try
			{
				if ((_NodeCount > 0) && (this.CurrentNode.Index != this.ListTail.Index))
				{
					//Set the current node to the next node in the list
					this.CurrentNode = (this.CurrentNode as ListNode).NextNode;

					//Update the statistics
					this._NavigationCount++;
					
					if (this._LogNavigation)
						this.WriteToLog ("Nav: [Move Next] in "+this.Count.ToString()+" items");
					
					return true;
				}
				else
				{
					return false;
				}
			}
			catch
			{
				//Eventually we may want to set up a log file chain for errors
				return false;
			}
		}

		public bool MoveLast ()
		{
			try
			{
				if (_NodeCount > 0)
				{
					//Set the current node to the list tail
					this.CurrentNode = this.ListTail;

					//Update the statistics
					this._NavigationCount++;
					
					if (this._LogNavigation)
						this.WriteToLog ("Nav: [Move Last] in "+this.Count.ToString()+" items");
					
					return true;
				}
				else
				{
					return false;
				}
			}
			catch
			{
				//Eventually we may want to set up a log file chain for errors
				return false;
			}
		}
		#endregion
		
		#region Linked List Search Functions
		/// <summary>
		/// A linear search
		/// </summary>
		/// <param name="KeyToSearchFor">The index # to find in the list</param>
		/// <returns></returns>
		public bool FindFirstByIndex (long IndexToSearchFor)
		{
			try
			{
				//Create the default match criteron (a key)
				NodeMatch FindKey = new NodeMatch (IndexToSearchFor);
				//Now find it
				return this.FindFirst (FindKey);
			}
			catch
			{
				this.WriteToLog ("Error finding key " + IndexToSearchFor.ToString());
				return false;
			}
		}
		
		public bool FindFirst (NodeMatch MatchingMethod)
		{
			try
			{
				return this.LinearSearchForwardFromBeginning (MatchingMethod);
			}
			catch
			{
				this.WriteToLog ("Error finding first item");
				return false;
			}			
		}
		
		public bool FindNext (NodeMatch MatchingMethod)
		{
			try
			{
				if ((this._NodeCount > 1) && (!this.EOL))
				{
					this.MoveNext();
					return this.LinearSearchForward(MatchingMethod);
				}
				else
				{
					return false;
				}
			}
			catch
			{
				this.WriteToLog ("Error finding next item");
				return false;
			}			
		}

		/// <summary>
		/// A forward linear search from the beginning of the list
		/// </summary>
		/// <param name="MatchingMethod">A derivative of NodeMatch that returns Match() as true on hits</param>
		/// <returns>True if anything was found</returns>
		public bool LinearSearchForwardFromBeginning (NodeMatch MatchingMethod)
		{
			bool MatchedItem = false;

			try
			{
				if (_NodeCount > 0)
				{
					//Update the statistics
					this._SearchCount++;

					//First see if we are already at the matching node
					if (MatchingMethod.Match(this.CurrentNode))
					{
						this._SearchMatchCount++;
						MatchedItem = true;
					}
					else
					{
						//Set the current node to the list head
						this.MoveFirst();
						//Now search forward and see if there are any hits
						MatchedItem = this.LinearSearchForward (MatchingMethod);
					}
				}
			}
			catch
			{
				this.WriteToLog("Error searching list of ("+this.Count.ToString()+") items");
			}

			return MatchedItem;
		}
		
		public bool LinearSearchForward (NodeMatch MatchingMethod)
		{
			bool MatchedItem = false;
			MutiTimeSpan SearchTimer = new MutiTimeSpan();
			
			if (this._LogSearches)
			{
				SearchTimer.Start();
			}

			try
			{
				if (this.Count > 0)
				{
					//Now walk through the list until we either find it or hit the end.
					while ((!MatchingMethod.Match(this.CurrentNode)) && (!this.EOL))
						this.MoveNext();
					
					//See if we found it or not
					if (MatchingMethod.Match(this.CurrentNode))
					{
						this._SearchMatchCount++;
						MatchedItem = true;
					}
				}
			}
			catch
			{
				this.WriteToLog("Error during forward linear searching list of ("+this.Count.ToString()+") items");				
			}
			finally
			{
				if (this._LogSearches)
				{
					SearchTimer.TimeStamp();
					if (MatchedItem)
						this.WriteToLog("Searched Forward in a list of ("+this.Count.ToString()+") items and found item #"+this.CurrentItemKey.ToString()+" taking "+SearchTimer.TimeDifference.ToString()+"ms.");
					else
						this.WriteToLog("Searched Forward in a list of ("+this.Count.ToString()+") items and did not get a match taking "+SearchTimer.TimeDifference.ToString()+"ms.");
				}
			}
			
			return MatchedItem;
		}
		#endregion
	}
	
	/// <summary>
	/// A linked list class; this will be inherited obviously.
	/// </summary>
	public class LinkedList : LinkedListBase
	{
		/// <summary>
		/// The current node.
		/// In the full list it is writable and readable.
		/// </summary>
		public override DataNode CurrentNode
		{
			get { return this._CurrentNode; }
			set {this._CurrentNode = value; }
		}		
		
		// The class initializer
		public LinkedList():base()
		{
			//Nothing needs to be done for a base linked list
		}
		
		#region Linked List Insert Functions
		/// <summary>
		/// Add a node to the linked list.  This will in most cases stay the way it is.  
		/// It does magic number generation and stuff.
		/// </summary>
		/// <param name="NodeDataGram">An object to be the datagram in the node</param>
		protected void AddNodeToList (object NodeDataGram)
		{
			// TODO: Later the add method style should be dynamic
			//       i.e. Beginning would add new nodes to the top, End would be the bottom, 
			//            BeforeCurrent before the CurrentNode, AfterCurrent after
			ListNode NodeToAdd;
			
			try
			{			
				NodeToAdd = new ListNode(NodeDataGram);
				
				if (this.Count > 0)
					//Add it to the end of the list
					this.AddNodeAfter (NodeToAdd, this.ListTail);
				else
					//Add it to an empty list
					this.AddFirstNode (NodeToAdd);
				
				this._NodeCount++;
				
				// We handle the key on inserts, as well.
				this._LastGivenKey++;
				NodeToAdd.Index = this._LastGivenKey;
				
				// Update the statistics
				this._AddCount++;			
			}
			catch
			{
				this.WriteToLog("Error adding a node to the list during the decision tree phase.");
			}
		}
		
		/// <summary>
		/// Add a node to an empty list
		/// </summary>
		/// <param name="NodeToAdd">The node to add to the list</param>
		protected void AddFirstNode (ListNode NodeToAdd)
		{
			try
			{
				// Add a node to the list of no nodes.
				if (this.Count == 0)
				{
					this.ListHead = NodeToAdd;
					this.CurrentNode = NodeToAdd;
					this.ListTail = NodeToAdd;
					if (this._LogAdds)
						this.WriteToLog("Added node # "+NodeToAdd.Index.ToString()+" successfully to the empty list.");
				}
			}
			catch
			{
				this.WriteToLog("Error adding the first node to the list.");
			}
		}
		
		/// <summary>
		/// Add a node after the node ReferencePoint
		/// </summary>
		/// <param name="NodeToAdd">The node to add to the list</param>
		/// <param name="ReferencePoint">The node after which this node is to come</param>
		protected void AddNodeAfter (ListNode NodeToAdd, ListNode ReferencePoint)
		{
			try
			{
				if (ReferencePoint.Index != this.ListTail.Index)
				{
					//This is being inserted after a normal node
					NodeToAdd.PreviousNode = ReferencePoint;
					NodeToAdd.NextNode = ReferencePoint.NextNode;
					
					ReferencePoint.NextNode = NodeToAdd;
					
					NodeToAdd.NextNode.PreviousNode = NodeToAdd;
				}
				else
				{
					//This is coming after the list tail
					NodeToAdd.PreviousNode = ReferencePoint;
					
					ReferencePoint.NextNode = NodeToAdd;
					
					this.ListTail = NodeToAdd;
				}

				if (this._LogAdds)
				{
					this.WriteToLog("Added node # "+NodeToAdd.Index.ToString()+" after node # "+ReferencePoint.Index.ToString()+".");
				}
			}
			catch
			{
				this.WriteToLog("Error adding node #"+NodeToAdd.Index+" after Node #"+ReferencePoint.Index+".");
			}
		}
		
		/// <summary>
		/// Add a node before the node ReferencePoint
		/// </summary>
		/// <param name="NodeToAdd">The node to add to the list</param>
		/// <param name="ReferencePoint">The node before which this node is to come</param>
		protected void AddNodeBefore (ListNode NodeToAdd, ListNode ReferencePoint)
		{
			try
			{
				if (ReferencePoint.Index != this.ListHead.Index)
				{
					//This is being inserted after a normal node
					NodeToAdd.NextNode = ReferencePoint;
					NodeToAdd.PreviousNode = ReferencePoint.PreviousNode;
					
					ReferencePoint.PreviousNode = NodeToAdd;
					
					NodeToAdd.PreviousNode.NextNode = NodeToAdd;
				}
				else
				{
					//This is coming before the list head
					NodeToAdd.NextNode = ReferencePoint;
					
					ReferencePoint.PreviousNode = NodeToAdd;
					
					this.ListHead = NodeToAdd;
				}

				if (this._LogAdds)
					this.WriteToLog("Added node # "+NodeToAdd.Index.ToString()+" before node # "+ReferencePoint.Index.ToString()+".");
			}
			catch
			{
				this.WriteToLog("Error adding node #"+NodeToAdd.Index+" after Node #"+ReferencePoint.Index+".");
			}
		}
		#endregion		
	}

	/// <summary>
	/// The linked list node.
	/// </summary>
	public class ListNode : DataNode
    {
		//The next and previous node
        private ListNode NextListNode;
        private ListNode PreviousListNode;

        public ListNode (object NewDataGram):base(NewDataGram)
        {
        	//This is just a pass-through
        }
        
        #region List Node Data Access Functions
        public ListNode NextNode
        {
        	get { return this.NextListNode; }
			set {this.NextListNode = value; }
        }
        
        public ListNode PreviousNode
        {
        	get { return this.PreviousListNode; }
        	set {this.PreviousListNode = value; }
        }
        #endregion
    }
}
