using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework.Graphics;
using TyriaPlanner.Hud.Api;
using TyriaPlanner.Hud.Services;
using TyriaPlanner.Hud.Settings;
using TyriaPlanner.Hud.Ui;
namespace TyriaPlanner.Hud
{
    [Export(typeof(Module))]
    public sealed class TyriaPlannerHudModule : Module
    {
        internal static readonly Blish_HUD.Logger Logger = Blish_HUD.Logger.GetLogger<TyriaPlannerHudModule>();
        private readonly ContentsManager _contents;
        private ModuleSettings _settings;
        private ApiClient _api;
        private ToastStack _stack;
        private NotificationService _notify;
        private NotificationHistory _history;
        private PollingService _poller;
        private WeeklyResetReminder _resetReminder;
        private MenuWindow _menu;
        private CornerIcon _cornerIcon;
        private Texture2D _iconTexture;
        [ImportingConstructor]
        public TyriaPlannerHudModule([Import("ModuleParameters")] ModuleParameters parameters)
            : base(parameters)
        {
            _contents = parameters.ContentsManager;
        }
        protected override void DefineSettings(SettingCollection root)
        {
            _settings = new ModuleSettings(root);
        }
        protected override async Task LoadAsync()
        {
            _api = new ApiClient();
            _history = new NotificationHistory();
            _stack = new ToastStack(_settings);
            _notify = new NotificationService(_stack, _settings, _history);
            _poller = new PollingService(_api, _settings, _notify);
            _resetReminder = new WeeklyResetReminder(_settings, _stack);
            _menu = new MenuWindow(_api, _settings, _notify, _history);
            await Task.CompletedTask;
        }
        protected override void OnModuleLoaded(System.EventArgs e)
        {
            base.OnModuleLoaded(e);
            try
            {
                _iconTexture = _contents.GetTexture("icon.png");
            }
            catch (System.Exception ex)
            {
                Logger.Warn(ex, "Failed to load corner icon Â· falling back to text-only.");
            }
            _cornerIcon = new CornerIcon
            {
                Icon = _iconTexture,
                IconName = "Tyria Planner",
                Priority = 7_111_111,
            };
            _cornerIcon.Click += (_, __) => _menu?.Toggle();
            _poller.Start();
            _resetReminder.Start();
            Logger.Info("Tyria Planner HUD loaded.");
        }
        protected override void Unload()
        {
            _poller?.Dispose();
            _resetReminder?.Dispose();
            _stack?.Clear();
            _menu?.Dispose();
            _cornerIcon?.Dispose();
            _api?.Dispose();
        }
        protected override void Update(Microsoft.Xna.Framework.GameTime gameTime)
        {
        }
    }
}
