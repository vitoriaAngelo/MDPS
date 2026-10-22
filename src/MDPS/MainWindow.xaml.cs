using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using AionMeter.Core.Capture;
using AionMeter.Core.Combat;
using AionMeter.Core.Demo;
using AionMeter.Core.Events;
using AionMeter.Core.Game;
using AionMeter.Core.Storage;
using AionMeter.Core.Updates;

namespace MDPS;

public partial class MainWindow : Window
{
    private readonly GameData _data = GameData.Load(Path.Combine(AppContext.BaseDirectory, "data"));
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private readonly string _storage = Path.Combine(AppContext.BaseDirectory, "user-data");
    private NameCache IdentityCache => new(Path.Combine(_storage, "names-cache.json"));
    private string LayoutPath => Path.Combine(_storage, _compactMode ? "compact-window.json" : "window.json");
    private string SettingsPath => Path.Combine(_storage, "settings.json");
    private readonly string[] _args;
    private CombatTracker _tracker = null!;
    private LiveCapture? _capture;
    private DemoFightSource? _demo;
    private EncounterSnapshot? _shown;
    private FightRecord? _selected;
    private bool _showDungeonTotal;
    private readonly List<FightRecord> _history = [];
    private readonly DungeonHealingTotal _dungeonHealing = new();
    private readonly DungeonDamageTotal _dungeonDamage = new();
    private readonly PartyOnly _partyOnly = new();
    private readonly DungeonInstanceSession _instanceSession = new();
    private readonly DungeonSkillDamage _dungeonSkills = new();
    private bool _demonstrating, _locked, _lockHotkey, _closing;
    private bool _showOwnDamage;
    private bool _compactMode;
    private bool _autoOverlay = true, _autoCollapsed;
    private int _themeIndex;
    private int _generation;
    private int _savedCacheVersion;
    private string? _storageError;
    private nint _handle;
    private HwndSource? _source;
    private Forms.NotifyIcon? _tray;
    private static readonly CultureInfo Pt = CultureInfo.GetCultureInfo("pt-BR");

    public MainWindow(string[] args)
    {
        _args = args;
        InitializeComponent();
        VersionCreditText.Text = $"MDPS v{CurrentVersion()} · criado por @misttylol";
        ReadSettings();
        UpdateMetricMenu();
        ThemePicker.SelectedIndex = _themeIndex;
        ThemePicker.SelectionChanged += (_, _) => ChangeTheme();
        ApplyTheme();
        Width = 282;
        MinWidth = 275;
        Height = 505;
        MinHeight = 420;
        ApplyCompactMode(false);
        if (!_args.Contains("--self-test") && !_args.Contains("--smoke-live")) RestoreLayout();
        ApplyAutoOverlay(GameData.IsDungeonMap(_tracker?.ExportCache().MapId ?? 0), true);
        ReadHistory();
        RefreshHistory();
        _timer.Tick += (_, _) => Render();
        Loaded += OnLoaded;
        Closing += (_, _) =>
        {
            _closing = true;
            _timer.Stop();
            StopSources();
            SaveIdentityCache();
            _tracker?.Reset();
            if (!_args.Contains("--self-test") && !_args.Contains("--smoke-live")) SaveLayout();
            if (_tray is not null)
            {
                _tray.Visible = false;
                _tray.ContextMenuStrip?.Dispose();
                _tray.Dispose();
            }
            for (int id = 1; id <= 3; id++) UnregisterHotKey(_handle, id);
            _source?.RemoveHook(WindowHook);
        };
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _handle = new WindowInteropHelper(this).Handle;
        RemoveSystemFrameColor();
        _source = HwndSource.FromHwnd(_handle);
        _source?.AddHook(WindowHook);
        bool showKey = RegisterHotKey(_handle, 1, 0x4006, 0x44); // Ctrl+Shift+D
        _lockHotkey = RegisterHotKey(_handle, 2, 0x4006, 0x4C);
        bool resetKey = RegisterHotKey(_handle, 3, 0x4006, 0x52);
        if (!showKey || !_lockHotkey || !resetKey)
            StatusText.Text = "Atalho ocupado · use a bandeja do Windows para destravar";
        if (!_args.Contains("--self-test")) InitTray();
        _timer.Start();
        if (_args.Contains("--self-test"))
        {
            try
            {
                NewTracker(true);
                FeedTestFight();
                _showOwnDamage = false;
                UpdateMetricMenu();
                Render();
                if (_shown is null || _shown.Combatants.Count != 2 || _shown.TotalDamage != 30000)
                    throw new InvalidOperationException("Falha na integração entre combate e interface.");
                if (_shown.Combatants.Single(p => p.IsSelf).Healing != 6000 || SelfMetricValue.Text != "6.000")
                    throw new InvalidOperationException("A interface não exibiu a cura realizada.");
                if (Players.Items.Count != 3 || !Players.Items.Cast<PlayerRow>().Any(p => p.Name == "Sem dados" && p.Dps == "—"))
                    throw new InvalidOperationException("A interface não exibiu o resultado.");
                if (BossVitals.Visibility != Visibility.Visible || BossPercent.Text != "70%"
                    || BossLimitText.Visibility != Visibility.Visible)
                    throw new InvalidOperationException("A interface não exibiu a vida e o tempo do boss.");
                var originalTheme = _themeIndex;
                _themeIndex = 1;
                ApplyTheme();
                if (PartyDps.Foreground.ToString() != "#FF71B8FF" || BossHp.Foreground.ToString() != "#FF71B8FF")
                    throw new InvalidOperationException("A interface não aplicou a cor escolhida.");
                _themeIndex = originalTheme;
                ApplyTheme();
                if (Players.Items.Cast<PlayerRow>().Any(p => p.Icon is null)
                    || new BitmapImage(new Uri(IconOf(GameClass.Ranger)!)).PixelWidth == 0)
                    throw new InvalidOperationException("Os ícones das classes não carregaram.");
                _showOwnDamage = true;
                UpdateMetricMenu();
                Render();
                if (SelfMetricValue.Text != "20.000" || SelfRateLabel.Text != "DPS" || SelfRateValue.Text == "—")
                    throw new InvalidOperationException("A interface não exibiu o DPS pessoal.");
                _showOwnDamage = false;
                UpdateMetricMenu();
                Render();
                GearPopup.IsOpen = true;
                if (!GearPopup.IsOpen) throw new InvalidOperationException("A engrenagem não abriu o menu.");
                GearPopup.IsOpen = false;
                _compactMode = true;
                ApplyCompactMode(true);
                Render();
                if (CompactPanel.Visibility != Visibility.Visible || CompactPlayers.Items.Count != 3
                    || CompactSelfMetric.Text != "Cura 6.000")
                    throw new InvalidOperationException("O modo compacto não exibiu a PT e a cura.");
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                UpdateLayout();
                var compactPreview = new RenderTargetBitmap((int)ActualWidth, (int)ActualHeight, 96, 96, PixelFormats.Pbgra32);
                compactPreview.Render(this);
                Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "artifacts"));
                var compactEncoder = new PngBitmapEncoder();
                compactEncoder.Frames.Add(BitmapFrame.Create(compactPreview));
                using (var stream = File.Create(Path.Combine(AppContext.BaseDirectory, "artifacts", "compact-preview.png"))) compactEncoder.Save(stream);
                _compactMode = false;
                ApplyCompactMode(true);
                Render();
                if (ResetButton.Content?.ToString() != "↺")
                    throw new InvalidOperationException("O botão de zerar não apareceu no topo.");
                if (UpdateButton.Content?.ToString() != "Verificar atualização")
                    throw new InvalidOperationException("O botão de atualização não apareceu na engrenagem.");
                if (!VersionCreditText.Text.Contains("@misttylol") || !VersionCreditText.Text.Contains($"v{CurrentVersion()}"))
                    throw new InvalidOperationException("A versão e o crédito não apareceram na engrenagem.");
                SetLocked(true);
                if (!_locked || ((long)GetWindowLongPtr(_handle, -20) & 0x20) == 0)
                    throw new InvalidOperationException("O modo fixado não atravessou os cliques.");
                SetLocked(false);
                if (_locked || ((long)GetWindowLongPtr(_handle, -20) & 0x20) != 0)
                    throw new InvalidOperationException("Não foi possível destravar o painel.");
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                UpdateLayout();
                var bitmap = new RenderTargetBitmap((int)ActualWidth, (int)ActualHeight, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(this);
                Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "artifacts"));
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using (var stream = File.Create(Path.Combine(AppContext.BaseDirectory, "artifacts", "preview.png"))) encoder.Save(stream);
                _autoOverlay = true;
                ApplyAutoOverlay(false, true);
                if (!_autoCollapsed || IdleOverlay.Visibility != Visibility.Visible || Height > 50)
                    throw new InvalidOperationException("O overlay não recolheu corretamente fora da DG.");
                ApplyAutoOverlay(true, true);
                if (_autoCollapsed || FullPanel.Visibility != Visibility.Visible || Height < 420)
                    throw new InvalidOperationException("O overlay não restaurou o painel ao entrar na DG.");
                ProcessEvent(new ZoneChangedEvent(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 600092, "DG de teste", true));
                Render();
                if (_shown is not null || _dungeonHealing.Total() != 0 || BossVitals.Visibility != Visibility.Collapsed)
                    throw new InvalidOperationException("A entrada na DG não zerou o painel.");
                ResetMeter(this, new RoutedEventArgs());
                if (_shown is not null || _dungeonHealing.Total() != 0)
                    throw new InvalidOperationException("Zerar painel não limpou o combate e a cura.");
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "artifacts", "self-test.txt"), "PASS: PT, cura e DPS pessoal; boss e tempo; entrada na DG; menu; atualizar; zerar; fixar/destravar; prévia renderizada.");
                Application.Current.Shutdown(0);
            }
            catch (Exception ex)
            {
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "self-test-error.txt"), ex.ToString());
                Application.Current.Shutdown(1);
            }
            return;
        }
        if (_args.Contains("--demo")) StartDemo(this, new RoutedEventArgs());
        else StartLive(this, new RoutedEventArgs());
        if (_args.Contains("--smoke-live"))
        {
            await Task.Delay(5000);
            Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "artifacts"));
            try
            {
                int adapters = LiveCapture.ListDevices().Count;
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "artifacts", "live-status.txt"),
                    $"Npcap: {LiveCapture.IsNpcapInstalled()}\nAdaptadores acessíveis: {adapters}\nEstado: {_capture?.Status.State}\nMensagem: {_capture?.Status.Message}");
                Application.Current.Shutdown(adapters > 0 ? 0 : 1);
            }
            catch (Exception ex)
            {
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "artifacts", "live-status.txt"), ex.ToString());
                Application.Current.Shutdown(1);
            }
        }
    }

    private void NewTracker(bool demo)
    {
        StopSources();
        SaveIdentityCache();
        _tracker?.Reset();
        _dungeonHealing.NewSession();
        _dungeonDamage.NewSession();
        _dungeonSkills.NewSession();
        _partyOnly.Reset();
        _demonstrating = demo;
        _selected = null;
        _showDungeonTotal = false;
        _tracker = new CombatTracker(_data, new MeterOptions { TargetMode = TargetMode.All });
        if (!demo)
        {
            var cached = IdentityCache.Load() ?? SelfRecovery.FromRecentHistory(_history, DateTimeOffset.UtcNow);
            if (cached is not null) _tracker.ImportCache(cached);
        }
        ApplyAutoOverlay(demo || GameData.IsDungeonMap(_tracker.ExportCache().MapId), true);
        _savedCacheVersion = _tracker.CacheVersion;
        _tracker.RosterReceived += _partyOnly.Update;
        // The closure belongs to this tracker: switching modes never saves a demonstration as a real fight.
        _tracker.EncounterFinished += record =>
        {
            record = _partyOnly.Filter(record);
            if (record.Summary.TotalDamage > 0) _dungeonDamage.AddFinished(record);
            if (demo) return;
            if (record.Summary.Combatants.Count == 0) return;
            record = _partyOnly.IncludeRoster(record);
            try
            {
                Directory.CreateDirectory(Path.Combine(_storage, "history"));
                string path = Path.Combine(_storage, "history", record.Summary.Id + ".json");
                File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(record));
                File.Move(path + ".tmp", path, true);
                if (!_closing) Dispatcher.BeginInvoke(() =>
                {
                    _history.RemoveAll(r => r.Summary.Id == record.Summary.Id);
                    _history.Insert(0, record);
                    RefreshHistory();
                });
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _storageError = "Não foi possível salvar o histórico: " + ex.Message;
            }
        };
        RefreshHistory();
        Render();
    }

    private void StopSources()
    {
        _generation++;
        _demo?.Dispose();
        _demo = null;
        _capture?.Dispose();
        _capture = null;
    }

    private void StartLive(object sender, RoutedEventArgs e)
    {
        GearPopup.IsOpen = false;
        NewTracker(false);
        if (!LiveCapture.IsNpcapInstalled())
        {
            StatusText.Text = "Npcap não encontrado. Instale pelo site npcap.com e clique em Ao vivo.";
            return;
        }
        _capture = new LiveCapture(_data, new LiveCaptureOptions());
        _capture.EventDecoded += ProcessEvent;
        int generation = _generation;
        _capture.StatusChanged += status =>
        {
            if (_closing) return;
            Dispatcher.BeginInvoke(() =>
            {
                if (generation == _generation) StatusText.Text = StatusLabel(status);
            });
        };
        _capture.Start();
    }

    private void StartDemo(object sender, RoutedEventArgs e)
    {
        GearPopup.IsOpen = false;
        NewTracker(true);
        _demo = new DemoFightSource(ProcessEvent);
        _demo.Start(TimeSpan.FromMinutes(3));
        StatusText.Text = "DEMONSTRAÇÃO · dados simulados, sem leitura do jogo";
    }

    private static string StatusLabel(CaptureStatus s) => s.State switch
    {
        CaptureState.Starting => "Iniciando leitura do AION 2…",
        CaptureState.Stopped => "Leitura parada",
        CaptureState.Capturing => $"Lendo AION 2 · {s.Events:N0} eventos recebidos",
        CaptureState.WaitingForGame when s.Message.Contains("not running") => "Aguardando você abrir o AION 2",
        CaptureState.WaitingForGame => "Aguardando dados do jogo · entre no personagem e na DG",
        _ => "Falha na captura: " + s.Message
    };

    private void ProcessEvent(GameEvent gameEvent)
    {
        if (gameEvent is PartyMemberObservedEvent partyMember) _partyOnly.Observe(partyMember);
        // Processing a zone event may finish a fight; count it before starting the new map's total.
        var previousMapId = _tracker.ExportCache().MapId;
        var identityBefore = _tracker.ExportCache();
        var changedZone = gameEvent is ZoneChangedEvent changed && !changed.IsTeleport
            && changed.MapId != previousMapId;
        var newSession = gameEvent is ZoneChangedEvent incoming
            && DungeonEntryReset.ShouldReset(previousMapId, incoming);
        if (gameEvent is InstanceTimerEvent instanceTimer && _instanceSession.Observe(instanceTimer))
            newSession = true;
        if (gameEvent is DamageEvent skillHit)
            _dungeonSkills.Observe(skillHit, identityBefore.SelfId, identityBefore.MapId, _data);
        if (gameEvent is ZoneChangedEvent zoneChange && !zoneChange.IsTeleport)
            ApplyAutoOverlay(zoneChange.IsDungeon || GameData.IsDungeonMap(zoneChange.MapId), true);
        else if (gameEvent is InstanceTimerEvent)
            ApplyAutoOverlay(true, true);
        _tracker.Process(gameEvent);
        if (gameEvent is HealEvent heal) _dungeonHealing.ObserveHeal(heal, _tracker.ExportCache().SelfId);
        if (changedZone && !newSession && !GameData.IsDungeonMap(previousMapId)) _partyOnly.Reset();
        if (!newSession) return;

        // Clear only after the old encounter has been finished and written to history.
        _tracker.Reset();
        _partyOnly.Reset();
        _dungeonHealing.NewSession();
        _dungeonDamage.NewSession();
        _dungeonSkills.NewSession();
        Dispatcher.BeginInvoke(() =>
        {
            if (_closing) return;
            _selected = null;
            _showDungeonTotal = false;
            HistoryPicker.SelectedIndex = 0;
            Render();
        });
    }

    private void Render()
    {
        if (_tracker is null) return;
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _tracker.Tick(now);
        SaveIdentityCache();
        var identity = _tracker.ExportCache();
        _partyOnly.ObserveKnownPlayers(identity.Players);
        _partyOnly.InferDungeonRoster(identity);
        var current = _partyOnly.Filter(_tracker.Snapshot(null, now));
        var projected = _selected is not null ? _partyOnly.Filter(_selected.Summary) :
            _showDungeonTotal ? _dungeonDamage.Snapshot(current) : current;
        _shown = projected?.Combatants.Count > 0 ? projected : null;
        var s = _shown;
        _timer.Interval = s?.IsActive == true ? TimeSpan.FromMilliseconds(250) : TimeSpan.FromSeconds(1);
        var self = s?.Combatants.FirstOrDefault(p => p.IsSelf);
        if (_showOwnDamage)
        {
            SelfMetricValue.Text = self is null ? "—" : self.Damage.ToString("N0", Pt);
            SelfRateValue.Text = self is null ? "—" : Compact(self.Dps);
        }
        else
        {
            SelfMetricValue.Text = _dungeonHealing.Total() == 0
                ? "—" : _dungeonHealing.Total().ToString("N0", Pt);
            SelfRateValue.Text = self?.Hps is { } hps ? Compact(hps) : "—";
        }
        PartyDps.Text = s is null ? "—" : Compact(s.PartyDps);
        TotalDamage.Text = s is null ? "Aguardando seu combate" : $"{s.TotalDamage.ToString("N0", Pt)} de dano · {s.PlayerCount} jogadores";
        FightTime.Text = s is null ? "00:00" : $"{s.CombatMs / 60000:00}:{s.CombatMs / 1000 % 60:00}";
        FightState.Text = _selected is not null ? "HISTÓRICO" : _showDungeonTotal ? "TOTAL DA DG" : _demonstrating ? "SIMULAÇÃO" : s is null ? "AGUARDANDO" : s.IsActive ? "EM COMBATE" : "AO VIVO";
        var visibleBoss = s is { IsActive: true, Boss: { MaxHpKnown: true, MaxHp: > 0, Hp: >= 0 } }
            ? s.Boss : null;
        BossName.Text = visibleBoss?.Name ?? s?.Title ?? "Nenhum combate da PT";
        BossVitals.Visibility = visibleBoss is null ? Visibility.Collapsed : Visibility.Visible;
        BossHp.Value = visibleBoss?.HpFraction * 100 ?? 0;
        BossHealthText.Text = visibleBoss is null ? "" : $"{CompactHealth(visibleBoss.Hp)} / {CompactHealth(visibleBoss.MaxHp)}";
        BossPercent.Text = visibleBoss is null ? "" : (visibleBoss.HpFraction * 100).ToString("0", Pt) + "%";
        var bossLimit = visibleBoss is not null && _selected is null
            ? _tracker.BossTimeRemaining(visibleBoss.ActorId, now) : null;
        BossLimitText.Visibility = bossLimit is null ? Visibility.Collapsed : Visibility.Visible;
        BossLimitText.Text = bossLimit is { } remaining ? "Derrote em " + FormatCountdown(remaining) : "";
        double max = s?.Combatants.MaxBy(p => p.Damage)?.Damage ?? 1;
        var selfClass = identity.Players.FirstOrDefault(p => p.Id == identity.SelfId)?.Class ?? GameClass.Unknown;
        if (selfClass == GameClass.Unknown)
            selfClass = SelfRecovery.ClassFromRecentHistory(_history, _tracker.SelfName, _tracker.SelfServerId, DateTimeOffset.UtcNow);
        var rows = s?.Combatants.OrderByDescending(p => p.Damage).Select(p => new PlayerRow(
            p.Name + (p.IsSelf ? " · você" : ""), p.Damage.ToString("N0", Pt) + " de dano",
            Compact(p.Dps), (p.Share * 100).ToString("0.0", Pt) + "%",
            max > 0 ? p.Damage / max * 100 : 0, p.IsSelf ? CurrentTheme.Main : "#B2C9F6",
            IconOf(p.IsSelf && p.Class == GameClass.Unknown ? selfClass : p.Class))).ToList() ?? [];
        var missing = _selected is null && (s is null || s.IsActive) ? _partyOnly.MissingMembers(s) : [];
        foreach (var member in missing)
        {
            bool isSelf = string.Equals(member.Name, _tracker.SelfName, StringComparison.OrdinalIgnoreCase)
                && (member.ServerId == 0 || _tracker.SelfServerId == 0 || member.ServerId == _tracker.SelfServerId);
            rows.Add(new PlayerRow(member.Name + (isSelf ? " · você" : ""),
                s is null ? "Aguardando combate" : "Sem dados de dano", "—", "—", 0,
                isSelf ? CurrentTheme.Main : "#8B9CB3", IconOf(isSelf && member.Class == GameClass.Unknown ? selfClass : member.Class)));
        }
        if (_selected is null && !rows.Any(p => p.Name == _tracker.SelfName + " · você"))
            rows.Insert(0, new PlayerRow((_tracker.SelfName ?? "Você") + " · você",
                s is null ? "Aguardando seu combate" : "Sem dados de dano", "—", "—", 0,
                CurrentTheme.Main, IconOf(selfClass)));
        Players.ItemsSource = rows;
        CompactPlayers.ItemsSource = rows;
        CompactFightTime.Text = FightTime.Text;
        CompactPartyDps.Text = "DPS TOTAL " + PartyDps.Text;
        CompactSelfMetric.Text = (_showOwnDamage ? "Dano " : "Cura ") + SelfMetricValue.Text;
        CompactBossName.Text = visibleBoss?.Name ?? "Combate da PT";
        CompactBossPercent.Text = BossPercent.Text;
        CompactBossHp.Value = BossHp.Value;
        CompactBossHealth.Text = visibleBoss is null ? TotalDamage.Text : BossHealthText.Text;
        CompactBossLimit.Text = BossLimitText.Visibility == Visibility.Visible ? BossLimitText.Text : "";
        EmptyMessage.Visibility = _selected is not null && s is null ? Visibility.Visible : Visibility.Collapsed;
        if (s?.MostDamageUnsent == true)
            StatusText.Text = "Dados parciais: o servidor não enviou o dano de todos os jogadores.";
        if (s is not null && !_demonstrating && !_partyOnly.HasRoster)
            StatusText.Text = "Aguardando a lista da PT · mostrando somente você";
        if (s is { IsActive: true } && missing.Length > 0)
            StatusText.Text = $"DPS parcial · dano recebido de {s.Combatants.Count}/{s.Combatants.Count + missing.Length} membros da PT";
        else if (s is { IsActive: true } && _partyOnly.HasRoster && !_demonstrating)
            StatusText.Text = $"PT identificada: {_partyOnly.MemberCount} nomes · dano de {s.Combatants.Count}";
        if (_partyOnly.IsInferred && _selected is null)
            StatusText.Text = $"PT provável: {_partyOnly.MemberCount} nomes · confirme se todos são da PT";
        if (_storageError is not null) StatusText.Text = _storageError;
        CompactStatus.Text = StatusText.Text;
        var skillTotals = _dungeonSkills.Snapshot(_data);
        var maxSkill = skillTotals.FirstOrDefault()?.Damage ?? 1;
        SkillRows.ItemsSource = skillTotals.Select(x => new SkillDisplayRow(x.Name,
            x.Damage.ToString("N0", Pt), x.Hits.ToString("N0", Pt) + " acertos",
            (x.Share * 100).ToString("0.0", Pt) + "%", x.Damage / (double)Math.Max(1, maxSkill) * 100)).ToList();
        SkillsEmpty.Visibility = skillTotals.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SaveIdentityCache()
    {
        if (_tracker is null || _demonstrating || _tracker.CacheVersion == _savedCacheVersion) return;
        IdentityCache.Save(_tracker.ExportCache());
        _savedCacheVersion = _tracker.CacheVersion;
    }

    private void ShowHealing(object sender, RoutedEventArgs e) => SetPersonalMetric(false);
    private void ShowDamage(object sender, RoutedEventArgs e) => SetPersonalMetric(true);

    private static readonly PanelTheme[] Themes =
    [
        new("#68E7CA", "#E5849E", "#A6EEA6"),
        new("#71B8FF", "#71B8FF", "#A8D4FF"),
        new("#B69BFF", "#B69BFF", "#D0BFFF"),
        new("#F5A3B5", "#F5A3B5", "#FFC2D0"),
        new("#F2CD70", "#F2CD70", "#FFE0A0"),
        new("#F58B8B", "#F58B8B", "#FFB2B2")
    ];

    private PanelTheme CurrentTheme => Themes[_themeIndex];

    private static SolidColorBrush Brush(string hex) => new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex)!);

    private void ChangeTheme()
    {
        _themeIndex = Math.Clamp(ThemePicker.SelectedIndex, 0, Themes.Length - 1);
        ApplyTheme();
        SaveSettings();
        Render();
    }

    private void ApplyTheme()
    {
        var theme = CurrentTheme;
        LogoText.Foreground = Brush(theme.Main);
        CompactLogo.Foreground = Brush(theme.Main);
        PartyDps.Foreground = Brush(theme.Main);
        CompactPartyDps.Foreground = Brush(theme.Main);
        StatusText.Foreground = Brush(theme.Main);
        SelfMetricValue.Foreground = Brush(theme.Self);
        CompactSelfMetric.Foreground = Brush(theme.Self);
        SelfRateValue.Foreground = Brush(theme.Self);
        BossHp.Foreground = Brush(theme.Boss);
        CompactBossHp.Foreground = Brush(theme.Boss);
        BossPercent.Foreground = Brush(theme.Boss);
        CompactBossPercent.Foreground = Brush(theme.Boss);
        CompactBossName.Foreground = Brush(theme.Boss);
    }

    private void SetPersonalMetric(bool damage)
    {
        _showOwnDamage = damage;
        UpdateMetricMenu();
        SaveSettings();
        GearPopup.IsOpen = false;
        Render();
    }

    private void ToggleCompactMode(object sender, RoutedEventArgs e)
    {
        GearPopup.IsOpen = false;
        if (!_autoCollapsed) SaveLayout();
        _compactMode = !_compactMode;
        ApplyCompactMode(true);
        if (!_autoCollapsed) RestoreLayout();
        SaveSettings();
        Render();
    }

    private void ToggleAutoOverlay(object sender, RoutedEventArgs e)
    {
        GearPopup.IsOpen = false;
        _autoOverlay = !_autoOverlay;
        AutoOverlayButton.Content = (_autoOverlay ? "✓ " : "") + "Recolher fora da DG";
        ApplyAutoOverlay(GameData.IsDungeonMap(_tracker.ExportCache().MapId), true);
        SaveSettings();
    }

    private void ApplyCompactMode(bool resize)
    {
        CompactModeButton.Content = _compactMode ? "✓ Modo compacto" : "Modo compacto";
        if (_autoCollapsed) return;
        FullPanel.Visibility = _compactMode ? Visibility.Collapsed : Visibility.Visible;
        CompactPanel.Visibility = _compactMode ? Visibility.Visible : Visibility.Collapsed;
        WindowBorder.Padding = _compactMode ? new Thickness(7, 5, 7, 4) : new Thickness(12, 10, 12, 10);
        MinWidth = _compactMode ? 240 : 275;
        MinHeight = _compactMode ? 165 : 420;
        if (resize)
        {
            Width = _compactMode ? 290 : 282;
            Height = _compactMode ? 245 : 505;
        }
        GearPopup.PlacementTarget = _compactMode ? CompactGearButton : GearButton;
    }

    private void ApplyAutoOverlay(bool inDungeon, bool resize)
    {
        var collapse = _autoOverlay && !inDungeon;
        var wasCollapsed = _autoCollapsed;
        if (collapse && !_autoCollapsed) SaveLayout();
        _autoCollapsed = collapse;
        IdleOverlay.Visibility = _autoCollapsed ? Visibility.Visible : Visibility.Collapsed;
        FullPanel.Visibility = !_autoCollapsed && !_compactMode ? Visibility.Visible : Visibility.Collapsed;
        CompactPanel.Visibility = !_autoCollapsed && _compactMode ? Visibility.Visible : Visibility.Collapsed;
        WindowBorder.Padding = _autoCollapsed ? new Thickness(7, 4, 7, 4) :
            _compactMode ? new Thickness(7, 5, 7, 4) : new Thickness(12, 10, 12, 10);
        MinWidth = _autoCollapsed ? 210 : _compactMode ? 240 : 275;
        MinHeight = _autoCollapsed ? 34 : _compactMode ? 165 : 420;
        if (resize)
        {
            if (_autoCollapsed)
            {
                Width = 225;
                Height = 38;
            }
            else
            {
                Width = _compactMode ? 290 : 282;
                Height = _compactMode ? 245 : 505;
            }
        }
        GearPopup.PlacementTarget = _autoCollapsed ? IdleGearButton : _compactMode ? CompactGearButton : GearButton;
        if (resize && !_autoCollapsed && wasCollapsed) RestoreLayout();
    }

    private void UpdateMetricMenu()
    {
        ShowHealingButton.Content = _showOwnDamage ? "Minha cura" : "✓ Minha cura";
        ShowDamageButton.Content = _showOwnDamage ? "✓ Meu DPS" : "Meu DPS";
        SelfMetricLabel.Text = _showOwnDamage ? "MEU DANO NO COMBATE" : "SUA CURA NA DG";
        SelfRateLabel.Text = _showOwnDamage ? "DPS" : "HPS";
        SelfMetricBorder.ToolTip = _showOwnDamage
            ? "Seu dano total e DPS no combate mostrado. Escolha Minha cura na engrenagem para ver a cura acumulada na DG."
            : "Soma da cura realizada por você desde a entrada no mapa atual, incluindo autocura. O HPS é do combate mostrado. Não separa overheal.";
    }

    private async void CheckForUpdate(object sender, RoutedEventArgs e)
    {
        GearPopup.IsOpen = false;
        UpdateButton.IsEnabled = false;
        SetStatus("Procurando atualização…");
        try
        {
            var current = CurrentVersion();
            using var feed = new UpdateFeed($"MDPS/{current}");
            var latest = await feed.GetLatestAsync();
            if (latest is null)
            {
                MessageBox.Show(this, "Não foi possível encontrar uma versão publicada.", "Atualização do MDPS",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                SetStatus("Nenhuma versão publicada foi encontrada");
                return;
            }
            if (latest.Version <= current)
            {
                MessageBox.Show(this, $"Você já está usando a versão mais recente ({current}).", "Atualização do MDPS",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                SetStatus($"MDPS {current} está atualizado");
                return;
            }
            if (latest.Installer is null)
                throw new InvalidDataException("A versão mais recente não possui instalador para Windows.");

            var answer = MessageBox.Show(this,
                $"A versão {latest.Version} está disponível.\n\nDeseja baixar e instalar agora?",
                "Atualização do MDPS", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes)
            {
                SetStatus($"Atualização {latest.Version} disponível");
                return;
            }

            var progress = new Progress<double>(value =>
                SetStatus($"Baixando atualização… {value * 100:0}%"));
            string folder = Path.Combine(Path.GetTempPath(), "MDPS", "updates", latest.Tag);
            string installer = await feed.DownloadAsync(latest.Installer, folder, progress);
            SetStatus("Abrindo o instalador…");
            Process.Start(new ProcessStartInfo(installer) { UseShellExecute = true });
            Close();
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or UnauthorizedAccessException
            or InvalidDataException or TaskCanceledException)
        {
            SetStatus("Não foi possível atualizar");
            MessageBox.Show(this, "Não foi possível concluir a atualização.\n\n" + ex.Message,
                "Atualização do MDPS", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            UpdateButton.IsEnabled = true;
        }
    }

    private void SetStatus(string text)
    {
        StatusText.Text = text;
        CompactStatus.Text = text;
    }

    private static Version CurrentVersion() =>
        UpdateFeed.Normalize(Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0));

    private void ReadSettings()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var settings = JsonSerializer.Deserialize<PersonalSettings>(File.ReadAllText(SettingsPath));
                _showOwnDamage = settings?.ShowOwnDamage ?? false;
                _themeIndex = Math.Clamp(settings?.ThemeIndex ?? 0, 0, Themes.Length - 1);
                _compactMode = settings?.CompactMode ?? false;
                _autoOverlay = settings?.AutoOverlay ?? true;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { }
        AutoOverlayButton.Content = (_autoOverlay ? "✓ " : "") + "Recolher fora da DG";
    }

    private void SaveSettings()
    {
        try
        {
            Directory.CreateDirectory(_storage);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(new PersonalSettings(_showOwnDamage, _themeIndex, _compactMode, _autoOverlay)));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }

    internal static string Compact(double value) => value switch
    {
        >= 1000000 => (value / 1000000).ToString("0.00", Pt) + " mi",
        >= 1000 => (value / 1000).ToString("0.0", Pt) + " mil",
        _ => value.ToString("N0", Pt)
    };

    internal static string? IconOf(GameClass gameClass) => gameClass == GameClass.Unknown
        ? null : $"pack://application:,,,/Assets/Classes/{gameClass.ToString().ToLowerInvariant()}.png";

    private static string CompactHealth(long hp) => hp switch
    {
        >= 1_000_000_000 => (hp / 1_000_000_000.0).ToString("0.00", Pt) + " bi",
        >= 1_000_000 => (hp / 1_000_000.0).ToString("0.0", Pt) + " mi",
        >= 1_000 => (hp / 1_000.0).ToString("0.0", Pt) + " mil",
        _ => hp.ToString("N0", Pt)
    };

    private static string FormatCountdown(long milliseconds)
    {
        var seconds = Math.Max(0, milliseconds / 1000);
        return $"{seconds / 60:00}:{seconds % 60:00}";
    }

    private void ResetFight(object sender, RoutedEventArgs e)
    {
        GearPopup.IsOpen = false;
        _selected = null;
        _showDungeonTotal = false;
        _tracker.Reset();
        _dungeonDamage.NewSession();
        _dungeonSkills.NewSession();
        _dungeonHealing.NewSession();
        HistoryPicker.SelectedIndex = 0;
        Render();
    }

    private void ResetMeter(object sender, RoutedEventArgs e)
    {
        GearPopup.IsOpen = false;
        _selected = null;
        _showDungeonTotal = false;
        _tracker.Reset();
        _dungeonHealing.NewSession();
        _dungeonDamage.NewSession();
        _dungeonSkills.NewSession();
        HistoryPicker.SelectedIndex = 0;
        Render();
    }

    private void ReadHistory()
    {
        string dir = Path.Combine(_storage, "history");
        if (!Directory.Exists(dir)) return;
        try
        {
            foreach (string path in Directory.EnumerateFiles(dir, "*.json").OrderByDescending(File.GetLastWriteTimeUtc).Take(100))
            {
                try
                {
                    if (JsonSerializer.Deserialize<FightRecord>(File.ReadAllText(path)) is { Summary: not null, Details: not null } record && record.Summary.Combatants is not null)
                        _history.Add(record);
                }
                catch (Exception ex) when (ex is IOException or JsonException) { }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { _storageError = "Não foi possível abrir o histórico."; }
    }

    private void RefreshHistory()
    {
        var selected = _selected;
        var choices = new List<FightChoice>
        {
            new("Combate atual (ao vivo)", null, false),
            new("Total acumulado da DG", null, true)
        };
        choices.AddRange(_history.OrderByDescending(r => r.Summary.StartedAt).Take(10).Select(r =>
            new FightChoice($"{r.Summary.StartedAt.ToLocalTime():dd/MM HH:mm} · {r.Summary.Title} · PT {r.Summary.PlayerCount}", r, false)));
        HistoryPicker.ItemsSource = choices;
        HistoryPicker.SelectedIndex = selected is not null
            ? Math.Max(0, choices.FindIndex(c => c.Record?.Summary.Id == selected.Summary.Id))
            : _showDungeonTotal ? 1 : 0;
    }

    private void SelectFight(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        var choice = HistoryPicker.SelectedItem as FightChoice;
        _selected = choice?.Record;
        _showDungeonTotal = choice?.DungeonTotal == true;
        Render();
    }

    private void ToggleLock(object sender, RoutedEventArgs e)
    {
        SetLocked(!_locked);
    }

    private void SetLocked(bool locked)
    {
        if (_locked == locked) return;
        _locked = locked;
        long style = (long)GetWindowLongPtr(_handle, -20);
        const long mask = 0x20 | 0x08000000;
        SetWindowLongPtr(_handle, -20, (nint)(_locked ? style | mask : style & ~mask));
        SetWindowPos(_handle, 0, 0, 0, 0, 0, 0x27);
        if (_tray is not null)
            _tray.Text = _locked ? "MDPS · Fixado; destrave pelo menu" : "MDPS · AION 2 Global";
    }

    private void InitTray()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Mostrar e destravar", null, (_, _) => Dispatcher.BeginInvoke(ShowAndUnlock));
        menu.Items.Add("Fechar MDPS", null, (_, _) => Dispatcher.BeginInvoke(Close));
        _tray = new Forms.NotifyIcon
        {
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? System.Drawing.SystemIcons.Application,
            Text = "MDPS · AION 2 Global",
            ContextMenuStrip = menu,
            Visible = true
        };
        _tray.DoubleClick += (_, _) => Dispatcher.BeginInvoke(ShowAndUnlock);
    }

    private void RestoreLayout()
    {
        try
        {
            if (!File.Exists(LayoutPath)) return;
            var layout = JsonSerializer.Deserialize<WindowLayout>(File.ReadAllText(LayoutPath));
            if (layout is null || !double.IsFinite(layout.Left) || !double.IsFinite(layout.Top)
                || !double.IsFinite(layout.Width) || !double.IsFinite(layout.Height)) return;
            var anchor = new System.Drawing.Point((int)layout.Left + 40, (int)layout.Top + 30);
            if (!Forms.Screen.AllScreens.Any(screen => screen.WorkingArea.Contains(anchor))) return;
            Width = Math.Clamp(layout.Width, MinWidth, 800);
            Height = Math.Clamp(layout.Height, MinHeight, 900);
            Left = layout.Left;
            Top = layout.Top;
            WindowStartupLocation = WindowStartupLocation.Manual;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { }
    }

    private void SaveLayout()
    {
        if (_autoCollapsed) return;
        if (!double.IsFinite(Left) || !double.IsFinite(Top)) return;
        try
        {
            Directory.CreateDirectory(_storage);
            var bounds = WindowState == WindowState.Normal ? new WindowLayout(Left, Top, Width, Height)
                : new WindowLayout(RestoreBounds.Left, RestoreBounds.Top, RestoreBounds.Width, RestoreBounds.Height);
            File.WriteAllText(LayoutPath, JsonSerializer.Serialize(bounds));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }

    private void RemoveSystemFrameColor()
    {
        // On Windows 11, suppress the DWM outline; the WPF border draws our dark frame.
        int noBorder = unchecked((int)0xFFFFFFFE);
        DwmSetWindowAttribute(_handle, 34, ref noBorder, sizeof(int));
        int darkCaption = 0x00211710; // COLORREF for #101721, used if DWM still paints a title strip.
        DwmSetWindowAttribute(_handle, 35, ref darkCaption, sizeof(int));
    }

    private void ShowAndUnlock()
    {
        SetLocked(false);
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void ToggleActionsMenu(object sender, RoutedEventArgs e) => GearPopup.IsOpen = !GearPopup.IsOpen;

    private nint WindowHook(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg != 0x0312) return 0;
        handled = true;
        switch ((int)wParam)
        {
            case 1: if (IsVisible && WindowState != WindowState.Minimized) Hide(); else { Show(); WindowState = WindowState.Normal; } break;
            case 2: ToggleLock(this, new RoutedEventArgs()); break;
            case 3: ResetFight(this, new RoutedEventArgs()); break;
        }
        return 0;
    }

    private void DragHeader(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject element)
            for (var p = element; p is not null; p = VisualTreeHelper.GetParent(p))
                if (p is System.Windows.Controls.Button) return;
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }
    private void Minimize(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void CloseWindow(object sender, RoutedEventArgs e) => Close();

    private void FeedTestFight()
    {
        long t = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 10000;
        ProcessEvent(new SelfIdentifiedEvent(t, 1, "Vitoria", 2001, GameClass.Ranger));
        ProcessEvent(new PlayerSeenEvent(t, 2, "Companheiro", 2001, GameClass.Gladiator));
        ProcessEvent(new PlayerSeenEvent(t, 3, "Pessoa de fora", 2001, GameClass.Ranger));
        ProcessEvent(new PartyRosterEvent(t, [new PartyMemberInfo("Vitoria", 2001, 1, 0, 0, GameClass.Ranger),
            new PartyMemberInfo("Companheiro", 2001, 1, 0, 0, GameClass.Gladiator),
            new PartyMemberInfo("Sem dados", 2001, 1, 0, 0, GameClass.Sorcerer)]));
        ProcessEvent(new NpcSeenEvent(t, 900, 0, 10_000_000, "Chefe de treinamento · simulação"));
        ProcessEvent(new DamageEvent(t, 1, 900, 14010000, 20000, HitFlags.Critical, 1));
        ProcessEvent(new DamageEvent(t + 9000, 2, 900, 11010000, 10000, HitFlags.None, 1));
        ProcessEvent(new DamageEvent(t + 9000, 3, 900, 14010000, 50000, HitFlags.None, 1));
        ProcessEvent(new NpcHpEvent(t + 9000, 900, 7_000_000, 10_000_000));
        ProcessEvent(new BossTimeLimitEvent(t + 9000, 900, t + 5 * 60_000));
        ProcessEvent(new HealEvent(t + 9000, 1, 2, 17010000, 6000, HitFlags.None));
        StatusText.Text = "DEMONSTRAÇÃO · dados simulados, sem leitura do jogo";
    }

    [DllImport("user32.dll")] private static extern bool RegisterHotKey(nint hwnd, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(nint hwnd, int id);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern nint GetWindowLongPtr(nint hwnd, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] private static extern nint SetWindowLongPtr(nint hwnd, int index, nint value);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(nint hwnd, nint after, int x, int y, int width, int height, uint flags);
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(nint hwnd, int attribute, ref int value, int size);
}

internal sealed record WindowLayout(double Left, double Top, double Width, double Height);
internal sealed record PersonalSettings(bool ShowOwnDamage, int ThemeIndex = 0, bool CompactMode = false, bool AutoOverlay = true);
internal sealed record PanelTheme(string Main, string Boss, string Self);

public sealed record PlayerRow(string Name, string Damage, string Dps, string Share, double Bar, string Color, string? Icon);
public sealed record SkillDisplayRow(string Name, string Damage, string Hits, string Share, double Bar);
public sealed record FightChoice(string Label, FightRecord? Record, bool DungeonTotal = false);
