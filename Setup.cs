extern alias Legacy;
using DifficultyAPI = R2API.DifficultyAPI;
using LegacyAPI = Legacy::R2API.DifficultyAPI;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using RoR2;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using UnityEngine;
using UnityEngine.SceneManagement;
using Resources = MultitudesDifficulty.Properties.Resources;

[assembly: AssemblyVersion(Local.Difficulty.Multitudes.Setup.version)]
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace Local.Difficulty.Multitudes
{
	[BepInPlugin(identifier, "MultitudesDifficulty", version)]
	public class Setup : BaseUnityPlugin
	{
		public const string identifier = "local.difficulty.multitudes";
		public const string version = "0.4.3";

		public static DifficultyIndex index;
		public static Color theme;

		private static DifficultyDef difficulty;
		public static bool eclipseMode, lobbyPlayerCount, forceEnable;

		public void Awake()
		{
			Settings.Load(Config, out eclipseMode);
			SceneManager.sceneUnloaded += _ =>
			{
				Settings.Load(Config);
				difficulty.descriptionToken = Settings.BuildDescription();

				RuleChoiceDef choice = DifficultyRule.FindChoice(difficulty.nameToken);
				if ( choice is object ) choice.tooltipBodyToken = difficulty.descriptionToken;
			};

			Color drizzle = ColorCatalog.GetColor(ColorCatalog.ColorIndex.EasyDifficulty);
			theme = new Color(r: drizzle.r, g: drizzle.b, b: drizzle.g);

			index = (DifficultyIndex)( eclipseMode ? sbyte.MaxValue : sbyte.MinValue );
			difficulty = new DifficultyDef(
					scalingValue: 
						DifficultyCatalog.GetDifficultyDef(DifficultyIndex.Hard).scalingValue,
					nameToken: "Multitudes",
					iconPath: null,
					descriptionToken: null,
					color: theme,
					serverTag: RoR2ServerTags.mod,
					countsAsHardMode: true
				);

			Texture2D texture = new Texture2D(0, 0);
			difficulty.foundIconSprite = ImageConversion.LoadImage(
					texture, eclipseMode ? Resources.eclipse : Resources.icon
				);
			difficulty.iconSprite = Sprite.Create(
					texture, new Rect(0, 0, texture.width, texture.height),
					pivot: new Vector2(texture.width / 2, texture.height / 2)
				);

			if ( ! forceEnable )
				Harmony.CreateAndPatchAll(typeof(Setup));

			Run.onRunStartGlobal += Session.Begin;
			Run.onRunDestroyGlobal += Session.End;
		}

		[HarmonyPatch(typeof(Run), nameof(Run.participatingPlayerCount), MethodType.Getter)]
		[HarmonyPrefix]
		public static bool GetPlayerCount(out int __result)
		{
			var players = PlayerCharacterMasterController.instances;
			__result = lobbyPlayerCount ? players.Count :
					players.Where( player => player.isConnected ).Count();

			return false;
		}

		private static RuleDef DifficultyRule => RuleCatalog.allRuleDefs.First();

		[HarmonyPatch(typeof(RuleCatalog), nameof(RuleCatalog.Init))]
		[HarmonyPostfix]
		private static void AddDifficulty()
		{
			RuleChoiceDef ruleChoice = DifficultyRule.AddChoice(difficulty.nameToken);

			ruleChoice.difficultyIndex = index;
			ruleChoice.tooltipNameToken = difficulty.nameToken;
			ruleChoice.tooltipNameColor = difficulty.color;
			ruleChoice.tooltipBodyToken = difficulty.descriptionToken;
			ruleChoice.serverTag = difficulty.serverTag;
			ruleChoice.sprite = difficulty.iconSprite;
			ruleChoice.globalIndex = RuleCatalog.allChoicesDefs.Count;

			RuleCatalog.allChoicesDefs.Add(ruleChoice);
			RuleCatalog.ruleChoiceDefsByGlobalName[ruleChoice.globalName] = ruleChoice;

			if ( Chainloader.PluginInfos.ContainsKey(submodule) ) SupportAPI();
			else if ( Chainloader.PluginInfos.TryGetValue(r2api, out PluginInfo plugin)
					&& plugin.Metadata.Version <= Version.Parse(oldVersion)
				) LegacySupport();
		}

		private const string oldVersion = Legacy::R2API.R2API.PluginVersion,
				submodule = DifficultyAPI.PluginGUID, r2api = Legacy::R2API.R2API.PluginGUID;

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void SupportAPI()
				=> DifficultyAPI.difficultyDefinitions[index] = difficulty;

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void LegacySupport()
		{
			if ( LegacyAPI.Loaded )
				LegacyAPI.difficultyDefinitions[index] = difficulty;
		}

		[HarmonyPatch(typeof(DifficultyCatalog), nameof(DifficultyCatalog.GetDifficultyDef))]
		[HarmonyPrefix]
		private static bool GetDifficulty(
				DifficultyIndex difficultyIndex, ref DifficultyDef __result)
		{
			if ( difficultyIndex == index )
			{
				__result = difficulty;
				return false;
			}

			return true;
		}

		private static DifficultyIndex BaseDifficulty => eclipseMode ?
				DifficultyIndex.Eclipse8 : DifficultyIndex.Hard;

		[HarmonyPatch(typeof(NetworkExtensions), nameof(NetworkExtensions.Write),
				new Type[] { typeof(UnityEngine.Networking.NetworkWriter), typeof(RuleBook) })]
		[HarmonyPrefix]
		private static void AdjustRuleBook(ref RuleBook src)
		{
			if ( src.FindDifficulty() == index )
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

			if ( __instance.selectedDifficulty == index )
				__instance.selectedDifficultyInternal = (int) BaseDifficulty;
		}

		[HarmonyPatch(typeof(Run), nameof(Run.OnSerialize))]
		[HarmonyPostfix]
		private static void RestoreDifficulty(Run __instance, int __state)
				=> __instance.selectedDifficultyInternal = __state;
	}
}
