using System.Linq;
using System.Threading.Tasks;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using FollowBot.SimpleEXtensions;
using log4net;

namespace FollowBot.Tasks
{
    internal class TrialPickerTask : ITask
    {
        private readonly ILog Log = Logger.GetLoggerInstanceForType();
        public string Author => "Helpless";

        public string Description => "Trial picker task";

        public string Name => "TrialPicker";

        public string Version => "0.0.0.0";

        public Task<LogicResult> Logic(Logic logic)
        {
            return Task.FromResult(LogicResult.Unprovided);
        }

        public MessageResult Message(Message message)
        {
            return MessageResult.Unprocessed;
        }

        public async Task<bool> Run()
        {
            if (!LokiPoe.LabyrinthTrialAreaIds.Contains(LokiPoe.CurrentWorldArea.Id)) return false;
            var me = LokiPoe.Me;
            if (me.IsAscendencyTrialCompleted(LokiPoe.CurrentWorldArea.Id)) return false;

            var trial = LokiPoe.ObjectManager.Objects.FirstOrDefault(x =>
                x.Metadata.Contains("LabyrinthTrialPlaque"));
            if (trial == null) return false;
            Log.Debug($"[{Name}] Find trial : [{trial.Name}]");
            if (!trial.PathExists() || me.Position.Distance(trial.Position) >= 50) return false;
            await trial.WalkablePosition().ComeAtOnce();
            await PlayerAction.Interact(trial);
            return true;
        }

        public void Start()
        {
            Log.InfoFormat("[{0}] Task Loaded.", Name);
        }

        public void Stop()
        {
        }

        public void Tick()
        {
        }
    }
}