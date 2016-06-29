using System;
using System.Collections.Generic;
using TShockAPI;

namespace CardsAgainstHumanity
{
	public class CahGame
	{
		public GameState gameState { get; private set; }
		public int Round { get; set; }
		public string Question { get; private set; }
		public int TimeLeft { get; private set; }
		public string Winner { get; private set; }
		public TSPlayer Judge { get; private set; }
		public bool Locked { get; set; }
		public int MaxRounds => 10;
		public int MaxPlayers => 7;
		private int TimeVar = 0;
		public Random rnd;

		public CahGame()
		{
			rnd = new Random();
			Round = 0;
			Winner = string.Empty;
		}

		public void GetNewQuestion()
		{
			Question = CaHMain.config.Questions[rnd.Next(0, CaHMain.config.Questions.Length)];
		}

		public void Start()
		{
			gameState = GameState.Started;
			Utils.CahBroadcast("Cards against humanity will start in 10 seconds!");
			TimeVar = 0;
		}

		public void Stop(bool end = false)
		{
			gameState = GameState.NotStarted;
			Round = 0;
			if (end)
			{
				TSPlayer winner = Utils.GetCahPlayers()[0];
				Utils.CahBroadcast($"{winner.Name} has won the game with {winner.GetCahPlayer().Score} points!");
				TSPlayer.All.SendInfoMessage($"{winner.Name} won Cards Against Humanity with {winner.GetCahPlayer().Score} points!");
				TSPlayer.All.SendInfoMessage("Type \"/cah join\" to join in for the next game!");
			}
			Utils.GetCahPlayers().ForEach((c) => { c.ClearInterfaceAndKick(); });
		}

		public void RunGame(int second)
		{
			if (gameState == GameState.NotStarted || gameState == GameState.Started)
			{
				Utils.GetCahPlayers().ForEach((c) =>
				{
					c.SendCahLobbyInterface(this);
				});
				if (gameState == GameState.Started)
				{
					if (TimeVar >= 10)
					{
						NextRound();
					}
					TimeVar++;
				}
			}
			else if (gameState == GameState.WaitingForAnswers)
			{
				Utils.GetCahPlayers().ForEach((c) =>
				{
					c.SendCaHGameInterface(this);
				});

				if (TimeLeft <= 1)
				{
					Utils.CahBroadcast("Time is up!");
					TimeVar = 0;
					SetJudge();
					gameState = GameState.WaitingForVote;
				}
				TimeLeft--;
			}
			else if (gameState == GameState.WaitingForVote || gameState == GameState.VoteCast)
			{
				Utils.GetCahPlayers().ForEach((c) =>
				{
					if (gameState == GameState.WaitingForVote && c == Judge)
					{
						c.SendCahJudgeInterface(this);
					}
					else
						c.SendCahVoteInterface(this);
				});
				if (gameState == GameState.VoteCast)
				{
					if (TimeVar >= 5)
					{
						gameState = GameState.ScoreOverview;
						TimeVar = 0;
					}
					TimeVar++;
				}
			}
			else if (gameState == GameState.ScoreOverview)
			{
				Utils.GetCahPlayers().ForEach((c) =>
				{
					c.SendCaHScoreInterface(this);
				});
				if (TimeVar >= 10)
				{
					NextRound();
				}
				TimeVar++;
			}
		}

		public void SetJudge()
		{
			List<TSPlayer> cahPlayers = Utils.GetCahPlayers();
			Judge = cahPlayers[rnd.Next(0, cahPlayers.Count)];
		}

		public void NextRound()
		{
			if (Round >= MaxRounds)
			{
				Stop(true);
				return;
			}
			Utils.GetCahPlayers().ForEach((c) => { c.GetCahPlayer().Reset(); });
			Round++;
			GetNewQuestion();
			TimeLeft = 40;
			Winner = string.Empty;
			gameState = GameState.WaitingForAnswers;
			Utils.CahBroadcast($"Round {Round} has started!");
		}

		public void Win(TSPlayer ts)
		{
			ts.GetCahPlayer().Score++;
			gameState = GameState.VoteCast;
			Winner = ts.Name;
		}
	}

	public enum GameState
	{
		NotStarted,
		Started,
		WaitingForAnswers,
		WaitingForVote,
		VoteCast,
		ScoreOverview
	}
}