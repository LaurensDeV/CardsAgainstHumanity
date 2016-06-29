using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TShockAPI;

namespace CardsAgainstHumanity
{
	public class Config
	{
		public static string SavePath => Path.Combine(TShock.SavePath, "CardsAgainstHumanity.json");

		public int MaxRounds;
		public int MaxPlayers;
		public string[] Questions;

		public Config()
		{
			MaxRounds = 15;
			MaxPlayers = 7;

			Questions = new string[]
				{
					"Instead of coal, Santa now gives the bad children ______.",
					"______. Thats's how I want do die.",
					"What ended my last relationship?",
					"I drink to forget _______.",
					"What is Batman's guilty pleasure?",
					"What are my parents hiding from me?",
					"______. It's a trap!",
					"What do old people smell like?",
					"What's my secret power?",
					"The class field trip was completely ruined by _______.",
					"Why do I hurt all over?",
					"What never fails to liven up the party?",
					"What would grandma find disturbing, yet oddly charming?",
					"What's that smell?",
					"What's the next Happy Meal toy?",
					"It's a pity that kids these days are all getting involved with ______.",
					"Whats the most emo?",
					"White people like ______.",
					"Trump is going to be an amazing president because _______.",
					"Justin Bieber's new hit ______ was just released.",
					"Hitler's only crime was ______.",
					"I've got 99 problems but a ______ ain't one.",
					"The United States is being ruined by ______.",
					"Man, Michael Jackson sure loved ______.",
					"When I am a billionaire, I shall erect a 50-foot statue to commemorate ______.",
					"War! What is it good for?",
					"In his new self-produced album, Kanye West raps over the sounds of ______.",
					"After blacking out during New Year's Eve, I was awoken by ______.",
					"______. Awesome in theory, kind of a mess in practice",
					"______. High five, bro.",
					"I learned the hard way that you can't cheer up a grieving friend with ______.",
					"When I am President of the United States, I will create the Department of ______.",
					"What's fun until it gets weird?",
					"What does Dick Cheney prefer?",
					"Daddy, why is mommy crying?",
					"Hey baby, come back to my place and I'll show you ______.",
					"What gets better with age?",
					"This month's Cosmo: \"Spice up your sex life by bringing ______ into the bedroom\".",
					"Maybe she's born with it. Maybe it's ______.",
					"Next on ESPN2: The world Series of ______.",
					"What did the US airdrop to the children of Afghanistan?",
					"What has been making life difficult at the nudist colony?",
					"TSA guidelines now prohibit ______ on airplanes.",
					"In 1,000 years, when paper money is but a distant memory, ______ will be our currency.",
					"What don't you want to find in your chinese food?",
					"What brought the orgy to a grinding halt?",
					"But before I kill you, Mr. Bond, I must show you ______.",
					"Studies show that lab rats navigate mazes 50% faster after being exposed to ______.",
					"Science will never explain the origin of ______.",
					"He who controls ______ controls the world.",
					"The CIA now interrogates enemy agents by repeatedly subjecting them to ______.",
				};
		}

		public static Config Load()
		{
			using (StreamReader sw = new StreamReader(File.Open(SavePath, FileMode.Open)))
			{
				return JsonConvert.DeserializeObject<Config>(sw.ReadToEnd());
			}
		}

		public void Save()
		{
			using (StreamWriter sw = new StreamWriter(File.Open(SavePath, FileMode.Create)))
			{
				sw.Write(JsonConvert.SerializeObject(this, Formatting.Indented));
			}
		}
	}
}

