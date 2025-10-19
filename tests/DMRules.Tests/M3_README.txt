M3 Starter: Engine Integration
================================

このパックは、互換レイヤ（StaticCompat）に依存せずに呼び出せる
正式な Engine API を導入するためのスターターです。

追加される API（DMRules.Engine 名前空間）
----------------------------------------
- TurnSystem.Step(IGameState state, object phase)
- TurnSystem.Step(IGameState state, object phase, object priority)
- ReplacementSystem.ApplyReplacement(IGameState state, object replacement)

設計方針
--------
- 既存の State/MinimalState の構造を崩さず、段階的に移行可能。
- エンジン内に既存の Step/ApplyReplacement 実装がある場合は、
  自動的に検出してそちらを優先。
- 無い場合でも、MinimalState/State の SetPhase を使って前進するため、
  現行の M2.5 シナリオは維持されます。

段階的な置換の流れ（推奨）
--------------------------
1) テストで StaticCompat 経由の呼び出しを、順次 TurnSystem/ReplacementSystem に切替
   - using static DMRules.Engine.TurnSystem;
   - using static DMRules.Engine.ReplacementSystem;
2) すべてのテストが Engine API で通ることを確認
3) tests/DMRules.Tests/StaticCompat.cs 等の Compat を削除

補足
----
- 本スターターは既存コードを壊さない追加のみです。
- 実務で Step のロジックが確立したら、TurnSystem.Step 内を
  直接実装に差し替えてください（反射部分は撤去可能）。
