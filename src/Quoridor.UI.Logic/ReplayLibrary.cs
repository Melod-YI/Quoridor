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
        new ReplayEntry("Standard · Medium vs Medium", BoardVariant.Standard, Difficulty.Medium, Difficulty.Medium, PlayerId.P1, 75, "1. e2 e8 2. e3 e7 3. e4 e6 4. d1h e7h 5. e2h d6 6. e5 d5 7. e6 d4 8. e7 d3 9. b1h d2 10. d2v c2 11. a2v c3 12. a3h d4h 13. c3v c4 14. b4h c7h 15. b2h b4 16. a5v a4 17. d7 a5 18. c7 a7h 19. c6 c6v 20. c5 g7h 21. d5 h8h 22. d6 d6v 23. d5 f8h 24. e5 d8h 25. f5 a6 26. f6 a7 27. f7 b7 28. g7 b6 29. h7 b5 30. i7 c5 31. i8 c6 32. h8 c7 33. g8 c6 34. f8 c5 35. e8 d5 36. d8 e5 37. c8 f5 38. c9"),
        new ReplayEntry("Standard · Medium vs Hard", BoardVariant.Standard, Difficulty.Medium, Difficulty.Hard, PlayerId.P1, 75, "1. e2 e8 2. e3 e7 3. e4 e6 4. d1h e7h 5. e2h c7h 6. f4 d6 7. f5 d5 8. g5 f5h 9. b1h h5h 10. g4 f4v 11. e1v g3h 12. h2h d5h 13. c6v b5h 14. b7v e3v 15. h4 a4v 16. i4 e5 17. i3 f5 18. h3 f4 19. g3 f3 20. f1h h3 21. g1v i3 22. f3 i4 23. a1v i5 24. f4 h5 25. f5 h4 26. e5 g4 27. d5 g5 28. d4 g4 29. d3 h4 30. c3 i4 31. b3 i3 32. a3 h3 33. a4 g3 34. a5 g2 35. a6 f2 36. a7 g2 37. a8 f2 38. a9"),
        new ReplayEntry("Standard · Hard vs Easy", BoardVariant.Standard, Difficulty.Hard, Difficulty.Easy, PlayerId.P1, 63, "1. e2 e8 2. e3 e7 3. e4 e6 4. d3h e5 5. e6 e4 6. e4v d4 7. b3h c4 8. a2h b4 9. a3v b5 10. f5h b6 11. f6v b7 12. g7h b8 13. b8v d6h 14. b6v e6v 15. d6 c5v 16. d5 c4h 17. e5 a8h 18. e4 c7h 19. d4 d8h 20. c4 f8h 21. b4 c8v 22. b5 a1h 23. c5 a8 24. c6 a7 25. c7 a6 26. d7 a5 27. e7 a4 28. e8 a3 29. f8 a4 30. g8 a5 31. h8 b5 32. h9"),
        new ReplayEntry("Standard · Hard vs Medium", BoardVariant.Standard, Difficulty.Hard, Difficulty.Medium, PlayerId.P2, 64, "1. e2 e8 2. e3 e7 3. e4 e6 4. d1h d4h 5. f4 e5 6. e5v f5h 7. d5v h6h 8. d7v f4h 9. b2h b6h 10. e7v g8h 11. g3h h4h 12. f3 c3v 13. a1h a8h 14. f2 d2v 15. f1 e6 16. h3v e7 17. d6h e8 18. g1 e9 19. h1 d9 20. i1 d8 21. i2 d7 22. h2 c7 23. g2 b7 24. g3 a7 25. h3 a6 26. h2 a5 27. h1 a4 28. g1 a3 29. f1 a2 30. e1 b2 31. d1 c2 32. c1 d1"),
        new ReplayEntry("Standard · Hard vs Hard", BoardVariant.Standard, Difficulty.Hard, Difficulty.Hard, PlayerId.P1, 59, "1. e2 e8 2. e3 e7 3. e4 e6 4. d2h d6h 5. f2h f6h 6. b2h b6h 7. h2h a7h 8. d5v h6h 9. e4h g4v 10. a2v c7h 11. a4v e7h 12. f4v h4h 13. d4 g7h 14. d5 f6 15. d6 g6 16. c6 g5 17. b6 g4 18. a6 g3 19. a7 f3 20. b7 f4 21. c7 e4 22. d7 d4 23. e7 c4 24. b4h b4 25. f7 b3 26. g7 c3 27. h7 d3 28. i7 e3 29. i8 e4 30. i9"),
        new ReplayEntry("Kid · Easy vs Easy", BoardVariant.Kid, Difficulty.Easy, Difficulty.Easy, PlayerId.P2, 10, "1. d2 d6 2. d3 d5 3. d4 d3 4. d5 d2 5. d6 d1"),
        new ReplayEntry("Kid · Easy vs Medium", BoardVariant.Kid, Difficulty.Easy, Difficulty.Medium, PlayerId.P2, 10, "1. d2 d6 2. d3 d5 3. d4 d3 4. d5 d2 5. d6 d1"),
        new ReplayEntry("Kid · Easy vs Hard", BoardVariant.Kid, Difficulty.Easy, Difficulty.Hard, PlayerId.P2, 10, "1. d2 d6 2. d3 d5 3. d4 d3 4. d5 d2 5. d6 d1"),
        new ReplayEntry("Kid · Medium vs Easy", BoardVariant.Kid, Difficulty.Medium, Difficulty.Easy, PlayerId.P1, 11, "1. d2 d6 2. d3 d5 3. d2h d4 4. d5 d3 5. d6 c3 6. d7"),
        new ReplayEntry("Kid · Medium vs Medium", BoardVariant.Kid, Difficulty.Medium, Difficulty.Medium, PlayerId.P1, 27, "1. d2 d6 2. d3 d5 3. c1h c5h 4. e1h d6h 5. e3 d4 6. e4 d3 7. e5 d2 8. e6 f6h 9. a5h b6h 10. d6 e2 11. c6 a1v 12. b6 a4v 13. a6 f2 14. a7"),
        new ReplayEntry("Kid · Medium vs Hard", BoardVariant.Kid, Difficulty.Medium, Difficulty.Hard, PlayerId.P2, 30, "1. d2 d6 2. d3 d5 3. d2h d5h 4. c1h b5h 5. d4 d3 6. d5 a4v 7. c5 f5h 8. a1v b4h 9. b2h d4h 10. e1h a6h 11. c6v c3 12. b3h b3 13. f2h a3 14. b5 a2 15. c5 a1"),
        new ReplayEntry("Kid · Hard vs Easy", BoardVariant.Kid, Difficulty.Hard, Difficulty.Easy, PlayerId.P1, 11, "1. d2 d6 2. d3 d5 3. d1h d4 4. d5 d3 5. d6 d2 6. d7"),
        new ReplayEntry("Kid · Hard vs Medium", BoardVariant.Kid, Difficulty.Hard, Difficulty.Medium, PlayerId.P2, 60, "1. d2 d6 2. d3 d5 3. c2h c6h 4. e2h d5h 5. a1h e5 6. d4 f5 7. d3h f4 8. f3h f5h 9. b3h e4 10. a3v c4 11. b1v c5 12. c4 b4h 13. b4 d4h 14. c4 b5 15. d4 a5 16. e4 a4 17. f4 a3 18. f5 a2 19. e5 b2 20. d5 b3 21. c5 c3 22. c6 d3 23. d6 e3 24. e6 e6h 25. f6 f6v 26. e6 b6v 27. d6 f3 28. c6 g3 29. c5 g2 30. b5 g1"),
        new ReplayEntry("Kid · Hard vs Hard", BoardVariant.Kid, Difficulty.Hard, Difficulty.Hard, PlayerId.P1, 45, "1. d2 d6 2. d3 d5 3. d2h c5h 4. b2h e5h 5. f2h a5h 6. a3v b4h 7. a1h e3v 8. e4h c3h 9. c1h d3v 10. e1h f6h 11. c3 c5 12. b3 b5 13. b4 a5 14. c4 a4 15. d4 a3 16. d5 a2 17. e5 b2 18. f5 c2 19. g5 d2 20. g6 e2 21. f6 f2 22. e6 g2 23. e7"),
    };
}
