using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WordleSolver
{
    class Program
    {
        static void Main(string[] args)
        {
            bool oneGame = false;

            if (oneGame)
            {
                var game = new Game(true, "taste");
                var run = game.Run();
            }
            else
            {
                ConcurrentBag<Game> results = new ConcurrentBag<Game>();
                Parallel.For(0, Words.Solutions.Length / 3, (i) =>
                {
                    var solutionIndex = i * 3;
                    //if (solutionIndex == -1)
                    //{
                    //    solution = Words.Solutions.SelectRandom();
                    //}
                    //else
                    //{
                       var solution = Words.Solutions[solutionIndex % Words.Solutions.Length];
                    //}

                    var game = new Game(false, solution);
                    var run = game.Run();
                    results.Add(game);
                });

                Console.WriteLine("Average:" + results.Select(g => g.steps).Average());

                var numberBeaten = results.Where(r => r.steps <= 6).Count();
                Console.WriteLine("% beaten:" + ((100f * numberBeaten) / results.Count));

                Console.WriteLine("Logs for lost games:");
                var lost = results.Where(r => r.steps > 5);
                foreach (var lostGame in lost)
                {
                    foreach (var logEntry in lostGame.Log)
                    {
                        Console.WriteLine(logEntry);
                    }
                }
            }
        }
    }

    public class Game
    {
        private string solution;

        private char[] knownPlaces = "-----".ToCharArray();
        private HashSet<char> unpositionedLetters = new HashSet<char>();
        private HashSet<char> excludedLetters = new HashSet<char>();

        private HashSet<char>[] notInThisPositionLetters = new HashSet<char>[6] {
            new HashSet<char>(),
            new HashSet<char>(),
            new HashSet<char>(),
            new HashSet<char>(),
            new HashSet<char>(),
            new HashSet<char>(),
        };

        public HashSet<string> guesses = new HashSet<string>();

        public HashSet<string> potentialSolutions;
        private bool shouldPrint;

        public List<string> Log = new List<string>();

        public Game(bool shouldPrint, string solution)
        {
            this.shouldPrint = shouldPrint;
            this.solution = solution;
        }

        public int steps = 0;

        public int Run()
        {
            potentialSolutions = Words.Solutions.ToHashSet();

            Print("Solution is " + solution + "\n\n\n");

            for (int i = 0; i < 10; i++)
            {
                steps++;
                var guessWord = i == 0 ? "oater" : ChooseGuess();

                var correct = Guess(guessWord);
                if (correct)
                {
                    Print("Correct! Word is " + guessWord);
                    Print("Got it in " + (i + 1) + " guesses\n------------------------------------------------------------\n");

                    break;
                }
                else
                {
                    FilterWords();
                    PrintInfo();
                }
            }

            return steps;
        }

        private string ChooseGuess()
        {
            if(potentialSolutions.Count == 1)
            {
                return potentialSolutions.Single();
            }

            var bestWord = "";
            var bestScore = -10000;
            foreach(var word in Words.allWords)
            {
                var score = 0;
                //if (word.Any(c => excludedLetters.Contains(c))) score -= 1;
                if (guesses.Contains(word)) continue;

                foreach (var potential in this.potentialSolutions)
                {
                    score += CompareWords(potential, word);

                    if(potential == word)
                    {
                        score += 1;
                    }
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestWord = word;
                }
            }

            return bestWord;
        }

        public int CompareWords(string a, string b)
        {
            return a.Intersect(b).Except(knownPlaces).Count();

            //var h = new HashSet<char>(a);


            //foreach (char c in a + b)
            //{
            //    h.Add(c);
            //}

            //var count = 0;
            //foreach (var c in knownPlaces)
            //{
            //    if (c == '-') continue;
            //    if (h.Contains(c))
            //    {
            //        count++;
            //    }
            //}

            //return h.Count();// 2 * h.Count() - (count);
            //var differentLetters = a.Intersect(b).ToArray();
            //var except = (a + b).Intersect(knownPlaces).ToArray();

            //return (differentLetters.Length) - (except.Length * 2);
        }

        private void FilterWords()
        {
            var filteredPotentialWords = potentialSolutions.Where(w => FilterGuess(w)).ToHashSet();

            potentialSolutions = filteredPotentialWords;
        }

        public bool FilterGuess(string word)
        {
            var unpositionedCheckCount = 0;
            for (int i = 0; i < word.Length; i++)
            {
                char w = word[i];
                if(excludedLetters.Contains(w)) { return false; }

                if (notInThisPositionLetters[i].Contains(w))
                {
                    return false;
                }

                char known = knownPlaces[i];
                if (known != '-' && known != w) { return false; }

                if (unpositionedLetters.Contains(w))
                {
                    unpositionedCheckCount++;
                }
            }

            if(unpositionedCheckCount < unpositionedLetters.Count())
            {
                return false;
            }

            return true;
        }

        private void PrintInfo()
        {
            var solutions = "";
            if (potentialSolutions.Count < 10)
            {
                solutions = "(";
                foreach (var sol in potentialSolutions)
                {
                    solutions += sol + ", ";
                }
                solutions += ")";
            }

            Print($"Known: {new string(knownPlaces)}, Unpositioned: {new string(unpositionedLetters.ToArray())}, Excluded: {new string(excludedLetters.ToArray())} \n" +
                $"{potentialSolutions.Count} potential words. " + solutions + "\n\n");

        }

        private bool Guess(string guess)
        {
            Print($"Guessing " + guess + "");

            guesses.Add(guess);

            if (guess == solution)
            {
                knownPlaces = solution.ToCharArray();
                return true;
            }

            for (int i = 0; i < guess.Length; i++)
            {
                char guessChar = (char)guess[i];
                char solutionChar = (char)solution[i];
                
                if (guessChar == solutionChar)
                {
                    knownPlaces[i] = guessChar;
                    //unpositionedLetters.Remove(guessChar);

                    continue;
                }

                notInThisPositionLetters[i].Add(guessChar);

                if (solution.Contains(guessChar))
                {
                    unpositionedLetters.Add(guessChar);
                }
                else
                {
                    excludedLetters.Add(guessChar);
                }
            }

            return false;
        }

        private void Print(string msg)
        {
            if (shouldPrint)
            {
                Console.WriteLine(msg);
            }

            Log.Add(msg);
        }
    }
}
