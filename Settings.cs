using BepInEx.Configuration;
using RoR2;

namespace Local.Difficulty.Multitudes
{
	public static class Settings
	{
		public static void Load(ConfigFile configuration)
		{
			var percentage = new AcceptableValueRange<decimal>(0, 100);

			string sectionTitle; int sectionNumber = 0;
			void section(string title) => sectionTitle = ++sectionNumber + ". " + title;

			section("General");

			ConfigEntry<byte> configEntry = configuration.Bind<byte>(
					section: sectionTitle,
					key: "Additional Player Count",
					defaultValue: 1,
					description: "Higher values increase difficulty. More enemies will spawn"
						+ " and purchase costs are increased."
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
						"Increasing this percentage results in additional interactables (i.e."
							+ " chests, shrines, & other loot), relative to player count.",
						acceptableValues: percentage
				)).Value / percentage.MaxValue;

			Session.extraRewards = configuration.Bind(
					section: sectionTitle,
					key: "Extra Item Rewards",
					defaultValue: false,
					description: "Enable to drop additional items during the teleporter event"
						+ " and hidden realms."
				).Value;

			Session.incomePenalty = configuration.Bind(
					section: sectionTitle,
					key: "Income Penalty",
					defaultValue: 75m,
					new ConfigDescription(
						"Gold is typically split between all players. Lower this value to"
							+ " lessen this effect.",
						acceptableValues: percentage
				)).Value / percentage.MaxValue;

			Session.bonusHealth = configuration.Bind(
					section: sectionTitle,
					key: "Bonus Health",
					defaultValue: 0m,
					new ConfigDescription(
						"Certain enemies receive bonus health in multiplayer. Reduce the amount"
							+ " granted to teleporter bosses and the like.",
						acceptableValues: percentage
				)).Value / percentage.MaxValue;

			Session.teleporterChargeRate = configuration.Bind(
					section: sectionTitle,
					key: "Teleporter Duration",
					defaultValue: 0m,
					new ConfigDescription(
						"The extent at which player count is considered when determining charge"
							+ " rate for holdout zones. Higher values result in slower charge.",
						acceptableValues: percentage
				)).Value / percentage.MaxValue;

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
			string description = "";
			if ( verbose )
			{
				description = "For those who wish to face vast hordes of enemies alone. " +
							"Multiplayer difficulty levels are in effect.\n\n" +
					"<style=cStack>>Base Difficulty:</style> <style=cSub>" +
							( Session.eclipseMode ? "Eclipse" : "Monsoon" ) + "</style>\n";
			}

			string lunar = ColorCatalog.GetColorHexString(ColorCatalog.ColorIndex.LunarItem),
				  equipment = ColorCatalog.GetColorHexString(ColorCatalog.ColorIndex.Equipment);

			return description + "<style=cStack>>Player Count:</style> " +
						$"<style=cDeath>+{ Session.additionalPlayers }</style>\n" +
				"<style=cStack>>Additional Interactables:</style> <style=cShrine>" +
						FormatPercent(Session.interactableScale, "None") + "</style>\n" +
				"<style=cStack>>Extra Item Rewards:</style> " +
						( Session.extraRewards ? "<style=cIsHealing>Enabled</style>\n" :
								"<color=#" + equipment + ">Disabled</color>\n" ) +
				"<style=cStack>>Player Income:</style> <style=cIsUtility>" +
						"+" + FormatPercent(1 - Session.incomePenalty) + " </style>\n" +
				"<style=cStack>>Enemy Bonus Health:</style> " +
						"<style=cIsVoid>" + FormatPercent(value: Session.bonusHealth - 1,
								"+" + FormatPercent(Session.additionalPlayers), "Off"
						) + "</style>\n" +
				"<style=cStack>>Teleporter Duration:</style> <sprite name=\"TP\">" +
						"<color=#" + lunar + ">+" + FormatPercent(
								Session.teleporterChargeRate * Session.additionalPlayers
						) + "</color>";
		}

		public static string FormatPercent(decimal value, string zero = null, string one = null)
		{
			string text = null;

			switch ( value )
			{
				case 0:
					text = zero;
					break;

				case -1:
				case 1:
					text = one;
					break;
			}

			return text ?? value.ToString("0.#%");
		}
	}
}
