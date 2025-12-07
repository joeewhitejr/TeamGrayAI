using System;
using System.Collections.Generic;

namespace Module8
{
    internal class TeamGrayAI : IPlayer
    {
        private static readonly List<Position> Guesses = new List<Position>();
        private int _index;
        private static readonly Random Random = new Random();
        private int _gridSize;

        public TeamGrayAI(string name)
        {
            Name = name;
        }

        public void StartNewGame(int playerIndex, int gridSize, Ships ships)
        {
            _gridSize = gridSize;
            _index = playerIndex;

            GenerateGuesses();

            // Random player just puts the ships in the grid in Random columns
            // Note it cannot deal with the case where there's not enough columns
            // for 1 per ship
            var availableColumns = new List<int>();
            for (int i = 0; i < gridSize; i++)
            {
                availableColumns.Add(i);
            }

            // 2D array of Boolen values to tell if a position is occupied
            bool[,] occupied = new bool[gridSize, gridSize];

            foreach (var ship in ships._ships)
            {
                bool placed = false;

                while (!placed)
                {
                    bool horizontal = Random.Next(2) == 0;
                    Direction direction = horizontal ? Direction.Horizontal : Direction.Vertical;

                    // Random start, adjusted so ship fits
                    int x = horizontal ? Random.Next(gridSize - ship.Length + 1) : Random.Next(gridSize);
                    int y = horizontal ? Random.Next(gridSize) : Random.Next(gridSize - ship.Length + 1);

                    // Check for overlap
                    bool collision = false;
                    for (int i = 0; i < ship.Length; i++)
                    {
                        // Current x and y to check
                        int cx = horizontal ? x + i : x;
                        int cy = horizontal ? y : y + i;
                        if (occupied[cx, cy])
                        {
                            collision = true;
                            break;
                        }
                    }

                    if (collision) continue;

                    // Mark occupied cells
                    for (int i = 0; i < ship.Length; i++)
                    {
                        int cx = horizontal ? x + i : x;
                        int cy = horizontal ? y : y + i;
                        occupied[cx, cy] = true;
                    }

                    // Place the ship
                    ship.Place(new Position(x, y), direction);
                    placed = true;
                }
            }
        }

        private void GenerateGuesses()
        {
            // We want all instances of TeamGrayAI to share the same pool of guesses
            // So they don't repeat each other.

            // We need to populate the guesses list, but not for every instance - so we only do it if the set is missing some guesses
            if (Guesses.Count < _gridSize * _gridSize)
            {
                Guesses.Clear();
                for (int x = 0; x < _gridSize; x++)
                {
                    for (int y = 0; y < _gridSize; y++)
                    {
                        Guesses.Add(new Position(x, y));
                    }
                }
            }
        }

        public string Name { get; }
        public int Index => _index;

        private readonly Queue<Position> _targetQueue = new Queue<Position>();
        public Position GetAttackPosition()
        {
            Position guess;

            // If we have positions to target (from a previous hit), use them first
            if (_targetQueue.Count > 0)
            {
                guess = _targetQueue.Dequeue();
                // Make sure this guess hasn't already been used
                if (!Guesses.Contains(guess))
                    return GetAttackPosition(); // skip invalid positions
            }
            else
            {
                // Otherwise pick a random position
                guess = Guesses[Random.Next(Guesses.Count)];
            }

            // Remove the guessed position from the shared pool
            Guesses.Remove(guess);

            return guess;
        }

        public void SetAttackResults(List<AttackResult> results)
        {
            foreach (var result in results)
            {
                if (result.ResultType == AttackResultType.Hit)
                {
                    AddAdjacentTargets(result.Position);
                }
            }
        }

        private void AddAdjacentTargets(Position pos)
        {
            // Left, Right, Up, Down
            Position[] adjacent = new Position[]
            {
                new Position(pos.X + 1, pos.Y),
                new Position(pos.X - 1, pos.Y),
                new Position(pos.X, pos.Y - 1),
                new Position(pos.X, pos.Y + 1)
            };

            foreach (var p in adjacent)
            {
                // Only add valid positions that haven't been guessed yet
                if (p.X >= 0 && p.X < _gridSize && p.Y >= 0 && p.Y < _gridSize && Guesses.Contains(p))
                {
                    _targetQueue.Enqueue(p);
                }
            }
        }
    }
}
