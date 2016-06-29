using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;

namespace CardsAgainstHumanity
{
	public static class Utils
	{
		public static string RepeatLineBreaks(int amount)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < amount; i++)
				sb.Append("\r\n");
			return sb.ToString();
		}

		public static string RepeatSpaces(int amount) => new string(' ', amount);

		public static string LineSegment => new string('-', 40);

		public static string SplitStringToFitScreen(string msg)
		{
			if (msg.Length < 50)
				return msg;

			StringBuilder sb = new StringBuilder();
			string[] split = msg.Split();

			int len = 0;
			for (int i = 0; i < split.Length; i++)
			{
				if (len + split[i].Length >= 50)
				{
					sb.Append("\r\n");
					len = 0;
				}
				len += split[i].Length;
				sb.Append(split[i]).Append(" ");
			}
			return sb.ToString();
		}

		public static void SendCaHGameInterface(this TSPlayer ts, CahGame cahGame)
		{
			string Optionalmsg = "";
			CahPlayer cplr = ts.GetCahPlayer();

			if (cahGame.Judge == ts)
				Optionalmsg = "You are the judge and cannot submit an answer for this round.";
			if (cplr.Spectating)
				Optionalmsg = "You are currently spectating and can't do anything.";
			else if (!cplr.Answered)
				Optionalmsg = "Use /cah answer <answer>";

			string message = string.Join("\r\n",
			new string[]
			{
				RepeatSpaces(100),
				RepeatLineBreaks(10),
				"Cards against humanity",
				Utils.LineSegment,
				$"Round {cahGame.Round}/{cahGame.MaxRounds} - ({cahGame.TimeLeft} seconds left to answer)",
				Utils.SplitStringToFitScreen("Question: " + cahGame.Question),
				Utils.LineSegment,
				"Given answers:",
				Utils.LineSegment,
				GetAnswers(cahGame.Judge),
				Utils.LineSegment,
				SplitStringToFitScreen(Optionalmsg),
				RepeatLineBreaks(50),
			});
			ts.SendData(PacketTypes.Status, message);
		}

		public static void SendCaHScoreInterface(this TSPlayer ts, CahGame cahGame)
		{
			string message = string.Join("\r\n",
			new string[]
			{
				RepeatSpaces(100),
				RepeatLineBreaks(10),
				"Cards against humanity",
				Utils.LineSegment,
				"Current scores overview:",
				Utils.LineSegment,
				GetScores(),
				Utils.LineSegment,
				RepeatLineBreaks(50),
			});
			ts.SendData(PacketTypes.Status, message);
		}

		public static void SendCahLobbyInterface(this TSPlayer ts, CahGame cahGame)
		{
			string message = string.Join("\r\n",
			new string[]
			{
				RepeatSpaces(100),
				RepeatLineBreaks(10),
				"Cards against humanity",
				Utils.LineSegment,
				$"Waiting for players to join",
				"Current players:",
				Utils.LineSegment,
				GetUsernames(),
				Utils.LineSegment,
				RepeatLineBreaks(50)
			});
			ts.SendData(PacketTypes.Status, message);
		}

		public static void SendCahVoteInterface(this TSPlayer ts, CahGame cahGame)
		{
			if (cahGame.Judge == null)
				cahGame.SetJudge();

			string message = string.Join("\r\n",
			new string[]
			{
				RepeatSpaces(100),
				RepeatLineBreaks(10),
				"Cards against humanity",
				Utils.LineSegment,
				$"Waiting for the judge ({cahGame.Judge.Name}) to vote",
				Utils.LineSegment,
				$"Winner of this round: {cahGame.Winner?.Name ??  ""}",
				SplitStringToFitScreen($"Question: {cahGame.Question}"),
				SplitStringToFitScreen($"Chosen answer: {cahGame.Winner?.GetCahPlayer()?.Answer ?? ""}"),
				Utils.LineSegment,
				RepeatLineBreaks(50)
			});
			ts.SendData(PacketTypes.Status, message);
		}

		public static void SendCahJudgeInterface(this TSPlayer ts, CahGame cahGame)
		{
			string message = string.Join("\r\n",
			new string[]
			{
				RepeatSpaces(100),
				RepeatLineBreaks(10),
				"Cards against humanity",
				Utils.LineSegment,
				$"You are the judge!",
				SplitStringToFitScreen("Type /cah win <player> to choose a winner for this round."),
				$"Players to choose from:",
				Utils.LineSegment,
				GetAnswers(cahGame.Judge),
				Utils.LineSegment,
				RepeatLineBreaks(50)
			});
			ts.SendData(PacketTypes.Status, message);
		}

		public static string GetUsernames()
		{
			StringBuilder sb = new StringBuilder();
			List<TSPlayer> cahPlayers = GetCahPlayers().FindAll(c => !c.GetCahPlayer().Spectating);
			for (int i = 0; i < cahPlayers.Count; i++)
			{
				sb.Append($"{cahPlayers[i].Name}");
				if (i < cahPlayers.Count - 1)
					sb.Append("\r\n");
			}
			return sb.ToString();
		}

		public static string GetScores()
		{
			StringBuilder sb = new StringBuilder();
			List<TSPlayer> cahPlayers = GetCahPlayers().FindAll(c => !c.GetCahPlayer().Spectating);
			for (int i = 0; i < cahPlayers.Count; i++)
			{
				sb.Append($"{cahPlayers[i].Name}: {cahPlayers[i].GetCahPlayer().Score}");
				if (i < cahPlayers.Count - 1)
					sb.Append("\r\n");
			}
			return sb.ToString();
		}

		public static string GetAnswers(TSPlayer judge)
		{
			StringBuilder sb = new StringBuilder();
			List<TSPlayer> cahPlayers = GetCahPlayers().FindAll(c => c != judge && !c.GetCahPlayer().Spectating);
			for (int i = 0; i < cahPlayers.Count; i++)
			{
				sb.Append(SplitStringToFitScreen($"{cahPlayers[i].Name}: {cahPlayers[i].GetCahPlayer().Answer}"));
				if (i < cahPlayers.Count - 1)
					sb.Append("\r\n");
			}
			return sb.ToString();
		}

		public static CahPlayer GetCahPlayer(this TSPlayer ts) => ts.GetData<CahPlayer>("cah");

		public static List<TSPlayer> GetCahPlayers() => TShock.Players.Where(t => t != null && t.IsLoggedIn && t.GetCahPlayer() != null).OrderBy(c => -c.GetCahPlayer().Score).ToList();

		public static void CahBroadcast(string msg)
		{
			GetCahPlayers().ForEach((c) =>
			{
				c.SendMessage(msg, new Color(34, 181, 54));
			});
		}

		public static void ClearInterfaceAndKick(this TSPlayer ts)
		{
			ts.SendData(PacketTypes.Status, string.Empty);
			ts.RemoveData("cah");
		}

		public static void Spectate(this TSPlayer ts, CahGame cahGame)
		{
			ts.GetCahPlayer().Spectating = true;
			if (cahGame.Judge == ts)
				cahGame.SetJudge();
		}
	}
}
