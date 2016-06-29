using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsAgainstHumanity
{
	public class CahPlayer
	{
		public int Score { get; set; }
		public string Answer { get; private set; }
		public bool Answered { get; private set; }

		public CahPlayer()
		{
			Answer = string.Empty;
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
