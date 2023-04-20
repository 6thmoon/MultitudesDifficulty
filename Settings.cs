using BepInEx.Configuration;

namespace Local.Difficulty.Multitudes
{
	public static class Settings
	{
		public static void Load(ConfigFile configuration)
		{
			string sectionTitle; int sectionNumber = 0;
			void section(string title) => sectionTitle = ++sectionNumber + ". " + title;

			section("General");

			ConfigEntry<byte> configEntry = configuration.Bind<byte>(
					section: sectionTitle,
					key: "Additional Player Count",
					defaultValue: 1,
					description: "Higher values increase difficulty. Although more enemies"
						+ " will spawn, less money is awarded and purchase costs are increased."
				);

			if ( configEntry.Value == 0 ) configEntry.Value = 1;
			Session.additionalPlayers = configEntry.Value;

			Session.eclipseMode = configuration.Bind(
					section: sectionTitle,
					key: "Eclipse Mode",
					defaultValue: false,
					description: "Use eclipse modifiers. Not for the faint of heart."
				).Value;

			section("Advanced");

			Session.interactableScale = configuration.Bind(
					section: sectionTitle,
					key: "Additional Interactables",
					defaultValue: 0m,
					new ConfigDescription(
						"Increasing this percentage results in additional interactables"
						+ " (i.e. chests, shrines, & other loot), relative to player count.",
							new AcceptableValueRange<decimal>(0, 100))
				).Value / 100;

			Session.extraRewards = configuration.Bind(
					section: sectionTitle,
					key: "Extra Item Rewards",
					defaultValue: false,
					description: "Enable to drop additional items during the teleporter event,"
						+ " Void Fields, and the Simulacrum."
				).Value;

			Session.incomePenalty = configuration.Bind(
					section: sectionTitle,
					key: "Income Penalty",
					defaultValue: 75m,
					new ConfigDescription(
						"Gold is typically split between all players. Lower this value to"
						+ " lessen this effect.",
							new AcceptableValueRange<decimal>(0, 100))
				).Value / 100;

			Session.teleporterChargeRate = configuration.Bind(
					section: sectionTitle,
					key: "Teleporter Duration",
					defaultValue: 0m,
					new ConfigDescription(
						"The extent at which player count is considered when determining charge"
						+ " rate for holdout zones. Higher values result in slower charge.",
							new AcceptableValueRange<decimal>(0, 100))
				).Value / 100;

			Session.bonusHealth = configuration.Bind(
					section: sectionTitle,
					key: "Bonus Health",
					defaultValue: 0m,
					new ConfigDescription(
						"Certain enemies receive bonus health in multiplayer. Reduce the amount"
						+ " granted to teleporter bosses and the like.",
							new AcceptableValueRange<decimal>(0, 100))
				).Value / 100;

			section("Other");

			Session.forceEnable = configuration.Bind(
					section: sectionTitle,
					key: "Force Enable",
					defaultValue: false,
					description: "Force player count adjustment regardless of difficulty"
						+ " selection. For use with other custom difficulty modes."
				).Value;
		}

		public static string BuildDescription(bool verbose = false)
		{
			return ( verbose ? "For those who wish to face vast hordes of enemies alone. " +
						"Multiplayer difficulty levels are in effect.\n\n" +
				"<style=cStack>>Base Difficulty:</style> <style=cSub>" +
						( Session.eclipseMode ? "Eclipse" : "Monsoon" ) + "</style>\n" : "" ) +
				"<style=cStack>>Player Count:</style> " +
						$"<style=cDeath>+{ Session.additionalPlayers }</style>\n" +
				"<style=cStack>>Additional Interactables:</style> " +
						"<style=cShrine>" + ( Session.interactableScale > 0 ?
						Session.interactableScale.ToString("0.#%") : "None" ) + "</style>\n" +
				"<style=cStack>>Extra Item Rewards:</style> " +
						( Session.extraRewards ? "<style=cIsHealing>Enabled</style>\n" :
						"<color=#FF8000>Disabled</color>\n" ) +				// Equipment color.
				"<style=cStack>>Player Income:</style> <style=cIsUtility>" +
						'+' + ( 1 - Session.incomePenalty ).ToString("0.#%") + "</style>\n" +
				"<style=cStack>>Teleporter Duration:</style> <sprite name=\"TP\">" +
						"<color=#307FFF>+" + ( Session.teleporterChargeRate *	// Lunar color.
						Session.additionalPlayers ).ToString("0.#%") + "</color>\n" +
				"<style=cStack>>Enemy Bonus Health:</style> <style=cIsVoid>" +
						'-' + ( 1 - Session.bonusHealth ).ToString("0.#%") + "</style>";
		}
	}
}
