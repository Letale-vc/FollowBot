using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Coroutine;
using DreamPoeBot.Loki.Game;
using log4net;
using System;
using System.Threading.Tasks;
using static DreamPoeBot.Loki.Game.LokiPoe.InGameState;

public class RuntimeCode
{
    class InteractQuestObject
    {
        public string ActId { get; }
        public string ObjectName { get; }
        public string[] IncludeQuestItem { get; }
        public InteractQuestObject(string actId, string objectName, string[] includeQuestItem)
        {
            ActId = actId;
            ObjectName = objectName;
            IncludeQuestItem = includeQuestItem;
        }
        public InteractQuestObject(string actId, string objectName)
        {
            ActId = actId;
            ObjectName = objectName;
            IncludeQuestItem = new string[] { };
        }
    }
    private static readonly ILog Log = Logger.GetLoggerInstanceForType();

    private Coroutine _coroutine;

    public string Name = "TEST";
    public void Execute()
    {
        ExilePather.Reload();
        //PluginManager.Start();
        //RoutineManager.Start();
        PlayerMoverManager.Start();
        // Create the coroutine
        _coroutine = new Coroutine(() => MainCoroutine());

        // Run the coroutine while it's not finished.
        while (!_coroutine.IsFinished)
        {
            try
            {
                _coroutine.Resume();
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("[Execute] {0}.", ex);
                break;
            }
        }

        // Cleanup the coroutine if it already exists
        if (_coroutine != null)
        {
            //PluginManager.Stop();
            //RoutineManager.Stop();
            PlayerMoverManager.Stop();
            _coroutine.Dispose();
            _coroutine = null;
        }
    }

    private async Task<bool> UserCoroutine()
    {
        //HookManager.InstallHook();

        Log.InfoFormat("UserCoroutine ");
        LokiPoe.ProcessHookManager.Enable();

        NpcDialogUi.Converse("Take Sewer Keys", true);
        await Coroutine.Sleep(300);
        var rewardInvenoryControls = RewardUi.InventoryControls;
        var rewardControl = rewardInvenoryControls[0];
        var reward = rewardControl.Inventory.Items[0];
        var result = rewardControl.FastMoveReward(reward.LocalId, true);

        if (result != FastMoveResult.None)
        {
            Log.Error($"[NpcHelper][TakeReward]  cannot take reward [{reward.FullName}]\n ERROR: {result}");
            return false;
        }

        LokiPoe.ProcessHookManager.Disable();


        //HookManager.RemoveHook();

        return false;
    }

    private async Task MainCoroutine()
    {
        while (true)
        {
            if (LokiPoe.IsInLoginScreen)
            {
                // Offload auto login logic to a plugin.
                foreach (var plugin in PluginManager.EnabledPlugins)
                {
                    await plugin.Logic(new Logic("login_screen"));
                }
            }
            else if (LokiPoe.IsInCharacterSelectionScreen)
            {

                // Offload character selection logic to a plugin.
                foreach (var plugin in PluginManager.EnabledPlugins)
                {
                    await plugin.Logic(new Logic("character_selection"));
                }
            }
            else if (LokiPoe.IsInGame)
            {
                // Execute user logic until false is returned.
                if (!await UserCoroutine())
                    break;
            }
            else
            {
                // Most likely in a loading screen, which will cause us to block on the executor, 
                // but just in case we hit something else that would cause us to execute...
                await Coroutine.Sleep(5000);
                continue;
            }

            // End of the tick.
            await Coroutine.Yield();
        }
    }
}