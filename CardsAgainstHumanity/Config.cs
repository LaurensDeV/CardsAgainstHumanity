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
		public string[] Questions;

		public Config()
		{
			Questions = new string[]
				{
					"Instead of coal, Santa now gives the bad children _________",
					"_______. Thats's how I want do die.",
					"What ended my last relationship?",
					"I drink to forget _______",
					"What is Batman's guilty pleasure?",
					"What are my parents hiding from me?",
					"________. It's a trap!",
					"What do old people smell like?",
					"What's my secret power?",
					"The class field trip was completely ruined by _______",
					"Why do I hurt all over?",
					"What never fails to liven up the party?",
					"What would grandma find disturbing, yet oddly charming?",
					"What's that smell?",
					"What's the next Happy Meal toy?",
					"It's a pity that kids these days are all getting involved with ______.",
					"Whats the most emo?",
					"White people like ______."
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

