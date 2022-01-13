using System;
using System.Collections.Generic;
using System.Linq;

namespace WordleSolver
{
    class Program
    {
        static void Main(string[] args)
        {
            bool oneGame = true;

            if (oneGame)
            {
                var game = new Game(true);
                var run = game.Run();
            }
            else
            {
                List<int> results = new List<int>();
                for (int i = 0; i < 100; i++)
                {
                    var game = new Game(false);
                    var run = game.Run();
                    results.Add(run);
                }

                Console.WriteLine("Average:" + results.Average());

                var numberBeaten = results.Where(r => r <= 6).Count();
                Console.WriteLine("% beaten:" + ((100f * numberBeaten) / results.Count));
            }
        }
    }

    public class Game
    {
        private Random random = new Random();

        private string solution;

        private char[] knownPlaces = "-----".ToCharArray();
        private HashSet<char> unpositionedLetters = new HashSet<char>();
        private HashSet<char> excludedLetters = new HashSet<char>();

        public List<string> potentialSolutions;
        private bool shouldPrint;

        private List<string> log = new List<string>();

        public Game(bool shouldPrint)
        {
            this.shouldPrint = shouldPrint;
        }

        public int Run()
        {
            potentialSolutions = Words.Solutions.ToList();
            solution = Words.Solutions.SelectRandom();

            Print("Solution is " + solution + "\n\n\n");

            for (int i = 0; i < 10; i++)
            {
                var guessWord = ChooseGuess();

                var correct = Guess(guessWord);
                if (correct)
                {
                    Print("Correct! Word is " + guessWord);
                    Print("Got it in " + (i + 1) + " guesses");

                    return i + 1;
                    break;
                }
                else
                {
                    FilterWords();
                    PrintInfo();
                }
            }

            return 11;
        }

        private string ChooseGuess()
        {
            if(potentialSolutions.Count == 1)
            {
                return potentialSolutions.Single();
            }

            var bestWord = "";
            var bestScore = 0;
            foreach(var word in Words.allWords)
            {
                var score = 0;
                if (word.Any(c => excludedLetters.Contains(c))) continue;

                foreach (var potential in this.potentialSolutions)
                {
                    score += CompareWords(potential, word);
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
            var differentLetters = a.Intersect(b);
            return differentLetters.Count();
        }

        private void FilterWords()
        {
            var filteredPotentialWords = potentialSolutions.Where(w => FilterGuess(w)).ToList();

            potentialSolutions = filteredPotentialWords;
        }

        public bool FilterGuess(string word)
        {
            var unpositionedCheckCount = 0;
            for (int i = 0; i < word.Length; i++)
            {
                char w = word[i];
                if(excludedLetters.Contains(w)) { return false; }

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
            Print($"Known: {new string(knownPlaces)}, Unpositioned: {new string(unpositionedLetters.ToArray())}, Excluded: {new string(excludedLetters.ToArray())} \n" +
                $"{potentialSolutions.Count} potential words.\n\n");
        }

        private bool Guess(string guess)
        {
            Print($"Guessing " + guess + "");

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

            log.Add(msg);
        }
    }
}
