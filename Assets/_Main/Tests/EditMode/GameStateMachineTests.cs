using System;
using System.Collections.Generic;
using BlobRunner.Core;
using NUnit.Framework;

namespace BlobRunner.Tests
{
    public class GameStateMachineTests
    {
        [Test]
        public void StartsInBoot()
        {
            Assert.AreEqual(GameState.Boot, new GameStateMachine().Current);
        }

        [Test]
        public void FullRunFollowsTheLegalPath()
        {
            var machine = new GameStateMachine();
            var seen = new List<string>();
            machine.Changed += (from, to) => seen.Add(from + ">" + to);

            Assert.True(machine.TryTransition(GameState.Menu));
            Assert.True(machine.TryTransition(GameState.Playing));
            Assert.True(machine.TryTransition(GameState.Paused));
            Assert.True(machine.TryTransition(GameState.Playing));
            Assert.True(machine.TryTransition(GameState.Won));

            Assert.AreEqual(GameState.Won, machine.Current);
            Assert.AreEqual("Boot>Menu,Menu>Playing,Playing>Paused,Paused>Playing,Playing>Won", string.Join(",", seen.ToArray()));
        }

        [Test]
        public void BootCanStartPlayingDirectly()
        {
            // used when a run is restarted: the menu is skipped
            var machine = new GameStateMachine();
            Assert.True(machine.TryTransition(GameState.Playing));
        }

        [TestCase(GameState.Boot, GameState.Won)]
        [TestCase(GameState.Boot, GameState.Lost)]
        [TestCase(GameState.Menu, GameState.Won)]
        [TestCase(GameState.Menu, GameState.Paused)]
        [TestCase(GameState.Paused, GameState.Won)]
        [TestCase(GameState.Paused, GameState.Lost)]
        [TestCase(GameState.Won, GameState.Playing)]
        [TestCase(GameState.Lost, GameState.Playing)]
        [TestCase(GameState.Playing, GameState.Menu)]
        [TestCase(GameState.Playing, GameState.Playing)]
        public void IllegalTransitionsAreRejected(GameState from, GameState to)
        {
            Assert.False(GameStateMachine.IsAllowed(from, to));
        }

        [Test]
        public void RejectedTransitionDoesNotChangeStateOrRaiseEvent()
        {
            var machine = new GameStateMachine();
            int raised = 0;
            machine.Changed += (a, b) => raised++;

            Assert.False(machine.TryTransition(GameState.Won));
            Assert.AreEqual(GameState.Boot, machine.Current);
            Assert.AreEqual(0, raised);
        }

        [Test]
        public void WonAndLostAreTerminal()
        {
            foreach (GameState to in Enum.GetValues(typeof(GameState)))
            {
                Assert.False(GameStateMachine.IsAllowed(GameState.Won, to), "Won -> " + to);
                Assert.False(GameStateMachine.IsAllowed(GameState.Lost, to), "Lost -> " + to);
            }
        }
    }
}
