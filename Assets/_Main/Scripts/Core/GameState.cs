using System;

namespace BlobRunner.Core
{
    /// <summary>High level phases of a run. Driven by <c>GameManager</c>.</summary>
    public enum GameState
    {
        /// <summary>Scene is loading / systems are being created.</summary>
        Boot = 0,

        /// <summary>Waiting for the player to press "Play".</summary>
        Menu = 1,

        /// <summary>The blob is running.</summary>
        Playing = 2,

        /// <summary>Run is suspended (pause menu, app lost focus).</summary>
        Paused = 3,

        /// <summary>The finish line has been crossed.</summary>
        Won = 4,

        /// <summary>Every body part has been cut off.</summary>
        Lost = 5
    }

    /// <summary>
    /// Tiny validated state machine. It owns no Unity types so it can be unit tested and reused.
    /// Won / Lost are terminal: a new run is started by reloading the scene.
    /// </summary>
    public sealed class GameStateMachine
    {
        public GameState Current { get; private set; } = GameState.Boot;

        /// <summary>Raised after a successful transition: (from, to).</summary>
        public event Action<GameState, GameState> Changed;

        public static bool IsAllowed(GameState from, GameState to)
        {
            switch (from)
            {
                case GameState.Boot:
                    return to == GameState.Menu || to == GameState.Playing;
                case GameState.Menu:
                    return to == GameState.Playing;
                case GameState.Playing:
                    return to == GameState.Paused || to == GameState.Won || to == GameState.Lost;
                case GameState.Paused:
                    return to == GameState.Playing;
                default:
                    return false; // Won and Lost are terminal
            }
        }

        public bool CanTransitionTo(GameState next)
        {
            return IsAllowed(Current, next);
        }

        /// <summary>Moves to <paramref name="next"/> if the transition is legal. Returns false (and does nothing) otherwise.</summary>
        public bool TryTransition(GameState next)
        {
            if (!IsAllowed(Current, next))
                return false;

            var previous = Current;
            Current = next;

            var handler = Changed;
            if (handler != null)
                handler(previous, next);

            return true;
        }
    }
}
