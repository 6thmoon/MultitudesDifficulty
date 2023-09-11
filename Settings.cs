using BepInEx.Configuration;
using HarmonyLib;
using RoR2;
using RoR2.UI;
using System;
using System.IO;
using UnityEngine;

namespace Local.Difficulty.Multitudes
{
	public static class Settings
	{
		public static void Load(ConfigFile configuration) => Load(configuration, out _);

		public static void Load(ConfigFile configuration, out bool eclipse)
		{
			var percentage = new AcceptableValueRange<decimal>(0, 100);

			int order = 0; string title;
			void section(string name) => title = ++order + ". " + name;

			try { configuration.Reload(); }
			catch ( FileNotFoundException ) { configuration.Clear(); }

			section("General");

			Session.additionalPlayers = configuration.Bind(
					section: title,
					key: "Additional Player Count",
					defaultValue: 1m,
					new ConfigDescription(
						"Add this many players to the game, increasing the difficulty"
							+ " of enemies. Also affects the other options listed below.",
						acceptableValues: new AcceptableValueRange<decimal>(0.5m, 250)
				)).Value;

			eclipse = configuration.Bind(
					section: title,
					key: "Eclipse Mode",
					defaultValue: false,
					description: "Use eclipse modifiers."
						+ " Please note, this requires a restart in order to take effect."
				).Value;

			section("Advanced");

			Session.interactableScale = configuration.Bind(
					section: title,
					key: "Additional Interactables",
					defaultValue: 0m,
					new ConfigDescription(
						"Increase this percentage for more loot (i.e. chests, shrines, etc.)"
							+ " on each stage, proportional to player count.",
						acceptableValues: percentage
				)).Value / percentage.MaxValue;

			Session.extraRewards = configuration.Bind(
					section: title,
					key: "Extra Item Rewards",
					defaultValue: false,
					description: "Enable to drop additional items from the teleporter event,"
						+ " other bosses, and hidden realms."
				).Value;

			Session.incomePenalty = configuration.Bind(
					section: title,
					key: "Income Penalty",
					defaultValue: 75m,
					new ConfigDescription(
						"Gold is typically split between all players. Lower this value to"
							+ " lessen this effect, increasing player income.",
						acceptableValues: percentage
				)).Value / percentage.MaxValue;

			Session.bonusHealth = configuration.Bind(
					section: title,
					key: "Bonus Health",
					defaultValue: 0m,
					new ConfigDescription(
						"Certain enemies receive bonus health in multiplayer. Reduce the"
							+ " amount granted to teleporter bosses and unique encounters.",
						acceptableValues: percentage
				)).Value / percentage.MaxValue;

			Session.teleporterChargeRate = configuration.Bind(
					section: title,
					key: "Teleporter Duration",
					defaultValue: 0m,
					new ConfigDescription(
						"The extent at which player count is considered when determining"
							+ " charge rate for holdout zones. Not recommended.",
						acceptableValues: percentage
				)).Value / percentage.MaxValue;

			section("Other");

			Setup.lobbyPlayerCount = configuration.Bind(
					section: title,
					key: "Ignore Disconnected Players",
					defaultValue: false,
					description: "By default, players that leave a multiplayer lobby are still"
						+ " taken into account, until they reconnect."
				).Value is false;

			Setup.forceEnable = configuration.Bind(
					section: title,
					key: "Force Enable",
					defaultValue: false,
					description: "For use with other difficulty options."
						+ " Apply the increase to player count regardless of selection."
				).Value;
		}

		[HarmonyPatch(typeof(TimerText), nameof(TimerText.Awake))]
		[HarmonyPatch(typeof(InfiniteTowerWaveCounter),
				nameof(InfiniteTowerWaveCounter.OnEnable))]
		[HarmonyPrefix]
		private static void ShowPlayerCount(Component __instance)
		{
			const string name = "PlayerText";
			Transform other = __instance.transform;

			if ( __instance is InfiniteTowerWaveCounter )
			{
				if ( other.Find(name) is object ) return;
				else other = other.Find("WaveText");
			}

			var text = new GameObject(name).AddComponent<HGTextMeshProUGUI>();

			Transform transform = text.transform;
			transform.SetParent(other.parent);

			transform.localPosition = other.localPosition;
			transform.localRotation = new Quaternion(0, -other.rotation.y * 0.25f, 0, 1);
			transform.localScale = other.localScale;

			RectTransform rectangle = text.rectTransform;

			rectangle.anchorMax = Vector2.one;
			rectangle.anchorMin = Vector2.zero;
			rectangle.offsetMax = Vector2.zero;
			rectangle.offsetMin = new Vector2(-1.5f, -1);

			text.alignment = TMPro.TextAlignmentOptions.BottomLeft;
			text.fontSize = 12;
			text.faceColor = Setup.theme;
			text.outlineWidth = 0.125f;

			string value = $"{ Run.instance.participatingPlayerCount }";
			if ( Session.additionalPlayers % 1 > 0 ) value += "½";

			text.SetText(value + "P");
		}

		public static string BuildDescription(bool verbose = true)
		{
			string description = "";
			if ( verbose )
			{
				description = "For those who wish to face vast hordes of enemies alone. " +
							"Multiplayer difficulty levels are in effect.\n\n" +
					"<style=cStack>>Base Difficulty:</style> <style=cSub>" +
							( Setup.eclipseMode ? "Eclipse" : "Monsoon" ) + "</style>\n";
			}

			string lunar = ColorCatalog.GetColorHexString(ColorCatalog.ColorIndex.LunarItem),
				  equipment = ColorCatalog.GetColorHexString(ColorCatalog.ColorIndex.Equipment);

			return description + "<style=cStack>>Player Count:</style> " +
						"<style=cDeath>+" + Math.Floor(Session.additionalPlayers) + (
								Session.additionalPlayers % 1 > 0 ? "½" : "" ) + "</style>\n" +
				"<style=cStack>>Additional Interactables:</style> <style=cShrine>" +
						FormatPercent(Session.interactableScale, "None") + "</style>\n" +
				"<style=cStack>>Extra Item Rewards:</style> " +
						( Session.extraRewards ? "<style=cIsHealing>Enabled</style>\n" :
								"<color=#" + equipment + ">Disabled</color>\n" ) +
				"<style=cStack>>Player Income:</style> <style=cIsUtility>" +
						"+" + FormatPercent(1 - Session.incomePenalty) + " </style>\n" +
				"<style=cStack>>Enemy Bonus Health:</style> <style=cIsVoid>" +
						FormatPercent(Session.bonusHealth - 1, "+100%", "Off") + "</style>\n" +
				"<style=cStack>>Teleporter Duration:</style> <sprite name=\"TP\">" +
						"<color=#" + lunar + ">+" +
								FormatPercent(Session.teleporterChargeRate) + "</color>";
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
