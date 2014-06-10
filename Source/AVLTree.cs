/**
* This source file is a part of the Dictionariosaur application.
* For full copyright and license information, please view the LICENSE file
* which should be distributed with this source code.
*
* @license MIT License
* @copyright Copyright (c) 2013, Steven Velozo
*
* A derivable self-balancing AVL tree class.
* During instantiation it requires a (read-only) comparator
* 
* TODO: Thread safe iterators
* TODO: Auto Sorted Trees by key, may require comparison stuff in case search doesn't use Sorted parameter
*/
using System;
using MutiUtility;

namespace MutiDataStructures
{
	public class AVLTreeBase : DataStructureBase
	{
		// The Tree head
		protected TreeNode TreeHead;

		// The Leftmost and Rightmost Branches
		protected TreeNode TreeLeftMostBranch;
		protected TreeNode TreeRightMostBranch;
		
		protected NodeMatch DefaultMatchMethod;
		
		/// <summary>
		/// This is the comparator (readonly from creation) that is used to build the tree.
		/// 
		/// It CAN NOT be changed once the tree has items added to it.
		/// </summary>
		public virtual NodeMatch TreeComparator
		{
			get { return this.DefaultMatchMethod; }
		}
		
		public AVLTreeBase ():base()
		{
			this.DefaultMatchMethod = new NodeMatch();
		}
		

		#region Data Access Functions
		public bool EOL
		{
			get
			{
				if ((this._NodeCount > 0) && (this.CurrentNode.Index == this.TreeRightMostBranch.Index))
					return true;
				else
					return false;
			}
		}				
		#endregion
		
		#region AVL Tree Navigation Functions
		public bool MoveFirst ()
		{
			try
			{
				if (_NodeCount > 0)
				{
					//Set the current node to the Tree head
					this.CurrentNode = this.TreeLeftMostBranch;
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
				if ((_NodeCount > 0) && (this.CurrentNode.Index != this.TreeLeftMostBranch.Index))
				{
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
				if ((_NodeCount > 0) && (this.CurrentNode.Index != this.TreeRightMostBranch.Index))
				{
					//Screwy AVL move next logic here.
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
					//Set the current node to the Tree tail
					this.CurrentNode = this.TreeRightMostBranch;
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
	}
	
	/// <summary>
	/// A AVL Tree class; this will be inherited obviously.
	/// </summary>
	public class AVLTree : AVLTreeBase
	{
		/// <summary>
		/// The current node.
		/// In the full Tree it is writable and readable.
		/// </summary>
		public override DataNode CurrentNode
		{
			get { return this._CurrentNode; }
			set {this._CurrentNode = value; }
		}		
		
		#region AVL Tree Insert Functions
		/// <summary>
		/// Add a node to the AVL Tree.  This will in most cases stay the way it is.  
		/// It does magic number generation and stuff.
		/// </summary>
		/// <param name="pData">An object to be the datagram in the node</param>
		public void AddNodeToTree (object pData)
		{
			TreeNode tmpNode;
			
			try
			{			
				this.WriteToLog("Adding node with datagram ["+pData.ToString()+"].");

				tmpNode = new TreeNode(pData);
				
				// We handle the key on inserts, as well.
				this._LastGivenKey++;

				tmpNode.Index = this._LastGivenKey;
				
				if (this.Count > 0)
					//Add it to the end or middle of the Tree
					this.AddNode (tmpNode);
				else
					//Add it to an empty Tree
					this.AddFirstNode (tmpNode);

				// Update the statistics
				this._AddCount++;			
			}
			catch
			{
				this.WriteToLog("Error adding a node to the Tree during the decision tree phase.");
			}
		}
		
		protected void AddNode (TreeNode pNode)
		{
			try
			{
				if (this.FindFirst (pNode, true))
					//There is a node that is exactly the same.  Insert over the current node
					this.AddNodeInsertion (pNode);
				else
					//Add it normally
					this.AddNodeBottom (pNode);
				if (this._LogAdds)
					this.WriteToLog("Added node # "+pNode.Index.ToString()+" successfully to the tree previously holding "+this.Count.ToString()+" items.");
			}
			catch
			{
				this.WriteToLog("Error adding a node to the tree of "+this.Count.ToString()+" items.");
			}
		}
		
		/// <summary>
		/// Add a node to an empty Tree
		/// </summary>
		/// <param name="pNode">The node to add to the Tree</param>
		protected void AddFirstNode (TreeNode pNode)
		{
			try
			{
				// Add a node to the Tree of no nodes.
				if (this.Count == 0)
				{
					this.TreeHead = pNode;
					this.TreeLeftMostBranch = pNode;
					this.TreeRightMostBranch = pNode;
					this.CurrentNode = pNode;
    				this._NodeCount++;
 
					if (this._LogAdds)
						this.WriteToLog("Added node # "+pNode.Index.ToString()+" successfully to the empty Tree.");
				}
			}
			catch
			{
				this.WriteToLog("Error adding the first node to the Tree.");
			}
		}
		
		protected void AddNodeInsertion (TreeNode pNode)
		{
			TreeNode tmpPointTwo;
			try
			{
				tmpPointTwo = (this.CurrentNode as TreeNode);
				/*
				 * Diagram of an insertion (add at current node shifting the branch down)
				 * This is used when we add duplicate nodes.  The shift will always be LEFT.
				 * 
				 * 1) Starting with a tree with values 1, 2, 3 (1 will be 1a for simplicity)
				 *        <p>
				 *        /
				 *     [1a]
				 * 
				 * 2) Insert Node value 1b by shifting the current 1a down and replacing it
				 *        <p>
				 *        /
				 *     [1b]
				 *      /
				 *   [1a]
				 * 
				 * Steps involved:
				  * 1: Assign [1a] to [1b]'s Left Branch
				  * 2: Assign [1b]'s Parent to <p> (if <p> exists)
				  * 3: Assign [1a]'s Parent to [1b]
				  * 4: Determine which of <p>'s branches point to [1a] (if <p> exists)
				  * 5: Assign <p>'s Determined Branch to [1b] (if <p> exists)
				  * 
				*/
				//Step 1:
				pNode.LeftBranch = tmpPointTwo;
				
				if (tmpPointTwo.ParentIsSet)
				{
					//Step 2:
					pNode.ParentBranch = tmpPointTwo.ParentBranch;
					
					//Step 3:
					tmpPointTwo.ParentBranch = pNode;
					
					//Step 4:
					if ((pNode.ParentBranch.LeftIsSet) && (pNode.ParentBranch.LeftBranch.Index == tmpPointTwo.Index))
					{
						//Ok so the left branch points to the old node
						//Step 5:				
						pNode.ParentBranch.LeftBranch = pNode;
					}
					else if ((pNode.ParentBranch.RightIsSet) && (pNode.ParentBranch.RightBranch.Index == tmpPointTwo.Index))
					{
						//Ok so the right branch points to the Currently Rotating Set
						//Step 5:				
						pNode.ParentBranch.RightBranch = pNode;
					}
					else
					{
						//Wha happen?!
						//Step 5: RUINEDED
						this.WriteToLog("Error during Insertion Add on node index "+pNode.Index.ToString()+" in a list of "+this.Count.ToString()+" items.");
						this.WriteToLog("  (*) Parent node set with no matching children to reassign");
					}
				}
				else
				{
					//Step 3:
					tmpPointTwo.ParentBranch = pNode;
				}
					
				this._NodeCount++;
				pNode.RefigureHeight();
				//Now balance the new node!
				this.BalanceTreeToRoot(pNode);
				if (this._LogAdds)
					this.WriteToLog("Added node # "+pNode.Index.ToString()+" inserting before node # "+tmpPointTwo.Index.ToString()+".");
			}
			catch
			{
				this.WriteToLog ("Error inserting a node to the tree of "+this.Count.ToString()+" items.");
			}
		}

		protected void AddNodeBottom (TreeNode pNode)
		{
			TreeNode tmpPointTwo;
			String AddLocation = "Nullified";
			
			try
			{
				tmpPointTwo = (this.CurrentNode as TreeNode);
				/*
				 * Diagram of an end-of-the-tree add
				 * 
				 * 1) Starting with a node with an unset side having the correct relationship:
				 *        [5]
				 * 
				 * 2) Insert Node value 2 on the left of 5.
				 *        [5]
				 *        /
				 *      [2]
				 * 
				 * Steps involved:
				  * 1: Assign [5] to [2]'s Parent
				  * 2: Assign [2] to [5]'s proper branch (left in this case)
				  * 
				*/
				
				//Step 1:
				pNode.ParentBranch = tmpPointTwo;
				
				if (this.TreeComparator.Compare(pNode, tmpPointTwo) < 0)
				{
					//It is being added on the left.
					//Step 2:
					tmpPointTwo.LeftBranch = pNode;
					AddLocation = "Left Branch";
				}
				else if (this.TreeComparator.Compare(pNode, tmpPointTwo) > 0)
				{
					//It is being added to the right.
					tmpPointTwo.RightBranch = pNode;
					AddLocation = "Right Branch";
				}
				
				this._NodeCount++;
				//Now balance the new node!
				this.BalanceTreeToRoot(pNode);
				if (this._LogAdds)
					this.WriteToLog("Added node # "+pNode.Index.ToString()+" after node # "+tmpPointTwo.Index.ToString()+" on the "+AddLocation+".");
			}
			catch
			{
				this.WriteToLog ("Error adding a node to the tree of "+this.Count.ToString()+" items.");
			}
		}
		#endregion
		
		#region Tree Leaf Operations
		/// <summary>
		/// Recursively balance all nodes from Starting Point to the top of the tree
		/// </summary>
		/// <param name="pStartingPoint">The point at which to start balancing.</param>
		/// <returns></returns>
		protected bool BalanceTreeToRoot (TreeNode pStartingPoint)
		{
			this.WriteToLog ("Begin Balance To Root for #"+pStartingPoint.Index);
			this.BalanceNode (pStartingPoint);
			
			if (pStartingPoint.ParentIsSet)
				//Hopefully the call stack can handle this.
				//Logging should go here.
				this.BalanceTreeToRoot (pStartingPoint.ParentBranch);
			
			this.WriteToLog ("Leaving for #"+pStartingPoint.Index);
			return true;
		}
		
		/// <summary>
		/// Do the magic of balancing a node that is off balance.
		/// </summary>
		/// <param name="pRotationPoint">The Node which needs to be checked and balanced.</param>
		/// <returns></returns>
		protected bool BalanceNode (TreeNode pRotationPoint)
		{
			this.WriteToLog ("Begin Balance Node for #"+pRotationPoint.Index.ToString()+" bal["+pRotationPoint.Balance.ToString()+"]");
			//This could use some deadlock protection.
			try
			{
				//Investigate Node Height Stuff by adding logging to the routines.
				//Fix rotate left first though!
				//Also, the single rotation bit is creating circular structures.
				while ((pRotationPoint.Balance < -1) || (pRotationPoint.Balance > 1))
				{
					this.WriteToLog ("  Balance for #"+pRotationPoint.Index.ToString()+" is "+pRotationPoint.Balance.ToString());
					if (pRotationPoint.Balance < -1)
					{
						//It is right heavy.  Rotate left.
						this.WriteToLog ("  Rotating Left");
						this.RotateLeft (pRotationPoint);
					}
					else if (pRotationPoint.Balance > 1)
					{
						//It is left heavy.  Rotate right.
						this.WriteToLog ("  Rotating Right");
						this.RotateRight (pRotationPoint);
					}
				}
				
				//Update the statistics
				this._NavigationCount++;
				if (this._LogNavigation)
					this.WriteToLog ("Nav: [Balanced] node#"+pRotationPoint.Index+" in "+this.Count.ToString()+" items"+" bal["+pRotationPoint.Balance.ToString()+"]");
				this.WriteToLog ("Leaving Balance Node for #"+pRotationPoint.Index.ToString()+" bal["+pRotationPoint.Balance.ToString()+"]");
				return true;
			}
			catch
			{
				return false;
			}
		}
		
		/// <summary>
		/// Rotate a node left.
		/// 
		/// Determine if single or double rotation is required.
		/// </summary>
		/// <param name="pRotationPoint">Node to Rotate</param>
		/// <returns>True if Successful</returns>
		protected bool RotateLeft (TreeNode pRotationPoint)
		{
			try
			{
				//Make sure there is a node attached to the Right Branch
				if (pRotationPoint.RightIsSet)
				{
					//See if there is a Left Branch on the Node that is being rotated
					if (pRotationPoint.RightBranch.LeftIsSet)
						//Double Rotation Necessary!
						return this.DoubleLeftRotation (pRotationPoint);
					else
						//We only need a Single Rotation
						return this.SingleLeftRotation (pRotationPoint);
				}
				else
				{
					return false;
				}
			}
			catch
			{
				return false;
			}
		}
		
		protected bool SingleLeftRotation (TreeNode pRotationPoint)
		{
			try
			{
				TreeNode tmpPointTwo;
				/*
				 * Diagram of a single Left Rotation (Rotation point will be the unbalanced point [1]:
				 * 
				 * 1) Starting out with the unbalanced tree at point [1]
				 *      <p>
				 *       |
				 *      [1]
				 *        \
				 *        [2]
				 *          \
				 *          [3]
				 * 
				 * 2) Rotate Node 1 down and replace 2's parent with 1's parent on the same link.
				 *        <p>
				 *         |
				 *        [2]
				 *        / \
				 *      [1] [3]
				 * 
				 * Steps involved:
				  * 1: Assign [1] to [2]'s Left Branch and Unassign [1]'s right branch
				  * 2: Assign [2]'s Parent to <p> (if <p> exists)
				  * 3: Assign [1]'s Parent to [2]
				  * 4: Determine which of <p>'s branches point to [1] (if <p> exists)
				  * 5: Assign <p>'s Determined Branch to [2] (if <p> exists)
				  * 
				*/
				
				tmpPointTwo = pRotationPoint.RightBranch;
				
				//Step 1:
				tmpPointTwo.LeftBranch = pRotationPoint;
				pRotationPoint.RightBranch = null;
				
				//Step 2:
				if (pRotationPoint.ParentIsSet)
				{
					tmpPointTwo.ParentBranch = pRotationPoint.ParentBranch;
				}
				else
				{
					tmpPointTwo.ParentBranch = null;
					this.TreeHead = tmpPointTwo;					
				}
				
				//Step 3:
				pRotationPoint.ParentBranch = tmpPointTwo;
				
				//Step 4:
				if (tmpPointTwo.ParentIsSet)
				{
					if ((tmpPointTwo.ParentBranch.LeftIsSet) && (tmpPointTwo.ParentBranch.LeftBranch.Index == pRotationPoint.Index))
					{
						//Ok so the left branch points to the Currently Rotating Set
						//Step 5:				
						tmpPointTwo.ParentBranch.LeftBranch = tmpPointTwo;
					}
					else if ((tmpPointTwo.ParentBranch.RightIsSet) && (tmpPointTwo.ParentBranch.RightBranch.Index == pRotationPoint.Index))
					{
						//Ok so the right branch points to the Currently Rotating Set
						//Step 5:				
						tmpPointTwo.ParentBranch.RightBranch = tmpPointTwo;
					}
					else
					{
						//Wha happen?!
						//Step 5: RUINEDED
						this.WriteToLog("Error during Single Left Rotation on node index "+pRotationPoint.Index.ToString()+" in a list of "+this.Count.ToString()+" items.");
						this.WriteToLog("  (*) Parent node set with no matching children to reassign");
					}
				}
				
				pRotationPoint.RefigureHeight();
				//Update the statistics
				this._NavigationCount++;
				if (this._LogNavigation)
					this.WriteToLog ("Nav: [Single Left Rotation] in "+this.Count.ToString()+" items");
				return true;
			}
			catch
			{
				this.WriteToLog("Error during Single Left Rotation on node index "+pRotationPoint.Index.ToString()+" in a list of "+this.Count.ToString()+" items.");
				return false;
			}
		}

		protected bool DoubleLeftRotation (TreeNode pRotationPoint)
		{
			try
			{
				TreeNode tmpPointTwo;
				/*
				 * Diagram of a double Left Rotation (Rotation point will be the unbalanced point [1]:
				 * 
				 * 1) Starting out with the unbalanced tree at point [1]
				 *      <p>
				 *       |
				 *      [1]
				 *        \
				 *         \
				 *         [3]
				 *         /
				 *       [2]
				 * 
				 * 2) Rotate Node 2 right
				 *      <p>
				 *       |
				 *      [1]
				 *        \
				 *        [2]
				 *          \
				 *          [3]
				 * 
				 * 3) Rotate Node 1 left
				 *        <p>
				 *         |
				 *        [2]
				 *        / \
				 *      [1] [3]
				 * 
				 * Steps involved:
				  * 1: Rotate 2 right
				  * 2: Rotate 1 left
				  * 
				*/
				
				tmpPointTwo = pRotationPoint.RightBranch;
				
				//Step 1:
				this.RotateRight(tmpPointTwo);
				
				//Step 2:
				this.RotateLeft(pRotationPoint);
				
				//Update the statistics
				this._NavigationCount++;

				if (this._LogNavigation)
				{
					this.WriteToLog ("Nav: [Double Left Rotation] in "+this.Count.ToString()+" items");
				}
					
				return true;
			}
			catch
			{
				this.WriteToLog("Error during Double Left Rotation on node index "+pRotationPoint.Index.ToString()+" in a list of "+this.Count.ToString()+" items.");
				return false;
			}		
		}

		/// <summary>
		/// Rotate a node right.
		/// 
		/// Determine if single or double rotation is required.
		/// </summary>
		/// <param name="pRotationPoint">Node to rotate</param>
		/// <returns>True if Successful</returns>
		protected bool RotateRight (TreeNode pRotationPoint)
		{
			try
			{
				//Make sure there is a node attached to the Right Branch
				if (pRotationPoint.LeftIsSet)
				{
					//See if there is a Left Branch on the Node that is being rotated
					if (pRotationPoint.LeftBranch.RightIsSet)
						//Double Rotation Necessary!
						return this.DoubleRightRotation (pRotationPoint);
					else
						//We only need a Single Rotation
						return this.SingleRightRotation (pRotationPoint);
				}
				else
				{
					return false;
				}
			}
			catch
			{
				return false;
			}
		}
		
		protected bool SingleRightRotation (TreeNode pRotationPoint)
		{
			try
			{
				TreeNode tmpPointTwo;
				/*
				 * Diagram of a single Right Rotation (Rotation point will be the unbalanced point [3]:
				 * 
				 * 1) Starting out with the unbalanced tree at point [3]
				 *        <p>
				 *         |
				 *        [3]
				 *        /
				 *      [2]
				 *      /
				 *    [1]
				 * 
				 * 2) Rotate Node 3 down and replace 2's parent with 3's parent on the same link.
				 *        <p>
				 *         |
				 *        [2]
				 *        / \
				 *      [1] [3]
				 * 
				 * Steps involved:
				  * 1: Assign [3] to [2]'s Right Branch and Unassign [3]'s Left Branch.
				  * 2: Assign [2]'s Parent to <p> (if <p> exists)
				  * 3: Assign [3]'s Parent to [2]
				  * 4: Determine which of <p>'s branches point to [3] (if <p> exists)
				  * 5: Assign <p>'s Determined Branch to [3] (if <p> exists)
				  * 
				*/
				
				tmpPointTwo = pRotationPoint.LeftBranch;
				
				//Step 1:
				tmpPointTwo.RightBranch = pRotationPoint;
				pRotationPoint.LeftBranch = null;
				
				//Step 2:
				if (pRotationPoint.ParentIsSet)
				{
					tmpPointTwo.ParentBranch = pRotationPoint.ParentBranch;
				}
				else
				{
					tmpPointTwo.ParentBranch = null;
					this.TreeHead = tmpPointTwo;					
				}
				
				//Step 3:
				pRotationPoint.ParentBranch = tmpPointTwo;
				
				//Step 4:
				if (tmpPointTwo.ParentIsSet)
				{
					if ((tmpPointTwo.ParentBranch.LeftIsSet) && (tmpPointTwo.ParentBranch.LeftBranch.Index == pRotationPoint.Index))
					{
						//Ok so the left branch points to the Currently Rotating Set
						//Step 5:				
						tmpPointTwo.ParentBranch.LeftBranch = tmpPointTwo;
					}
					else if ((tmpPointTwo.ParentBranch.RightIsSet) && (tmpPointTwo.ParentBranch.RightBranch.Index == pRotationPoint.Index))
					{
						//Ok so the right branch points to the Currently Rotating Set
						//Step 5:				
						tmpPointTwo.ParentBranch.RightBranch = tmpPointTwo;
					}
					else
					{
						//Wha happen?!
						//Step 5: RUINEDED
						this.WriteToLog("Error during Single Right Rotation on node index "+pRotationPoint.Index.ToString()+" in a list of "+this.Count.ToString()+" items.");
						this.WriteToLog("  (*) Parent node set with no matching children to reassign");
					}
				}
				
				pRotationPoint.RefigureHeight();
				//Update the statistics
				this._NavigationCount++;
				if (this._LogNavigation)
					this.WriteToLog ("Nav: [Single Right Rotation] in "+this.Count.ToString()+" items");
				return true;
			}
			catch
			{
				this.WriteToLog("Error during Single Right Rotation on node index "+pRotationPoint.Index.ToString()+" in a list of "+this.Count.ToString()+" items.");
				return false;
			}
		}

		protected bool DoubleRightRotation (TreeNode pRotationPoint)
		{
			try
			{
				TreeNode tmpPointTwo;
				/*
				 * Diagram of a double Left Rotation (Rotation point will be the unbalanced point [3]:
				 * 
				 * 1) Starting out with the unbalanced tree at point [3]
				 *        <p>
				 *         |
				 *        [3]
				 *        /
				 *       /
				 *     [1]
				 *       \
				 *       [2]
				 * 
				 * 2) Rotate Node 1 left
				 *      <p>
				 *       |
				 *      [3]
				 *      /
				 *    [2]
				 *    /
				 *  [1]
				 * 
				 * 3) Rotate Node 3 right
				 *        <p>
				 *         |
				 *        [2]
				 *        / \
				 *      [1] [3]
				 * 
				 * Steps involved:
				  * 1: Rotate 1 left
				  * 2: Rotate 2 right
				  * 
				*/
				
				tmpPointTwo = pRotationPoint.LeftBranch;
				
				//Step 1:
				this.RotateLeft(tmpPointTwo);
				
				//Step 2:
				this.RotateRight(pRotationPoint);
				
				//Update the statistics
				this._NavigationCount++;
				if (this._LogNavigation)
					this.WriteToLog ("Nav: [Double Right Rotation] in "+this.Count.ToString()+" items");
				return true;
			}
			catch
			{
				this.WriteToLog("Error during Double Right Rotation on node index "+pRotationPoint.Index.ToString()+" in a list of "+this.Count.ToString()+" items.");
				return false;
			}		
		}
		#endregion
		
		#region AVL Tree Search Functions
		public bool FindFirstIndex (long pIndex)
		{
			DataNode tmpNode;
			bool tmpData = false;
			tmpNode = new DataNode(tmpData);
			tmpNode.Index = pIndex;
			return FindFirst (tmpNode);
		}
		
		public bool FindFirst (DataNode pNodeToMatch)
		{
			//This will do an iterate match from the top for it!  Sweet!
			//Default to not assigning the failed endpoint to current node.
			return this.IterateMatch (pNodeToMatch, this.TreeHead, false);			
		}
		
		public bool FindFirst (DataNode pNodeToMatch, bool pAssignFailedEndpoint)
		{
			return this.IterateMatch (pNodeToMatch, this.TreeHead, pAssignFailedEndpoint);
		}
		
		public bool IterateMatch (DataNode pNodeToMatch, TreeNode pNodeToCompare, bool pAssignFailedEndpoint)
		{
			/* Recursively search a list for either (A) a matching node or, 
			 *  (B) an endpoint where it will fit.
			 * 
			 * 1: With a balanced list of 7 objects:
			 *                 [10]
			 *                /    \
			 *               /      \
			 *              /        \
		     *            [5]        [15]
		     *            / \        /  \
		     *          [3] [8]   [12]  [18]
		     * 
		     * 2: Finding 8 would take three iterations.
		     *    Input       Return
		     *    .....       ......
		     *    10          -1
		     *    5           1
		     *    8           0 (true)
		     * 
		     * 3: Finding 13 would take three iterations to fail.
		     *    Input       Return
		     *    .....       ......
		     *    10          1
		     *    15          -1
		     *    12          1 (false)
		     * 
		     * Handily, our function will still set CurrentNode to point to the
		     * failed point on the list so we can insert there if the trasverse
		     * is for that purpose.
		     * 
		     * Actually, that is an optional parameter.  The FindFirst can be overloaded.
		     * 
		     * So, if 0 assign and return true.
		     */
		     
		     try
		     {
			     // Node Balance
			     this.WriteToLog ("Beginning iterate search for node with index #"+pNodeToMatch.Index.ToString()+" at index #"+pNodeToCompare.Index.ToString()+".  Current #"+this.CurrentNode.Index.ToString()+".");

			     int tmpNodeBalance;
			     
			     tmpNodeBalance = this.TreeComparator.Compare (pNodeToMatch, pNodeToCompare);
			     
			     if (tmpNodeBalance == 0)
			     {
			     	//Equal match
			     	this.CurrentNode = pNodeToCompare;
				    if (this._LogSearches)
				     	this.WriteToLog ("Search success.  Ended at node #"+this.CurrentNode.Index.ToString()+".");
			     	return true;
			     }
			     else if (tmpNodeBalance < 0)
			     {
			     	// The Pachinko ball falls left
			     	if (pNodeToCompare.LeftIsSet)
			     	{
			     		//Recurse
					    if (this._LogSearches)
					     	this.WriteToLog ("Recursing Left.");
			     		return this.IterateMatch (pNodeToMatch, pNodeToCompare.LeftBranch, pAssignFailedEndpoint);
			     	}
			     }
			     else if (tmpNodeBalance > 0)
			     {
			     	//Falling right
			     	if (pNodeToCompare.RightIsSet)
			     	{
			     		//Recurse
					    if (this._LogSearches)
					     	this.WriteToLog ("Recursing Right.");
			     		return this.IterateMatch (pNodeToMatch, pNodeToCompare.RightBranch, pAssignFailedEndpoint);
			     	}
			     }
			     
			     //If the function gets here we ran out of branches!
			     if (pAssignFailedEndpoint)
			     {
			     	//Assign the failed endpoint.  Probably for an add
			     	this.CurrentNode = pNodeToCompare;
			     }

			     if (this._LogSearches)
				 {
			     	this.WriteToLog ("Search failed.  Ended at node #"+this.CurrentNode.Index.ToString()+".");
				 }	
			     
			     return false;
		     }
		     catch
		     {
			    this.WriteToLog ("Recursive Search had an Error.  Ended at node #"+pNodeToCompare.Index.ToString()+".");
		     	return false;
		     }
		}
		#endregion
	}

	/// <summary>
	/// The AVL Tree node.
	/// </summary>
	public class TreeNode : DataNode
    {
		//The parent node
		private TreeNode _ParentBranch;
		
		//The right and left branches
        private TreeNode _RightBranch;
        private TreeNode _LeftBranch;
        
        //The cached height of the branch so we don't have massive recursive untidiness
        private int _Height;
        
        //This will ensure that recomputing of heights happens when it is necessary.
        private bool _Altered;
                        
        public TreeNode (object NewDataGram):base(NewDataGram)
        {
        	this._Altered = true;
        	
       		this.RefigureHeight();
        }
        
        #region Tree Node Data Access Functions
        public TreeNode ParentBranch
        {
        	get { return this._ParentBranch; }
			set {this._ParentBranch = value; }
        }
        
        public TreeNode LeftBranch
        {
        	get { return this._LeftBranch; }
			set
			{
				this._Altered = true;
				this._LeftBranch = value;
			}
        }
        
        public TreeNode RightBranch
        {
        	get { return this._RightBranch; }
        	set
        	{
        		this._Altered = true;
        		this._RightBranch = value;
        	}
        }
        
        public void RefigureHeight ()
        {
        	int tmpOldHeight = this._Height;

        	this._Height = 1;

        	if (this.RightIsSet)
        		this._Height += this.RightBranch.Height;

        	if (this.LeftIsSet)
        		this._Height += this.LeftBranch.Height;
        	
        	if ((this._Height != tmpOldHeight) && this.ParentIsSet)
        		//This will automagically cascade upwards!  Sweet!
        		this.ParentBranch.RefigureHeight();
        	
        	this._Altered = false;
        }
        
        public int Height
        {
        	get
        	{
        		if (this._Altered)
        			//This creates an automagic opposing direction recursive height figuring set
        			this.RefigureHeight();
        		
        		return this._Height;
        	}
        }

        public bool ParentIsSet
        {
        	get
        	{
        		if (this.ParentBranch == null)
        			return false;
        		else
        			return true;
        	}
        }
        
        
        public int RightBranchHeight
        {
        	get
        	{
        		if (this.RightIsSet)
        			return this.RightBranch.Height;
        		else
        			return 0;
        	}
        }

        public bool RightIsSet
        {
        	get
        	{
        		if (this.RightBranch == null)
        			return false;
        		else
        			return true;
        	}
        }
        

        public int LeftBranchHeight
        {
        	get
        	{
        		if (this.LeftIsSet)
        			return this.LeftBranch.Height;
        		else
        			return 0;
        	}
        }
        
        public bool LeftIsSet
        {
        	get
        	{
        		if (this.LeftBranch == null)
        			return false;
        		else
        			return true;
        	}
        }
        
        public bool EndPoint
        {
        	get
        	{
        		if (this.Height == 1)
        			return true;
        		else
        			return false;
        	}
        }
        
        /// <summary>
        /// The balance of the tree.
        /// 
        /// +1 is Left Heavy, higher is heavier.
        /// 0 is Balanced
        /// -1 is Right Heavy, lower is heavier.
        /// </summary>
        public int Balance
        {
        	get { return this.LeftBranchHeight - this.RightBranchHeight; }
        }
        #endregion
    }
}
