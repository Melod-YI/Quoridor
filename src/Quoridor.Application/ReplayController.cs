using System.Collections.Immutable;
using Quoridor.Domain.Core;
using Quoridor.Domain.Notation;
using Quoridor.Domain.Rules;

namespace Quoridor.Application;

/// <summary>记谱回放控制器: 导入记谱串, 提供 ⏮⬅➡⏭ 步进与跳转。命令不可逆, StepBack 通过从头重放实现。</summary>
public sealed class ReplayController
{
    private readonly BoardConfig _cfg;
    private readonly int _players;
    private readonly ImmutableArray<IGameCommand> _cmds;
    private GameState _state;
    private int _cursor;

    public ReplayController(BoardConfig cfg, int players, string notation)
    {
        _cfg = cfg;
        _players = players;
        _cmds = NotationService.Decode(notation, cfg);  // cfg 感知: 越界在此抛精确异常
        _state = GameSetup.Create(cfg, players);
        _cursor = 0;
    }

    public GameState Current => _state;
    public int Cursor => _cursor;
    public int Total => _cmds.Length;
    public bool AtStart => _cursor == 0;
    public bool AtEnd => _cursor == _cmds.Length;

    public void Reset()
    {
        _state = GameSetup.Create(_cfg, _players);
        _cursor = 0;
    }

    public bool StepForward()
    {
        if (_cursor >= _cmds.Length) return false;
        var r = RuleEngine.ValidateAndApply(_state, _cmds[_cursor]);
        if (r.State is null)
            throw new NotationParseException($"回放第 {_cursor + 1} 手非法: {_cmds[_cursor]}");
        _state = r.State!;
        _cursor++;
        return true;
    }

    public bool StepBack()
    {
        if (_cursor == 0) return false;
        int target = _cursor - 1;
        Reset();
        for (int i = 0; i < target; i++) StepForward();
        return true;
    }

    public void GoTo(int index)
    {
        if (index < 0 || index > _cmds.Length)
            throw new System.ArgumentOutOfRangeException(nameof(index));
        Reset();
        for (int i = 0; i < index; i++) StepForward();
    }
}
