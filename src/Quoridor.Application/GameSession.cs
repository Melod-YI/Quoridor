using System.Collections.Generic;
using System.Collections.Immutable;
using Quoridor.Application.Logging;
using Quoridor.Application.Seats;
using Quoridor.Domain.Core;
using Quoridor.Domain.Notation;
using Quoridor.Domain.Rules;

namespace Quoridor.Application;

/// <summary>
/// 对局编排中枢: 持有当前 GameState + 座位表, 人手与 AI 走同一 Submit→ValidateAndApply→广播 通道。
/// AI 座位在人手之后由本类自动驱动(循环到人类回合或终局)。
/// </summary>
public sealed class GameSession
{
    private readonly BoardConfig _cfg;
    private readonly IReadOnlyDictionary<PlayerId, IPlayer> _seats;
    private readonly IAppLogger _logger;
    private readonly List<IGameEvent> _eventLog = new();
    private readonly bool _autoDriveAi;

    private const int DefaultMaxPlies = 1000;  // AI 驱动安全上限, 防失控

    public GameState State { get; private set; }
    public IReadOnlyList<IGameEvent> EventLog => _eventLog;

    /// <summary>事件广播: 每个已发生的事件(含 Rejected)都会经此通知订阅者。
    /// 注意: 状态在广播前已提交, 订阅者读到的是最新 State; 但禁止在处理函数内重入 Submit/Start(单线程编排, 未做重入保护)。</summary>
    public event Action<IGameEvent>? EventOccurred;

    /// <param name="autoDriveAi">true(默认, 同步驱动, 供测试/demo): Start/Submit 后自动跑后续 AI 座位到人类回合。
    /// false(异步模式, 供 Godot UI): 不自动驱动, 由调用方在后台线程 PeekAiProposal 取决策后回主线程 Submit——避免 AI 长搜索阻塞渲染。</param>
    public GameSession(BoardConfig cfg, IReadOnlyList<IPlayer> seats, IAppLogger? logger = null, bool autoDriveAi = true)
    {
        _cfg = cfg;
        _logger = logger ?? NullAppLogger.Instance;
        _seats = BuildSeatMap(seats);
        _autoDriveAi = autoDriveAi;
        State = GameSetup.Create(cfg, seats.Count);
        _logger.Log(LogLevel.Info, "GameSession 构造 cfg={Variant} players={N} autoDrive={Auto}", cfg.Variant, seats.Count, autoDriveAi);
    }

    /// <summary>启动对局: autoDriveAi=true 时若起手座位是 AI 则自动驱动; false 时空操作(由调用方 PeekAiProposal 驱动)。</summary>
    public void Start(int maxPlies = DefaultMaxPlies)
    {
        _logger.Log(LogLevel.Info, "对局开始");
        if (_autoDriveAi) DriveAi(maxPlies);
    }

    /// <summary>提交一手命令(人或外部来源)。合法则替换状态并广播。
    /// autoDriveAi=true 时随后自动驱动后续 AI 座位; false 时不驱动(调用方自行异步驱动)。</summary>
    public RuleEngine.ApplyResult Submit(IGameCommand command)
    {
        _logger.Log(LogLevel.Info, "Submit 入口 active={Active} cmd={Cmd}", State.ActivePlayer, command);

        if (State.IsFinished)
        {
            _logger.Log(LogLevel.Warning, "Submit 跳过: 对局已终局");
            return new RuleEngine.ApplyResult(null, ImmutableArray<IGameEvent>.Empty);
        }

        var r = RuleEngine.ValidateAndApply(State, command);
        if (r.State is null)
        {
            Broadcast(r.Events);   // 拒绝: 状态不变, 广播 Rejected
            _logger.Log(LogLevel.Warning, "Submit 被规则拒绝 events={Events}", string.Join(',', r.Events));
            return r;
        }

        State = r.State!;           // 先提交状态
        _logger.Log(LogLevel.Info, "Submit 应用成功 新活跃={Active}", State.ActivePlayer);
        Broadcast(r.Events);        // 再广播: 订阅者看到已提交的新状态

        if (_autoDriveAi) DriveAi(DefaultMaxPlies);  // 同步模式自动驱动; 异步模式由调用方驱动
        return r;
    }

    /// <summary>当前是否轮到 AI(autoDriveAi=false 异步模式用)。终局返回 false。</summary>
    public bool IsAiTurn =>
        !State.IsFinished
        && _seats.TryGetValue(State.ActivePlayer, out var seat)
        && !seat.IsHuman;

    /// <summary>在当前状态取活跃 AI 座位的拟走命令; 非 AI 回合或终局返回 null。
    /// 线程安全: 只读不可变 State + 构造后不变的 _seats, 调用纯函数 IQuoridorAi.Choose(只读 state 创建新 state)。
    /// 供 Godot 端在后台线程调用, 拿到命令后回主线程 Submit。</summary>
    public IGameCommand? PeekAiProposal()
    {
        if (State.IsFinished) return null;
        if (!_seats.TryGetValue(State.ActivePlayer, out var seat)) return null;
        if (seat.IsHuman) return null;
        return seat.ProposeMove(State);
    }

    /// <summary>导出当前已走完的记谱串(仅含已应用的走子/设墙)。</summary>
    public string Export() => NotationService.Encode(_eventLog, State.Players.Length);

    private void DriveAi(int maxPlies)
    {
        int plies = 0;
        while (!State.IsFinished && plies < maxPlies)
        {
            if (!_seats.TryGetValue(State.ActivePlayer, out var seat))
            {
                _logger.Log(LogLevel.Warning, "DriveAi: 活跃玩家无座位 {Active}, 停止", State.ActivePlayer);
                return;
            }
            if (seat.IsHuman) return;  // 等人类 Submit

            var cmd = seat.ProposeMove(State);
            if (cmd is null) return;  // 债7: AI 拒绝(含已终局防御)

            var r = RuleEngine.ValidateAndApply(State, cmd);
            if (r.State is null)
            {
                Broadcast(r.Events);   // 理论不触发(AI 永不下非法手), 仍广播以保持一致
                _logger.Log(LogLevel.Error, "DriveAi: AI 产出非法命令 {Cmd}, 停止", cmd);
                return;
            }
            State = r.State!;           // 先提交
            _logger.Log(LogLevel.Debug, "DriveAi: AI({Who}) 走 {Cmd}", seat.Id, cmd);
            Broadcast(r.Events);        // 再广播
            plies++;
        }

        if (State.IsFinished)
            _logger.Log(LogLevel.Info, "对局终局 winner={Winner}", State.Winner);
    }

    private void Broadcast(ImmutableArray<IGameEvent> events)
    {
        foreach (var e in events)
        {
            _eventLog.Add(e);
            EventOccurred?.Invoke(e);
        }
    }

    private static IReadOnlyDictionary<PlayerId, IPlayer> BuildSeatMap(IReadOnlyList<IPlayer> seats)
    {
        var dict = new Dictionary<PlayerId, IPlayer>();
        foreach (var s in seats) dict[s.Id] = s;
        return dict;
    }
}
