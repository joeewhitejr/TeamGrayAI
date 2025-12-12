using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace Module8
{
    internal class TeamGrayAI : IPlayer
    {
        private static readonly List<Position> Guesses = new List<Position>();
        private int _index;
        private static readonly Random Random = new Random();
        private int _gridSize;
        private bool hitCheckedThisTurn = true;
        Position lastGuess = null;

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

            // 2D array of Boolean values to tell if a position is occupied
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
                    // Exclude the bottom row for the battleship
                    int y = horizontal ? Random.Next(gridSize - 1) : Random.Next(gridSize - ship.Length);

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

                    // Check for battleship
                    if (ship.IsBattleShip)
                    {
                        // Battleship must be on the bottom row
                        int by = gridSize - 1;

                        // Random X so the ship always fits horizontally
                        int bx = Random.Next(gridSize - ship.Length + 1);

                        // Mark occupied before placing
                        for (int i = 0; i < ship.Length; i++)
                        {
                            occupied[bx + i, by] = true;
                        }

                        ship.Place(new Position(bx, by), Direction.Horizontal);
                        placed = true;
                    }

                    else
                    {
                        // Place the ship
                        ship.Place(new Position(x, y), direction);
                        placed = true;
                    }
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
            hitCheckedThisTurn = false;
            Position guess;
            if (_targetQueue.Count > 0)
            {
                Debug.WriteLine("Next up in Queue: " + _targetQueue.Peek().X + ", " + _targetQueue.Peek().Y);
            }

            // If we have positions to target (from a previous hit), use them first
            if (_targetQueue.Count > 0)
            {
                guess = _targetQueue.Dequeue();
                bool goodGuess = false;
                // Make sure this guess exists in the target pool FIX THIS, NOT WORKING PROPERLY
                while (!goodGuess && (_targetQueue.Count > 0))
                {
                    if (!Guesses.Contains(guess))
                    {
                        goodGuess = true;
                    }
                    else
                    {
                        guess = _targetQueue.Dequeue();
                    }
                }
            }
            else
            {
                // Otherwise pick a random position
                guess = Guesses[Random.Next(Guesses.Count)];
            }

            // Remove the guessed position from the shared pool
            Debug.WriteLine("x: " + guess.X + "  y: " + guess.Y);
            lastGuess = guess;

            Guesses.Remove(guess);



            return guess;
        }
        public void SetAttackResults(List<AttackResult> results)
        {
            if (!hitCheckedThisTurn)
            {
                hitCheckedThisTurn = true;
                foreach (var result in results)
                {
                    if ((result.ResultType == AttackResultType.Hit) && (result.PlayerIndex != Index))
                    {
                        Debug.WriteLine("HIT DETECTED - Position: " + lastGuess.X + ", " + lastGuess.Y);
                        AddAdjacentTargets(lastGuess);
                    }
                }
            }
        }

        private void AddAdjacentTargets(Position lastGuess)
        {
            // Left, Right, Up, Down
            _targetQueue.Clear();

            Position[] adjacent = new Position[]
            {
                new Position(lastGuess.X + 1, lastGuess.Y),
                new Position(lastGuess.X - 1, lastGuess.Y),
                new Position(lastGuess.X, lastGuess.Y - 1),
                new Position(lastGuess.X, lastGuess.Y + 1)
            };
            Debug.WriteLine("Created: " + adjacent[0].X + ", " + adjacent[0].Y);
            Debug.WriteLine("Created: " + adjacent[1].X + ", " + adjacent[1].Y);
            Debug.WriteLine("Created: " + adjacent[2].X + ", " + adjacent[2].Y);
            Debug.WriteLine("Created: " + adjacent[3].X + ", " + adjacent[3].Y);

            foreach (var p in adjacent)
            {
                //Only add valid positions, previous use checking will happen in getAttackPosition()
                if ((p.X >= 0) && (p.X < _gridSize) && (p.Y >= 0) && (p.Y < _gridSize))
                {
                    _targetQueue.Enqueue(p);
                    Debug.WriteLine("Added to queue: " + p.X + ", " + p.Y);

                }
            }
        }
    }
}
