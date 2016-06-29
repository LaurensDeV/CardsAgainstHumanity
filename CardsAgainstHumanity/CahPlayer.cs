using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;

namespace CardsAgainstHumanity
{
	public class CahPlayer
	{
		public int Score { get; set; }
		public string Answer { get; private set; }
		public bool Answered { get; private set; }
		public bool Spectating { get; set; }

		public CahPlayer(bool Spectate = false)
		{
			Answer = string.Empty;
			if (Spectate)
				Spectating = true;
		}

		public void SetAnswer(string answer)
		{
			Answer = answer;
			Answered = true;
		}

		public void Reset()
		{
			Answer = string.Empty;
			Answered = false;
		}
	}
}
