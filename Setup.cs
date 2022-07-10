extern alias Legacy;
using DifficultyAPI = R2API.DifficultyAPI;
using LegacyAPI = Legacy::R2API.DifficultyAPI;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using RoR2;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using UnityEngine;
using Resources = MultitudesDifficulty.Properties.Resources;

[assembly: AssemblyVersion(Local.Difficulty.Multitudes.Setup.versionNumber)]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
		// Allow private member access via publicized assemblies.

namespace Local.Difficulty.Multitudes
{
	[BepInPlugin("local.difficulty.multitudes", "MultitudesDifficulty", versionNumber)]
	public class Setup : BaseUnityPlugin
	{
		public const string versionNumber = "0.3.5";

		public static DifficultyIndex multitudesIndex = DifficultyIndex.Invalid;
		public static Color colorTheme;

		private static DifficultyDef multitudesDifficulty;

		public void Awake()
		{
			LoadConfiguration();

			Color drizzle = ColorCatalog.GetColor(ColorCatalog.ColorIndex.EasyDifficulty);
			colorTheme = new Color(r: drizzle.r, g: drizzle.b, b: drizzle.g);

			multitudesDifficulty = new DifficultyDef(
					scalingValue: 
						DifficultyCatalog.GetDifficultyDef(DifficultyIndex.Hard).scalingValue,
					nameToken: "Multitudes",
					iconPath: null,
					descriptionToken: BuildDescription(flavorText: true),
					color: colorTheme,
					serverTag: RoR2ServerTags.mod,
					countsAsHardMode: true
				);

			Texture2D texture = new Texture2D(0, 0);

			multitudesDifficulty.foundIconSprite = ImageConversion.LoadImage(
					texture, Session.eclipseMode ? Resources.eclipse : Resources.icon);
			multitudesDifficulty.iconSprite = Sprite.Create(
					texture, new Rect(0, 0, texture.width, texture.height),
					pivot: new Vector2(texture.width / 2, texture.height / 2)
				);

			if ( !Session.forceEnable )
				Harmony.CreateAndPatchAll(typeof(Setup));

			Run.onRunStartGlobal += Session.Begin;
			Run.onRunDestroyGlobal += Session.End;
		}

		private void LoadConfiguration()
		{
			string sectionTitle; int sectionNumber = 0;
			void section(string title) => sectionTitle = ++sectionNumber + ". " + title;

			section("General");

			ConfigEntry<byte> configEntry = Config.Bind<byte>(
					section: sectionTitle,
					key: "Additional Player Count",
					defaultValue: 1,
					description: "Higher values increase difficulty. Although more enemies"
						+ " will spawn, less money is awarded and purchase costs are increased."
				);

			if ( configEntry.Value == 0 ) configEntry.Value = 1;
			Session.additionalPlayers = configEntry.Value;

			Session.eclipseMode = Config.Bind(
					section: sectionTitle,
					key: "Eclipse Mode",
					defaultValue: false,
					description: "Use eclipse modifiers. Not for the faint of heart."
				).Value;

			section("Advanced");

			Session.interactableScale = Config.Bind<decimal>(
					section: sectionTitle,
					key: "Additional Interactables",
					defaultValue: 25,
					new ConfigDescription(
						"Increasing this percentage results in additional interactables"
						+ " (i.e. chests, shrines, & other loot), relative to player count.",
							new AcceptableValueRange<decimal>(0, 100))
				).Value / 100;

			Session.extraRewards = Config.Bind(
					section: sectionTitle,
					key: "Extra Item Rewards",
					defaultValue: false,
					description: "Enable to drop additional items during the teleporter event,"
						+ " Void Fields, and the Simulacrum."
				).Value;

			Session.teleporterChargeRate = Config.Bind(
					section: sectionTitle,
					key: "Teleporter Duration",
					defaultValue: (decimal) 12.5,
					new ConfigDescription(
						"The extent at which player count is considered when determining charge"
						+ " rate for holdout zones. Higher values result in slower charge.",
							new AcceptableValueRange<decimal>(0, 100))
				).Value / 100;

			section("Other");

			Session.forceEnable = Config.Bind(
					section: sectionTitle,
					key: "Force Enable",
					defaultValue: false,
					description: "Force player count adjustment regardless of difficulty"
						+ " selection. For use with other custom difficulty modes."
				).Value;
		}

		public static string BuildDescription(bool flavorText = false)
		{
			return ( flavorText ? "For those who wish to face vast hordes of enemies alone. " +
						"Multiplayer difficulty levels are in effect.\n\n" : "" ) +
				( Session.forceEnable ? "" :
						"<style=cStack>>Base Difficulty:</style> <style=cSub>" +
						( Session.eclipseMode ? "Eclipse" : "Monsoon" ) + "</style>\n" ) +
				"<style=cStack>>Player Count:</style> " +
						$"<style=cDeath>+{ Session.additionalPlayers }</style>\n" +
				"<style=cStack>>Additional Interactables:</style> " +
						"<style=cShrine>" + ( Session.interactableScale > 0 ?
						Session.interactableScale.ToString("0.#%") : "None" ) + "</style>\n" +
				"<style=cStack>>Extra Item Rewards:</style> " +
						( Session.extraRewards ? "<style=cIsHealing>Enabled</style>\n" :
						"<color=#FF8000>Disabled</color>\n" ) +				// Equipment color.
				"<style=cStack>>Teleporter Duration:</style> <sprite name=\"TP\">" +
						"<color=#307FFF>+" + ( Session.teleporterChargeRate *	// Lunar color.
						Session.additionalPlayers ).ToString("0.#%") + "</color>";
		}

		private static RuleDef DifficultyRule => RuleCatalog.allRuleDefs.First();

		[HarmonyPatch(typeof(RuleCatalog), nameof(RuleCatalog.Init))]
		[HarmonyPostfix]
		private static void AddDifficulty()
		{
			RuleChoiceDef ruleChoice =
					DifficultyRule.AddChoice(multitudesDifficulty.nameToken);

			// Arbitrary value - should match on host/client, large enough to prevent conflicts.
			multitudesIndex = DifficultyIndex.Invalid + ( Session.eclipseMode ? 1 : -1 ) * 0x7F;

			ruleChoice.difficultyIndex = multitudesIndex;
			ruleChoice.tooltipNameToken = multitudesDifficulty.nameToken;
			ruleChoice.tooltipNameColor = multitudesDifficulty.color;
			ruleChoice.tooltipBodyToken = multitudesDifficulty.descriptionToken;
			ruleChoice.serverTag = multitudesDifficulty.serverTag;
			ruleChoice.sprite = multitudesDifficulty.iconSprite;
			ruleChoice.globalIndex = RuleCatalog.allChoicesDefs.Count;

			RuleCatalog.allChoicesDefs.Add(ruleChoice);
			RuleCatalog.ruleChoiceDefsByGlobalName[ruleChoice.globalName] = ruleChoice;

			if ( Chainloader.PluginInfos.ContainsKey(module) ) SupportAPI();
			else if ( Chainloader.PluginInfos.TryGetValue(r2api, out PluginInfo plugin)
					&& plugin.Metadata.Version < Version.Parse(newVersion)
				) LegacySupport();
		}

		private const string r2api = Legacy::R2API.R2API.PluginGUID,
				newVersion = R2API.R2API.PluginVersion, module = DifficultyAPI.PluginGUID;

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void SupportAPI()
				=> DifficultyAPI.difficultyDefinitions[multitudesIndex] = multitudesDifficulty;

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void LegacySupport()
		{
			if ( LegacyAPI.Loaded )
				LegacyAPI.difficultyDefinitions[multitudesIndex] = multitudesDifficulty;
		}

		[HarmonyPatch(typeof(DifficultyCatalog), nameof(DifficultyCatalog.GetDifficultyDef))]
		[HarmonyPrefix]
		private static bool GetDifficulty(DifficultyIndex difficultyIndex, 
				ref DifficultyDef __result)
		{
			if ( difficultyIndex == multitudesIndex )
			{
				__result = multitudesDifficulty;
				return false;		// Skip original method execution.
			}

			return true;
		}

		private static DifficultyIndex BaseDifficulty => Session.eclipseMode ?
				DifficultyIndex.Eclipse8 : DifficultyIndex.Hard;

		[HarmonyPatch(typeof(NetworkExtensions), nameof(NetworkExtensions.Write),
				new Type[] { typeof(UnityEngine.Networking.NetworkWriter), typeof(RuleBook) })]
		[HarmonyPrefix]
		private static void AdjustRuleBook(ref RuleBook src)
		{
			if ( src.FindDifficulty() == multitudesIndex )
			{
				RuleBook ruleBook = new RuleBook();
				ruleBook.Copy(src);

				ruleBook.ApplyChoice(
						DifficultyRule.FindChoice(BaseDifficulty.ToString())
					);
				src = ruleBook;
			}
		}

		[HarmonyPatch(typeof(Run), nameof(Run.OnSerialize))]
		[HarmonyPrefix]
		private static void SendBaseDifficulty(Run __instance, ref int __state)
		{
			__state = __instance.selectedDifficultyInternal;

			if ( __instance.selectedDifficulty == multitudesIndex )
				__instance.selectedDifficultyInternal = (int) BaseDifficulty;
		}

		[HarmonyPatch(typeof(Run), nameof(Run.OnSerialize))]
		[HarmonyPostfix]
		private static void RestoreDifficulty(Run __instance, int __state)
				=> __instance.selectedDifficultyInternal = __state;
	}
}