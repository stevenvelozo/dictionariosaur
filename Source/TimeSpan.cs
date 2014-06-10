/**
* This source file is a part of the Dictionariosaur application.
* For full copyright and license information, please view the LICENSE file
* which should be distributed with this source code.
*
* @license MIT License
* @copyright Copyright (c) 2013, Steven Velozo
*/
using System;

namespace MutiUtility
{
	/// <summary>
	/// Description of MutiTimeSpan.
	/// </summary>
	public class MutiTimeSpan
	{
		private int _ProcessorTickStart;
		private int _ProcessorTickStop;
		
		private int _LastDifference;

		public MutiTimeSpan()
		{
			//No sense not starting the timer at instantiation
			this.Start();
		}
		
		#region Data Access
		public int TimeDifference
		{
			get { return this._LastDifference; }
		}
		#endregion
		
		public void Start()
		{
			//Set the start time
			this._ProcessorTickStart = System.Environment.TickCount;
			//Reset the difference
			this._LastDifference = 0;
		}
		
		public void TimeStamp()
		{
			//Set the stop time
			this._ProcessorTickStop = System.Environment.TickCount;
			//Figure out the difference
			this._LastDifference = this._ProcessorTickStop - this._ProcessorTickStart;
		}
	}
}
