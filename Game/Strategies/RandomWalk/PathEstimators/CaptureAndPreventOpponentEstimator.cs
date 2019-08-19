using Game.Protocol;
using Game.Sim;
using Game.Sim.Undo;

namespace Game.Strategies.RandomWalk.PathEstimators
{
    public class CaptureAndPreventOpponentEstimator : IPathEstimator
    {
        private int prevCaptured;
        private readonly int[] prevScores = new int[6];
        private int prevTime;
        private readonly Direction[] commands = new Direction[6];
#if DEBUG
        private readonly StateBackup backup = new StateBackup();
        private readonly int[] pathsBackup = new int[6];
#endif

        public void Before(State state, int player)
        {
            prevCaptured = state.players[player].opponentTerritoryCaptured;
            for (int i = 0; i < state.players.Length; i++)
                prevScores[i] = state.players[i].score;
            prevTime = state.time;
        }

        public double Estimate(State state, InterestingFacts facts, int player, int pathStartLen)
        {
            double baseScore;
            baseScore = 0;
            for (var i = 0; i < state.players.Length; i++)
            {
                if (i != player
                    && state.players[i].status == PlayerStatus.Eliminated
                    && (state.players[i].killedBy & (1 << player)) != 0)
                    baseScore += 1_000_000_000;
            }

            var nitroTimeBonus = state.players[player].nitrosCollected * 30 * (Env.TICKS_PER_REQUEST - Env.NITRO_TICKS_PER_REQUEST);
            var nitroScoreBonus = nitroTimeBonus / Env.TICKS_PER_REQUEST;

            var slowTimePenalty = state.players[player].slowsCollected * 30 * (Env.SLOW_TICKS_PER_REQUEST - Env.TICKS_PER_REQUEST);
            var slowScorePenalty = slowTimePenalty / Env.TICKS_PER_REQUEST;

            var opponentCaptured = state.players[player].opponentTerritoryCaptured - prevCaptured;
            if (pathStartLen > 0 && opponentCaptured == 0)
                return int.MinValue;

            var score = state.players[player].score - prevScores[player];
            var time = state.time - prevTime;

            if (state.time < Env.MAX_TICK_COUNT - 100)
            {
                time += slowTimePenalty - nitroTimeBonus;
                score += nitroScoreBonus - slowScorePenalty;
            }

            if (opponentCaptured > 0)
            {
#if DEBUG
                backup.Backup(state);
                for (int i = 0; i < state.players.Length; i++)
                    pathsBackup[i] = facts.pathsToOwned[i].len;
#endif
                while (!state.isGameOver)
                {
                    var ended = 0;
                    for (var i = 0; i < state.players.Length; i++)
                    {
                        if (state.players[i].status == PlayerStatus.Eliminated || state.players[i].status == PlayerStatus.Broken)
                        {
                            ended++;
                            continue;
                        }

                        if (state.players[i].arriveTime != 0)
                        {
                            if (facts.pathsToOwned[i].len < 0)
                                ended++;
                            continue;
                        }

                        if (facts.pathsToOwned[i].len <= 0)
                            ended++;

                        commands[i] = facts.pathsToOwned[i].ApplyNext(state, i);
                    }

                    if (ended == state.players.Length)
                        break;

                    state.NextTurn(commands, false);
                }

                var sumOpponentScore = 0;
                for (int i = 0; i < state.players.Length; i++)
                {
                    if (i == player || state.players[i].status == PlayerStatus.Eliminated || facts.places[i] > 2)
                        continue;

                    var opponentScore = state.players[i].score - prevScores[i] - facts.potentialScores[i];
                    sumOpponentScore += opponentScore;
                }
#if DEBUG
                backup.Restore(state);
                for (int i = 0; i < state.players.Length; i++)
                    facts.pathsToOwned[i].len = pathsBackup[i];
#endif

                return baseScore + 10_000_000.0 + 100_000.0 * opponentCaptured - sumOpponentScore / 5.0 * 10_000.0 - 100.0 * time + score;
            }

            return baseScore + score;
        }
    }
}