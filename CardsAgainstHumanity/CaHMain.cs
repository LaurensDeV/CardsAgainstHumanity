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
using TShockAPI.Hooks;

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
			ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
			CahGame = new CahGame(config);
			timer.Elapsed += Timer_Elapsed;
		}

		void OnLeave(LeaveEventArgs e)
		{
			if (TShock.Players[e.Who] == null)
				return;
			if (CahGame.Judge == TShock.Players[e.Who])
			{
				TShock.Players[e.Who].RemoveData("cah");
				CahGame.SetJudge();
			}
			else
				TShock.Players[e.Who].RemoveData("cah");
		}

		void Cah(CommandArgs args)
		{
			if (!args.Player.IsLoggedIn)
			{
				args.Player.SendErrorMessage("You need to be logged in to use this command!");
				return;
			}
			CahPlayer cplr = args.Player.GetCaHPlayer();
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
					if (cplr != null && cplr.Spectating)
					{
						args.Player.SendErrorMessage("You are in spectate mode and cannot use this command!");
						return;
					}
					AnswerCommand(newArgs);
					break;
				case "win":
					if (cplr != null && cplr.Spectating)
					{
						args.Player.SendErrorMessage("You are in spectate mode and cannot use this command!");
						return;
					}
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
				case "spectate":
					SpectateCommand(newArgs);
					break;
				default:
					args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /cah <subcommand>");
					args.Player.SendInfoMessage("/cah join - join a cah game");
					args.Player.SendInfoMessage("/cah leave - leave a cah game");
					args.Player.SendInfoMessage("/cah answer <answer> give your answer for the current round");
					args.Player.SendInfoMessage("/cah win <number> choose which answer wins the round");
					args.Player.SendInfoMessage("/cah spectate - spectate the current game.");
					if (args.Player.HasPermission("cah.admin"))
					{
						args.Player.SendInfoMessage("/cah start - start the game.");
						args.Player.SendInfoMessage("/cah lock - toggle the ability to join the game.");
						args.Player.SendInfoMessage("/cah stop - Stop the game.");
					}
					return;
			}
		}

		public void SpectateCommand(CommandArgs args)
		{
			CahPlayer cahPlayer = args.Player.GetCaHPlayer();
			if (cahPlayer != null)
			{
				if (cahPlayer.Spectating)
				{
					args.Player.SendErrorMessage("You are already spectating!");
					return;
				}
				args.Player.Spectate(CahGame);
				Utils.CahBroadcast($"{args.Player.Name} switched to spectate mode!");
				return;
			}
			args.Player.SetData("cah", new CahPlayer(true));
			args.Player.SendInfoMessage("You are now spectating Cards against Humanity!");
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
			if (CahGame.gameState != GameState.NotStarted && CahGame.gameState != GameState.AutoStarting)
			{
				args.Player.SendErrorMessage("The game is already running!");
				return;
			}
			if (Utils.GetCahPlayers().Count(c => !c.GetCaHPlayer().Spectating) < 3)
			{
				args.Player.SendErrorMessage("There need to be atleast 3 players to start Cards against Humanity!");
				return;
			}
			args.Player.SendInfoMessage("You have started Cards Against Humanity!");
			CahGame.Start();
		}

		public void JoinCommand(CommandArgs args)
		{
			CahPlayer cahPlayer = args.Player.GetCaHPlayer();
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
			if (Utils.GetCahPlayers().Count(c => !c.GetCaHPlayer().Spectating) >= CahGame.MaxPlayers)
			{
				args.Player.SendErrorMessage("The game is already full! If you want to spectate type /cah spectate.");
				return;
			}
			args.Player.SetData("cah", new CahPlayer());
			Utils.CahBroadcast($"{args.Player.Name} has joined the game!");
		}

		public void LeaveCommand(CommandArgs args)
		{
			CahPlayer cahPlayer = args.Player.GetCaHPlayer();
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
			CahPlayer cahPlayer = args.Player.GetCaHPlayer();
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
			if (CahGame.Judge == args.Player)
			{
				args.Player.SendErrorMessage("You are the judge and cannot give in answer for this round!");
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
			List<TSPlayer> cahPlayers = Utils.GetCahPlayers().FindAll(c => c != CahGame.Judge && !c.GetCaHPlayer().Spectating).OrderBy(c => (c.Name)).ToList();
			if (args.Parameters.Count < 1)
			{
				args.Player.SendErrorMessage($"Invalid syntax! Proper syntax: /cah win <1 - {cahPlayers.Count}>");
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
			int num;
			if (!int.TryParse(args.Parameters[0], out num))
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /cah win <number>");
				return;
			}
			if (num < 1 || num > cahPlayers.Count)
			{
				args.Player.SendErrorMessage($"Invalid syntax! Proper syntax: /cah win <1 - {cahPlayers.Count}>");
				return;
			}
			var plr = cahPlayers[num - 1];
			args.Player.SendInfoMessage($"You have selected {plr.Name} as winner for this round!");
			Utils.CahBroadcast($"{plr.Name} has been selected as winner for this round!");
			CahGame.Win(plr);
		}

		private void Timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			CahGame.RunGame(e.SignalTime.Second);
		}

		protected override void Dispose(bool disposing)
		{
			ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
			timer.Elapsed -= Timer_Elapsed;
			base.Dispose(disposing);
		}

		public CaHMain(Main game) : base(game)
		{
			Order = 1;
		}
	}
}
