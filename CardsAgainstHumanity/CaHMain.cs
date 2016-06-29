using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace CardsAgainstHumanity
{
	[ApiVersion(1, 23)]
	public class CaHMain : TerrariaPlugin
	{
		public override string Author => "Laurens";
		public override string Description => "Cards Against Humanity";
		public override string Name => "CardsAgainstHumanity";
		public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;


		public static Timer timer = new Timer(1000) { Enabled = true };
		public static Config config;

		public CahGame CahGame;

		public override void Initialize()
		{
			if (!File.Exists(Config.SavePath))
			{
				config = new Config();
				config.Save();
			}
			else config = Config.Load();



			Commands.ChatCommands.Add(new Command("cah.play", Cah, "cah"));
			CahGame = new CahGame();
			timer.Elapsed += Timer_Elapsed;
		}

		void Cah(CommandArgs args)
		{
			if (!args.Player.IsLoggedIn)
			{
				args.Player.SendErrorMessage("You need to be logged in to use this command!");
				return;
			}
			CommandArgs newArgs = null;
			if (args.Parameters.Count > 0)
				newArgs = new CommandArgs(args.Message, args.Player, args.Parameters.GetRange(1, args.Parameters.Count - 1));
			switch (args.Parameters.Count == 0 ? "help" : args.Parameters[0].ToLower())
			{
				case "start":
					if (!args.Player.HasPermission("cah.admin"))
					{
						args.Player.SendErrorMessage("You do not have permission to use this command!");
						return;
					}
					StartCommand(newArgs);
					break;
				case "join":
					JoinCommand(newArgs);
					break;
				case "leave":
					LeaveCommand(newArgs);
					break;
				case "answer":
					AnswerCommand(newArgs);
					break;
				case "win":
					WinCommand(newArgs);
					break;
				case "stop":
					if (!args.Player.HasPermission("cah.admin"))
					{
						args.Player.SendErrorMessage("You do not have permission to use this command!");
						return;
					}
					StopCommand(newArgs);
					break;
				case "lock":
					if (!args.Player.HasPermission("cah.admin"))
					{
						args.Player.SendErrorMessage("You do not have permission to use this command!");
						return;
					}
					LockCommand(newArgs);
					break;
				default:
					args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /cah <subcommand>");
					args.Player.SendInfoMessage("/cah join - join a cah game");
					args.Player.SendInfoMessage("/cah leave - leave a cah game");
					args.Player.SendInfoMessage("/cah answer <answer> give your answer for the current round");
					args.Player.SendInfoMessage("/cah win <player> choose which player wins the round");
					args.Player.SendInfoMessage("/cah lock - toggle the ability to join the game.");
					return;
			}
		}

		public void LockCommand(CommandArgs args)
		{
			CahGame.Locked = !CahGame.Locked;
			args.Player.SendInfoMessage("The game is now {0}locked", CahGame.Locked ? "" : "un");
		}

		public void StopCommand(CommandArgs args)
		{
			if (CahGame.gameState == GameState.NotStarted)
			{
				args.Player.SendErrorMessage("The game isn't running!");
				return;
			}
			args.Player.SendInfoMessage("You have stopped the game!");
			Utils.CahBroadcast($"{args.Player.Name} has stopped the game!");
			CahGame.Stop();
		}

		public void StartCommand(CommandArgs args)
		{
			if (CahGame.gameState != GameState.NotStarted)
			{
				args.Player.SendErrorMessage("The game is already running!");
				return;
			}
			CahGame.Start();
		}

		public void JoinCommand(CommandArgs args)
		{
			CahPlayer cahPlayer = args.Player.GetCahPlayer();
			if (cahPlayer != null)
			{
				args.Player.SendErrorMessage("You are already in the game!");
				return;
			}
			if (CahGame.Locked)
			{
				args.Player.SendErrorMessage("The game is locked and you cannot join!");
				return;
			}
			if (Utils.GetCahPlayers().Count >= CahGame.MaxPlayers)
			{
				args.Player.SendErrorMessage("The game is already full!");
				return;
			}
			args.Player.SetData("cah", new CahPlayer());
			Utils.CahBroadcast($"{args.Player.Name} has joined the game!");
		}

		public void LeaveCommand(CommandArgs args)
		{
			CahPlayer cahPlayer = args.Player.GetCahPlayer();
			if (cahPlayer == null)
			{
				args.Player.SendErrorMessage("You are not in the game!");
				return;
			}
			args.Player.ClearInterfaceAndKick();
			args.Player.SendInfoMessage("You have left the game!");
			Utils.CahBroadcast($"{args.Player.Name} has left the game!");
			if (Utils.GetCahPlayers().Count == 0)
				CahGame.Stop();
		}

		public void AnswerCommand(CommandArgs args)
		{
			if (CahGame.gameState == GameState.NotStarted)
			{
				args.Player.SendErrorMessage("The game hasn't started yet!");
				return;
			}
			if (args.Parameters.Count == 0)
			{
				args.Player.SendErrorMessage("Invalid syntax! proper syntax: /cah answer <answer>");
				return;
			}
			CahPlayer cahPlayer = args.Player.GetCahPlayer();
			if (cahPlayer == null)
			{
				args.Player.SendErrorMessage("You are not participating in the current game!");
				return;
			}
			if (CahGame.gameState != GameState.WaitingForAnswers)
			{
				args.Player.SendErrorMessage("The game is not waiting for answers at this moment!");
				return;
			}
			if (cahPlayer.Answered)
			{
				args.Player.SendErrorMessage("You have already given an answer for this round!");
				return;
			}
			cahPlayer.SetAnswer(string.Join(" ", args.Parameters));
			args.Player.SendInfoMessage("Your answer has been submitted!");
		}

		public void WinCommand(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /cah win <player>");
				return;
			}
			if (CahGame.gameState != GameState.WaitingForVote)
			{
				args.Player.SendErrorMessage("It is not the time to vote!");
				return;
			}
			if (args.Player != CahGame.Judge)
			{
				args.Player.SendErrorMessage("You are not the judge!");
				return;
			}
			string plStr = String.Join(" ", args.Parameters);
			var players = TShock.Utils.FindPlayer(plStr);
			if (players.Count == 0)
			{
				args.Player.SendErrorMessage("Invalid player!");
				return;
			}
			if (players.Count > 1)
			{
				TShock.Utils.SendMultipleMatchError(args.Player, players.Select(p => p.Name));
				return;
			}
			var plr = players[0];
			if (!Utils.GetCahPlayers().Any(c => c == plr))
			{
				args.Player.SendErrorMessage("This player is not in the current game!");
				return;
			}
			args.Player.SendInfoMessage($"You have selected {plr.Name} as winner for this round!");
			Utils.CahBroadcast($"{plr.Name} has been selected as winner for this round!");
			CahGame.Win(plr);
		}

		private void Timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			CahGame.RunGame(e.SignalTime.Second);
		}

		public CaHMain(Main game) : base(game)
		{
			Order = 1;
		}
	}
}
