using System;
using System.Collections.Generic;
using System.Linq;
using TShockAPI;

namespace CardsAgainstHumanity
{
	public class CahGame
	{
		public GameState gameState { get; private set; }
		public int Round { get; set; }
		public string Question { get; private set; }
		public int TimeLeft { get; private set; }
		public TSPlayer Winner { get; private set; }
		public TSPlayer Judge { get; private set; }
		public bool Locked { get; set; }
		public int MaxRounds { get; private set; }
		public int MaxPlayers { get; private set; }
		public bool AutoStart { get; private set; }
		private List<string> Questions { get; set; }
		private int TimeVar = 0;
		public Random rnd;

		public CahGame(Config config)
		{
			TimeVar = 50;
			MaxRounds = config.MaxRounds;
			MaxPlayers = config.MaxPlayers;
			AutoStart = config.AutoStart;
			Questions = new List<string>(config.Questions);
			rnd = new Random();
			Round = 0;
		}

		public void GetNewQuestion()
		{
			Question = Questions.FindAll(c => c != Question)[rnd.Next(0, CaHMain.config.Questions.Length - 1)];
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
				List<TSPlayer> Winners = Utils.GetCahPlayers().FindAll(c => c.GetCaHPlayer().Score == winner.GetCaHPlayer().Score);

				if (Winners.Count > 1)
				{
					string winstr = string.Join(", ", Winners.Select(c => c.Name));
					Utils.CahBroadcast($"{winstr} tied with {winner.GetCaHPlayer().Score} points in Cards Against Humanity!");
					TSPlayer.All.SendInfoMessage($"{winstr} tied with {winner.GetCaHPlayer().Score} points in Cards Against Humanity!");
				}
				else
				{
					Utils.CahBroadcast($"{winner.Name} won the game with {winner.GetCaHPlayer().Score} points!");
					TSPlayer.All.SendInfoMessage($"{winner.Name} won Cards Against Humanity with {winner.GetCaHPlayer().Score} points!");
				}
				TSPlayer.All.SendInfoMessage("Type \"/cah join\" to join in for the next game!");
			}
			Utils.GetCahPlayers().ForEach((c) => { c.ClearInterfaceAndKick(); });
			Locked = false;
			TimeVar = 50;
		}

		public void RunGame(int second)
		{
			if (gameState == GameState.NotStarted || gameState == GameState.Started || gameState == GameState.AutoStarting)
			{
				Utils.GetCahPlayers().ForEach((c) =>
				{
					c.SendCaHLobbyInterface(this);
				});
				if (gameState == GameState.NotStarted)
				{
					if (Utils.GetCahPlayers().Count(c => !c.GetCaHPlayer().Spectating) >= 3)
						gameState = GameState.AutoStarting;
				}
				else if (gameState == GameState.AutoStarting)
				{
					if (Utils.GetCahPlayers().Count(c => !c.GetCaHPlayer().Spectating) < 3)
					{
						gameState = GameState.NotStarted;
						Utils.CahBroadcast("Not enough players! Autostart has been cancelled.");
						TimeVar = 50;
					}
					if (second % 15 == 0)
						Utils.CahBroadcast($"Cards against Humanity will automatically start in {10 + TimeVar} seconds!");
					if (second % 25 == 0)
						TSPlayer.All.SendInfoMessage("Cards against humanity will begin soon, type /cah join to play!");
					TimeVar--;
					if (TimeVar <= 0)
						gameState = GameState.Started;
				}
				else if (gameState == GameState.Started)
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
						c.SendCaHJudgeInterface(this);
					}
					else
						c.SendCaHVoteInterface(this);
				});
				if (gameState == GameState.VoteCast)
				{
					if (TimeVar >= 10)
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
			List<TSPlayer> cahPlayers = Utils.GetCahPlayers().FindAll(c => c != Judge && !c.GetCaHPlayer().Spectating);
			Judge = cahPlayers[rnd.Next(0, cahPlayers.Count)];
		}

		public void NextRound()
		{
			if (Round >= MaxRounds)
			{
				Stop(true);
				return;
			}
			Utils.GetCahPlayers().ForEach((c) => { c.GetCaHPlayer().Reset(); });
			Round++;
			GetNewQuestion();
			TimeLeft = 40;
			Winner = null;
			gameState = GameState.WaitingForAnswers;
			SetJudge();
			Utils.CahBroadcast($"Round {Round} has started!");
		}

		public void Win(TSPlayer ts)
		{
			ts.GetCaHPlayer().Score++;
			gameState = GameState.VoteCast;
			Winner = ts;
		}
	}

	public enum GameState
	{
		NotStarted,
		AutoStarting,
		Started,
		WaitingForAnswers,
		WaitingForVote,
		VoteCast,
		ScoreOverview
	}
}