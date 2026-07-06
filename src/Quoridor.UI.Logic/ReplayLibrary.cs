using Quoridor.Domain.AI;
using Quoridor.Domain.Core;

namespace Quoridor.UI.Logic;

public sealed record ReplayEntry(string Name, BoardVariant Variant, Difficulty P1Diff, Difficulty P2Diff, PlayerId Winner, int Plies, string Notation);

public static class ReplayLibrary
{
    public static IReadOnlyList<ReplayEntry> All { get; } = new[]
    {
        new ReplayEntry("Standard · Easy vs Easy", BoardVariant.Standard, Difficulty.Easy, Difficulty.Easy, PlayerId.P2, 14, "1. e2 e8 2. e3 e7 3. e4 e6 4. e5 e4 5. e6 e3 6. e7 e2 7. e8 e1"),
        new ReplayEntry("Standard · Easy vs Medium", BoardVariant.Standard, Difficulty.Easy, Difficulty.Medium, PlayerId.P2, 14, "1. e2 e8 2. e3 e7 3. e4 e6 4. e5 e4 5. e6 e3 6. e7 e2 7. e8 e1"),
        new ReplayEntry("Standard · Easy vs Hard", BoardVariant.Standard, Difficulty.Easy, Difficulty.Hard, PlayerId.P2, 14, "1. e2 e8 2. e3 e7 3. e4 e6 4. e5 e4 5. e6 e3 6. e7 e2 7. e8 e1"),
        new ReplayEntry("Standard · Medium vs Easy", BoardVariant.Standard, Difficulty.Medium, Difficulty.Easy, PlayerId.P1, 15, "1. e2 e8 2. e3 e7 3. e4 e6 4. d1h e5 5. e6 e4 6. e7 e3 7. e8 e2 8. e9"),
        new ReplayEntry("Standard · Medium vs Medium", BoardVariant.Standard, Difficulty.Medium, Difficulty.Medium, PlayerId.P1, 29, "1. e2 e8 2. e3 e7 3. e4 e6 4. d1h e7h 5. e2h d6 6. e5 d5 7. c5 d4 8. c6 d3 9. c7 d2 10. c8 b8h 11. b1h c7v 12. b7h c2 13. b8 d2 14. a8 b3v 15. a9"),
        new ReplayEntry("Standard · Medium vs Hard", BoardVariant.Standard, Difficulty.Medium, Difficulty.Hard, PlayerId.P2, 60, "1. e2 e8 2. e3 e7 3. e4 e6 4. d2h e8h 5. f2h c8h 6. b1h g8h 7. e5 e4 8. e6 e3 9. e7 g7v 10. h6h d7v 11. e6 d5v 12. f1v e5h 13. f6 f6v 14. g5v f4v 15. g3v e3v 16. c3v d3 17. c5v d4 18. f7 d5 19. f8 d6 20. g8 d7 21. g7 c7 22. g6 c6 23. g5 c5 24. g4 c4 25. g3 c3 26. f3 c2 27. f4 d2 28. d1h e2 29. f5 f2 30. f4 f1"),
        new ReplayEntry("Standard · Hard vs Easy", BoardVariant.Standard, Difficulty.Hard, Difficulty.Easy, PlayerId.P1, 17, "1. e2 e8 2. e3 e7 3. e4 e6 4. d2h e5 5. e6 e4 6. e7 e3 7. e3v d3 8. e8 c3 9. e9"),
        new ReplayEntry("Standard · Hard vs Medium", BoardVariant.Standard, Difficulty.Hard, Difficulty.Medium, PlayerId.P2, 72, "1. e2 e8 2. e3 e7 3. e4 e6 4. d2h e7h 5. f2h d8h 6. b2h e5 7. e6 e4 8. d3v h2v 9. h1h e5v 10. e4h f4 11. g1v g3v 12. f6h g4 13. a1h g5 14. g7v g6 15. e7 h6 16. d7 h7 17. d8 h8 18. c8 b8h 19. d8 h9 20. c8 g9 21. d8 g8 22. c8 g7 23. d8 f7 24. c8 e7 25. d8 e6 26. c8 e5 27. d8 d5 28. c8 d4 29. d8 d3 30. c8 c3 31. d8 b3 32. c8 a3 33. d8 a2 34. c8 b2 35. d8 c2 36. e8 c1"),
        new ReplayEntry("Standard · Hard vs Hard", BoardVariant.Standard, Difficulty.Hard, Difficulty.Hard, PlayerId.P1, 71, "1. e2 e8 2. e3 e7 3. e4 e6 4. d2h e8h 5. f2h c8h 6. b2h g8h 7. h2h a1v 8. e5 e4 9. a3v a6h 10. b4v d4 11. e6 c4 12. c3h d4 13. a5h e4 14. e3h f4 15. f4v f5 16. f6 g7v 17. f7 b7v 18. g7 f6h 19. f7 d6h 20. e7 e5v 21. d7 f6 22. c7 g6 23. c6 g5 24. c5 g4 25. c4 g3 26. d4 f3 27. e4 e3 28. f4 d3 29. f5 c3 30. f6 b3 31. g6 b4 32. h6 b5 33. h7 a5 34. h8 a4 35. i8 a3 36. i9"),
        new ReplayEntry("Kid · Easy vs Easy", BoardVariant.Kid, Difficulty.Easy, Difficulty.Easy, PlayerId.P2, 10, "1. d2 d6 2. d3 d5 3. d4 d3 4. d5 d2 5. d6 d1"),
        new ReplayEntry("Kid · Easy vs Medium", BoardVariant.Kid, Difficulty.Easy, Difficulty.Medium, PlayerId.P2, 10, "1. d2 d6 2. d3 d5 3. d4 d3 4. d5 d2 5. d6 d1"),
        new ReplayEntry("Kid · Easy vs Hard", BoardVariant.Kid, Difficulty.Easy, Difficulty.Hard, PlayerId.P2, 10, "1. d2 d6 2. d3 d5 3. d4 d3 4. d5 d2 5. d6 d1"),
        new ReplayEntry("Kid · Medium vs Easy", BoardVariant.Kid, Difficulty.Medium, Difficulty.Easy, PlayerId.P1, 11, "1. d2 d6 2. d3 d5 3. d2h d4 4. d5 d3 5. d6 c3 6. d7"),
        new ReplayEntry("Kid · Medium vs Medium", BoardVariant.Kid, Difficulty.Medium, Difficulty.Medium, PlayerId.P1, 39, "1. d2 d6 2. d3 d5 3. d2h d3h 4. c3 d4 5. c4 b4 6. c5 b3 7. c6 b6h 8. b1h b2 9. a1v d6h 10. c2v b3 11. a2h b4 12. c4v b5 13. b5h a5 14. b6 a5v 15. d5h a6 16. c6 a7 17. d6 b7 18. e6 c7 19. f6 d7 20. f7"),
        new ReplayEntry("Kid · Medium vs Hard", BoardVariant.Kid, Difficulty.Medium, Difficulty.Hard, PlayerId.P2, 38, "1. d2 d6 2. d3 d5 3. d2h c5h 4. c1h e5h 5. d4 d3 6. d5 e4v 7. d4 a5h 8. d1v d3h 9. a4v f6h 10. c4 d6h 11. c3 b6h 12. e3 f3 13. f2h d3 14. b2v c3 15. f3 c4 16. f4 b4 17. g4 b3 18. g5 b2 19. a1v b1"),
        new ReplayEntry("Kid · Hard vs Easy", BoardVariant.Kid, Difficulty.Hard, Difficulty.Easy, PlayerId.P1, 15, "1. d2 d6 2. d3 d5 3. d2h d4 4. d5 d3 5. c3v e3 6. f2h e4 7. d6 e5 8. d7"),
        new ReplayEntry("Kid · Hard vs Medium", BoardVariant.Kid, Difficulty.Hard, Difficulty.Medium, PlayerId.P1, 33, "1. d2 d6 2. d3 d5 3. c2h c5h 4. e2h d3h 5. c4v e5 6. f3h e6 7. a2h d6 8. b5v d7 9. f1h c7 10. d1h b7 11. c3 b6 12. c4 b5 13. b4 b3 14. b5 c3 15. b6 b6h 16. a6 d3 17. a7"),
        new ReplayEntry("Kid · Hard vs Hard", BoardVariant.Kid, Difficulty.Hard, Difficulty.Hard, PlayerId.P1, 37, "1. d2 d6 2. d3 d5 3. d2h c6h 4. b2h e6h 5. f2h b1h 6. c5v a6h 7. d4h f5h 8. a3v e5 9. b4v d3h 10. c2v f5 11. e3 f4 12. f3 e4 13. f4 d4 14. f5 c4 15. e5 c3 16. e6 b3 17. f6 b4 18. g6 e1h 19. g7"),
    };
}
