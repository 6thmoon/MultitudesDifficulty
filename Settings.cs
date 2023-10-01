using BepInEx.Configuration;
using System.IO;
using DifficultyIcon = RoR2.UI.CurrentDifficultyIconController;
using TextMesh = RoR2.UI.HGTextMeshProUGUI;

namespace Local.Difficulty.Multitudes;

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
					acceptableValues: new AcceptableValueRange<decimal>(0.25m, 250)
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
				description: "By default, players that leave a multiplayer lobby are"
					+ " still taken into account, until they reconnect."
			).Value is false;

		Setup.forceEnable = configuration.Bind(
				section: title,
				key: "Force Enable",
				defaultValue: false,
				description: "For use with other difficulty options."
					+ " Apply the increase to player count regardless of selection."
			).Value;
	}

	[HarmonyPatch(typeof(DifficultyIcon), nameof(DifficultyIcon.Start))]
	[HarmonyPrefix]
	private static void ShowPlayerCount(Component __instance)
	{
		Transform parent = __instance.transform;
		var text = new GameObject("PlayerText").AddComponent<TextMesh>();

		Transform transform = text.transform;
		transform.SetParent(parent);

		transform.localPosition = Vector3.zero;
		transform.localRotation = new Quaternion(0, -parent.rotation.y * 0.25f, 0, 1);
		transform.localScale = Vector3.one;

		RectTransform rectangle = text.rectTransform;

		rectangle.anchorMin = Vector2.right;
		rectangle.anchorMax = Vector2.one + Vector2.right * 1.5f;
		rectangle.offsetMin = Vector2.down - Vector2.one * 0.75f;
		rectangle.offsetMax = Vector2.zero;

		text.alignment = TMPro.TextAlignmentOptions.BottomLeft;
		text.fontSize = 12;
		text.faceColor = Setup.theme;
		text.outlineWidth = 0.125f;

		int playerCount = Run.instance?.participatingPlayerCount ?? 0;
		text.SetText(FormatFraction(Session.additionalPlayers % 1 + playerCount) + "P");
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

		return description + "<style=cStack>>Player Count:</style> <style=cDeath>" +
					"+" + FormatFraction(Session.additionalPlayers) + "</style>\n" +
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

	private static string FormatFraction(decimal value)
	{
		decimal integer = Math.Truncate(value);
		string fraction = "";

		switch ( value - integer )
		{
			case 0:
				if ( integer == 0 ) return "0";
				break;

			case < 0.375m:
				fraction = "¼";
				break;

			case > 0.625m:
				fraction = "¾";
				break;

			default:
				fraction = "½";
				break;
		}

		return integer != 0 ? integer + fraction : fraction;
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
			case +1:
				text = one;
				break;
		}

		return text ?? value.ToString("0.#%");
	}
}
