using System;


namespace AssemblyCSharp.Assets.Scripts
{
	public struct ProtocolData
	{
		public enum MessageCode
		{
			SYNC = 0, TURN = 1, MOVE = 2, RESTART = 3, EXIT = 4
		}

		public enum MoveSpace
		{
			TL = 0, TC = 1, TR = 2, ML = 3, MC = 4, MR = 5, BL = 6, BC = 7, BR = 8, NULL_SPACE = -1
		}

		public MessageCode messageCode { get; set; }

		public MoveSpace space { get; set; }

	}
}