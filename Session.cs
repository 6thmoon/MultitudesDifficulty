using EntityStates.AurelioniteHeart;
using EntityStates.FalseSonBoss;
using EntityStates.Geode;
using EntityStates.Missions.GeodeSecretMission;
using EntityStates.SolusHeart.Death;
using Acrid = EntityStates.Croco;
using VoidSeed = EntityStates.VoidCamp;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using System.Reflection;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

namespace Local.Difficulty.Multitudes;

public static class Session
{
	public static decimal players, interactableScale;
	public static bool multiply, extraRewards;
	public static decimal incomePenalty, bonusHealth, teleporterChargeRate;

	private static Harmony instance = null;
	private static bool broadcast;

	internal static void Begin(Run thisRun)
	{
		if ( instance is null && NetworkServer.active &&
				( Setup.forceEnable || thisRun.selectedDifficulty == Setup.index ))
		{
			instance = Harmony.CreateAndPatchAll(typeof(Settings));
			instance.PatchAll(typeof(Session));
		}
	}

	internal static void End(object _)
	{
		broadcast = false;

		instance?.UnpatchSelf();
		instance = null;
	}

	public static decimal GetAdditionalPlayers(decimal limit = decimal.One)
	{
		Setup.GetPlayerCount(out int count);

		if ( multiply )
			return count * ( players - 1 ) * limit;
		else if ( count > 0 )
			return players * limit;
		else return 0;
	}

	[HarmonyPatch(typeof(SurvivorPodController), nameof(SurvivorPodController.OnPassengerExit))]
	[HarmonyPatch(typeof(Acrid.WakeUp), nameof(Acrid.WakeUp.OnExit))]
	[HarmonyPatch(typeof(InfiniteTowerRun), nameof(InfiniteTowerRun.OnSafeWardActivated))]
	[HarmonyPostfix]
	private static void GreetUser()
	{
		if ( broadcast ) return;
		string text = "Multitudes Enabled\n";
		broadcast = true;

		if ( RoR2Application.isInMultiPlayer )
			text += Settings.BuildDescription(verbose: false);
		else if ( Setup.eclipseMode && Random.value < 0.15 )
			text = "Good luck.";
		else return;

		text = "<color=#" + ColorUtility.ToHtmlStringRGB(Setup.theme) + ">" + text + "</color>";
		Chat.SendBroadcastChat(new Chat.SimpleChatMessage { baseToken = text });
	}

	[HarmonyPatch(typeof(SceneDirector), nameof(SceneDirector.Start))]
	[HarmonyILManipulator]
	private static void AdjustInteractableCredits(ILContext context)
	{
		InsertHook(context, typeof(Session).GetField(nameof(interactableScale)),
				typeof(Run).GetProperty(nameof(Run.participatingPlayerCount)).GetMethod);
	}

	[HarmonyPatch(typeof(BossGroup), nameof(BossGroup.DropRewards))]
	[HarmonyILManipulator]
	private static void AdjustBossRewards(ILContext context)
	{
		ApplyPlayerAdjustment(context);
	}

	[HarmonyPatch(typeof(TeamManager), nameof(TeamManager.GiveTeamMoney),
			[ typeof(TeamIndex), typeof(uint) ])]
	[HarmonyILManipulator]
	private static void AdjustPlayerIncome(ILContext context)
	{
		InsertHook(context, typeof(Session).GetField(nameof(incomePenalty)),
				typeof(Run).GetProperty(nameof(Run.livingPlayerCount)).GetMethod);
	}

	internal static void InsertHook(ILContext context, FieldInfo field, MethodInfo method)
	{
		ILCursor cursor = new(context);
		string identifier = context.Method.FullName;

		while ( cursor.TryGotoNext(MoveType.After, i => i.MatchCallOrCallvirt(method)) )
		{
			int index = int.MinValue;
			while ( OpCodes.Conv_R4 != cursor.Next.OpCode )
			{
				if ( index < 0 )
					cursor.TryGotoNext(( Instruction i ) => i.MatchStloc(out index));

				if ( ! cursor.TryGotoNext(MoveType.After, i => i.MatchLdloc(index)) )
				{
					Console.WriteLine("Unable to insert hook.\n\t" + identifier);
					return;
				}
			}

			++cursor.Index;

			cursor.Emit(OpCodes.Ldsfld, field);
			cursor.Emit(OpCodes.Call, typeof(Session).GetMethod(nameof(GetAdditionalPlayers)));
			cursor.Emit(OpCodes.Call, typeof(decimal).GetMethod(nameof(decimal.ToSingle)));
			cursor.Emit(OpCodes.Add);
		}

		if ( cursor.Index is 0 )
			Console.WriteLine("No match found.\n\t" + identifier);
	}

	[HarmonyPatch(typeof(CombatDirector), nameof(CombatDirector.Spawn))]
	[HarmonyPatch(typeof(CharacterMaster), nameof(CharacterMaster.ScaleDifficultyAsBoss))]
	[HarmonyILManipulator]
	private static void AddBonusHealth(ILContext context, MethodBase __originalMethod)
	{
		if ( __originalMethod.DeclaringType == typeof(CombatDirector) )
		{
			MethodReference target = default;
			var sequence = new Func<Instruction, bool>[]
			{
				( Instruction i ) => i.MatchLdftn(out target),
				( Instruction i ) => i.MatchNewobj(out _),
				( Instruction i ) => i.MatchStfld<DirectorSpawnRequest>(
						nameof(DirectorSpawnRequest.onSpawnedServer))
			};

			if ( new ILCursor(context).TryGotoNext(sequence) )
			{
				instance.Patch(target.ResolveReflection(), ilmanipulator:
						new HarmonyMethod(MethodBase.GetCurrentMethod() as MethodInfo));
				return;
			}
		}

		InsertHook(context, typeof(Session).GetField(nameof(bonusHealth)),
				typeof(Run).GetProperty(nameof(Run.livingPlayerCount)).GetMethod);
	}

	[HarmonyPatch(typeof(HoldoutZoneController), nameof(HoldoutZoneController.DoUpdate))]
	[HarmonyILManipulator]
	private static void AdjustChargeRate(ILContext context)
	{
		MethodInfo method = typeof(HoldoutZoneController).GetMethod(
				nameof(HoldoutZoneController.CountLivingPlayers), AccessTools.all);
		InsertHook(context, typeof(Session).GetField(nameof(teleporterChargeRate)), method);
	}

	[HarmonyPatch(typeof(EscapeSequenceController),
			nameof(EscapeSequenceController.BeginEscapeSequence))]
	[HarmonyPrefix]
	private static void IncreaseCountdown(EscapeSequenceController __instance)
	{
		decimal other = GetAdditionalPlayers(teleporterChargeRate);
		Setup.GetPlayerCount(out int count);

		if ( other > 0 )
			__instance.countdownDuration *= decimal.ToSingle(1 + other / count);
	}

	[HarmonyPatch(typeof(Run), nameof(Run.RecalculateDifficultyCoefficentInternal))]
	[HarmonyPatch(typeof(CombatDirector.DirectorMoneyWave),
			nameof(CombatDirector.DirectorMoneyWave.Update))]
	[HarmonyPatch(typeof(OnCollisionEventController),
			nameof(OnCollisionEventController.OnTriggerStay))]
	[HarmonyILManipulator]
	private static void IncreaseDifficultyCoefficient(ILContext context)
	{
		InsertHook(context, typeof(decimal).GetField(nameof(decimal.One)),
				typeof(Run).GetProperty(nameof(Run.participatingPlayerCount)).GetMethod);
	}

	[HarmonyPatch(typeof(ArenaMissionController), nameof(ArenaMissionController.EndRound))]
	[HarmonyPatch(typeof(InfiniteTowerWaveController),
			nameof(InfiniteTowerWaveController.DropRewards))]
	[HarmonyPatch(typeof(VoidSeed.Deactivate), nameof(VoidSeed.Deactivate.OnEnter))]
	[HarmonyPatch(typeof(HalcyoniteShrineInteractable),
			nameof(HalcyoniteShrineInteractable.DropRewards))]
	[HarmonyPatch(typeof(SkyJumpDeathState), nameof(SkyJumpDeathState.GiveColossusItem))]
	[HarmonyPatch(typeof(AurelioniteHeartActivationState),
			nameof(AurelioniteHeartActivationState.FixedUpdate))]
	[HarmonyPatch(typeof(GeodeSecretMissionRewardState),
			nameof(GeodeSecretMissionRewardState.DropRewards))]
	[HarmonyPatch(typeof(GeodeShatter), nameof(GeodeShatter.OnEnter))]
	[HarmonyPatch(typeof(SolusHeartFinaleSequence.Death),
			nameof(SolusHeartFinaleSequence.Death.OnEnter))]
	[HarmonyILManipulator]
	private static void ApplyPlayerAdjustment(ILContext context)
	{
		var cursor = new ILCursor(context);
		MethodInfo method = 
				typeof(Run).GetProperty(nameof(Run.participatingPlayerCount)).GetMethod;

		while ( cursor.TryGotoNext(MoveType.After, i => i.MatchCallOrCallvirt(method)) )
		{
			cursor.Emit(OpCodes.Ldsfld, typeof(decimal).GetField(nameof(decimal.One)));
			cursor.Emit(OpCodes.Call, typeof(Session).GetMethod(nameof(GetAdditionalPlayers)));
			cursor.Emit(OpCodes.Call, typeof(decimal).GetMethod(nameof(decimal.ToInt32)));

			cursor.Emit(OpCodes.Ldsfld, typeof(Session).GetField(nameof(extraRewards)));
			cursor.Emit(OpCodes.Mul);

			cursor.Emit(OpCodes.Add);
		}

		if ( cursor.Index is 0 )
			Console.WriteLine("No match found.\n\t" + context.Method.FullName);
	}

	[Obsolete("If needed, refer to `" + nameof(GetAdditionalPlayers) + "` instead.")]
	public static readonly decimal additionalPlayers = 0;
}
