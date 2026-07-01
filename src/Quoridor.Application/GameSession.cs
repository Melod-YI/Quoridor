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

    private const int DefaultMaxPlies = 1000;  // AI 驱动安全上限, 防失控

    public GameState State { get; private set; }
    public IReadOnlyList<IGameEvent> EventLog => _eventLog;

    /// <summary>事件广播: 每个已发生的事件(含 Rejected)都会经此通知订阅者。</summary>
    public event Action<IGameEvent>? EventOccurred;

    public GameSession(BoardConfig cfg, IReadOnlyList<IPlayer> seats, IAppLogger? logger = null)
    {
        _cfg = cfg;
        _logger = logger ?? NullAppLogger.Instance;
        _seats = BuildSeatMap(seats);
        State = GameSetup.Create(cfg, seats.Count);
        _logger.Log(LogLevel.Info, "GameSession 构造 cfg={Variant} players={N}", cfg.Variant, seats.Count);
    }

    /// <summary>启动对局: 若起手座位是 AI 则自动驱动。人类起手为空操作。</summary>
    public void Start(int maxPlies = DefaultMaxPlies)
    {
        _logger.Log(LogLevel.Info, "对局开始");
        DriveAi(maxPlies);
    }

    /// <summary>提交一手命令(人或外部来源)。合法则替换状态并广播, 随后自动驱动后续 AI 座位。</summary>
    public RuleEngine.ApplyResult Submit(IGameCommand command)
    {
        _logger.Log(LogLevel.Info, "Submit 入口 active={Active} cmd={Cmd}", State.ActivePlayer, command);

        if (State.IsFinished)
        {
            _logger.Log(LogLevel.Warning, "Submit 跳过: 对局已终局");
            return new RuleEngine.ApplyResult(null, ImmutableArray<IGameEvent>.Empty);
        }

        var r = RuleEngine.ValidateAndApply(State, command);
        Broadcast(r.Events);

        if (r.State is null)
        {
            _logger.Log(LogLevel.Warning, "Submit 被规则拒绝 events={Events}", string.Join(',', r.Events));
            return r;
        }

        State = r.State!;
        _logger.Log(LogLevel.Info, "Submit 应用成功 新活跃={Active}", State.ActivePlayer);

        DriveAi(DefaultMaxPlies);  // 自动驱动后续 AI 座位
        return r;
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
            Broadcast(r.Events);
            if (r.State is null)
            {
                _logger.Log(LogLevel.Error, "DriveAi: AI 产出非法命令 {Cmd}, 停止", cmd);
                return;
            }
            State = r.State!;
            _logger.Log(LogLevel.Debug, "DriveAi: AI({Who}) 走 {Cmd}", seat.Id, cmd);
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
