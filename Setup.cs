extern alias Legacy;
global using HarmonyLib;
global using RoR2;
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using UnityEngine;
global using Console = System.Console;
using BepInEx;
using BepInEx.Bootstrap;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using DifficultyAPI = R2API.DifficultyAPI;
using LegacyAPI = Legacy::R2API.DifficultyAPI;
using Version = System.Version;
using Resources = MultitudesDifficulty.Properties.Resources;

[assembly: AssemblyVersion(Local.Difficulty.Multitudes.Setup.version)]
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace Local.Difficulty.Multitudes;

[BepInPlugin(identifier, "MultitudesDifficulty", version)]
public class Setup : BaseUnityPlugin
{
	public const string identifier = "local.difficulty.multitudes";
	public const string version = "1.1.0";

	internal static DifficultyIndex index;
	internal static Color theme;

	private static DifficultyDef difficulty;
	private static RuleChoiceDef other, choice;

	public static bool eclipseMode, lobbyPlayerCount, forceEnable;

	protected void Awake()
	{
		Settings.Load(Config, out eclipseMode);
		SceneManager.sceneUnloaded += _ =>
		{
			if ( choice is null ) return;
			Settings.Load(Config);

			choice.tooltipBodyToken = difficulty.descriptionToken = Settings.BuildDescription();
			choice.excludeByDefault = forceEnable;
		};

		Color drizzle = ColorCatalog.GetColor(ColorCatalog.ColorIndex.EasyDifficulty);
		theme = new Color(r: drizzle.r, g: drizzle.b, b: drizzle.g);

		index = (DifficultyIndex)( eclipseMode ? sbyte.MaxValue : sbyte.MinValue );
		difficulty = new DifficultyDef(
				DifficultyCatalog.GetDifficultyDef(DifficultyIndex.Hard).scalingValue,
				nameToken: "Multitudes", iconPath: null, descriptionToken: null,
				color: theme, serverTag: RoR2ServerTags.mod, countsAsHardMode: true
			);

		const int size = 256, scale = 2;
		Texture2D texture = new(size, size, TextureFormat.ARGB32, scale + 1, linear: false);

		difficulty.foundIconSprite = ImageConversion.LoadImage(
				texture, eclipseMode ? Resources.eclipse : Resources.icon);
		difficulty.iconSprite = Sprite.Create(
				texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);

		Harmony.CreateAndPatchAll(typeof(Setup));

		Run.onRunStartGlobal += Session.Begin;
		Run.onRunDestroyGlobal += Session.End;
	}

	[HarmonyPatch(typeof(Run), nameof(Run.participatingPlayerCount), MethodType.Getter)]
	[HarmonyPrefix]
	internal static bool GetPlayerCount(out int __result)
	{
		var players = PlayerCharacterMasterController.instances;
		__result = lobbyPlayerCount ?
				players.Count : players.Where( player => player.isConnected ).Count();

		return false;
	}

	[HarmonyPatch(typeof(RuleCatalog), nameof(RuleCatalog.Init))]
	[HarmonyPostfix]
	private static void AddDifficulty()
	{
		RuleDef difficulties = RuleCatalog.allRuleDefs.First(
				( RuleDef definition ) => definition.globalName is "Difficulty");
		choice = difficulties.AddChoice(difficulty.nameToken);

		choice.difficultyIndex = index;
		choice.tooltipNameToken = difficulty.nameToken;
		choice.tooltipNameColor = difficulty.color;
		choice.serverTag = difficulty.serverTag;
		choice.sprite = difficulty.iconSprite;
		choice.globalIndex = RuleCatalog.allChoicesDefs.Count;

		RuleCatalog.allChoicesDefs.Add(choice);
		RuleCatalog.ruleChoiceDefsByGlobalName[choice.globalName] = choice;

		foreach ( RuleChoiceDef definition in difficulties.choices )
			if ( definition.difficultyIndex == ( eclipseMode ?
					DifficultyIndex.Eclipse8 : DifficultyIndex.Hard ))
			{
				other = definition;
				break;
			}

		CheckCompatibility(Chainloader.PluginInfos);
	}

	private static void CheckCompatibility(Dictionary<string, PluginInfo> info)
	{
		const string version = Legacy::R2API.R2API.PluginVersion,
				api = Legacy::R2API.R2API.PluginGUID, module = DifficultyAPI.PluginGUID;

		if ( info.ContainsKey(module) ) SupportAPI();
		else if ( info.TryGetValue(api, out PluginInfo plugin) &&
				plugin.Metadata.Version <= Version.Parse(version)
			) LegacySupport();

		[MethodImpl(MethodImplOptions.NoInlining)]
		static void SupportAPI()
				=> DifficultyAPI.difficultyDefinitions[index] = difficulty;

		[MethodImpl(MethodImplOptions.NoInlining)]
		static void LegacySupport()
		{
			if ( LegacyAPI.Loaded )
				LegacyAPI.difficultyDefinitions[index] = difficulty;
		}
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

	[HarmonyPatch(typeof(NetworkRuleBook), nameof(NetworkRuleBook.OnSerialize))]
	[HarmonyPatch(typeof(RunReport), nameof(RunReport.Write))]
	[HarmonyILManipulator]
	private static void AdjustRuleBook(ILContext context)
	{
		ILCursor cursor = new(context);
		MethodInfo method = typeof(RoR2.NetworkExtensions).GetMethod(
				nameof(NetworkExtensions.Write), [ typeof(NetworkWriter), typeof(RuleBook) ]);

		if ( cursor.TryGotoNext(( Instruction i ) => i.MatchCall(method)) )
		{
			cursor.EmitDelegate(( RuleBook original ) =>
			{
				if ( original.FindDifficulty() != index )
					return original;

				var ruleBook = new RuleBook();

				ruleBook.Copy(original);
				ruleBook.ApplyChoice(other);

				return ruleBook;
			});
		}
		else Console.WriteLine("Unable to modify rulebook serialization.");
	}

	[HarmonyPatch(typeof(Run), nameof(Run.OnSerialize))]
	[HarmonyILManipulator]
	private static void SendBaseIndex(ILContext context)
	{
		ILCursor cursor = new(context);
		FieldInfo field = typeof(Run).GetField(
				nameof(Run.selectedDifficultyInternal), AccessTools.all);

		while ( cursor.TryGotoNext(MoveType.After, ( Instruction i ) => i.MatchLdfld(field)) )
		{
			cursor.EmitDelegate(( DifficultyIndex value ) =>
			{
				if ( value != index ) return value;
				else return other.difficultyIndex;
			});
		}

		if ( cursor.Index is 0 )
			Console.WriteLine("Unable to map index of difficulty.");
	}
}
