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
                var game = new Game(true, "fizzy");
                var run = game.Run();
            }
            else
            {
                ConcurrentBag<Game> results = new ConcurrentBag<Game>();
                Parallel.For(0, Words.Solutions.Length, (i) =>
                {
                    var solutionIndex = i;
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

    public class Knowledge
    {
        public string pretendSolution;

        public char[] knownPlaces;
        public HashSet<char> unpositionedLetters;
        public HashSet<char> excludedLetters;
        public HashSet<char>[] notInThisPositionLetters;

        public HashSet<string> potentialSolutions;


        public Knowledge(char[] knownPlaces, HashSet<char> unpositionedLetters, HashSet<char> excludedLetters, HashSet<char>[] notInThisPositionLetters)
        {
            this.knownPlaces = knownPlaces;
            this.unpositionedLetters = unpositionedLetters;
            this.excludedLetters = excludedLetters;
            this.notInThisPositionLetters = notInThisPositionLetters;
            this.potentialSolutions = new HashSet<string>();
        }

        public Knowledge (Knowledge baseKnowledge, string pretendSolution)
        {
            this.pretendSolution = pretendSolution;

            this.knownPlaces = new string(baseKnowledge.knownPlaces).ToCharArray();
            this.unpositionedLetters = new HashSet<char>(baseKnowledge.unpositionedLetters);
            this.excludedLetters = new HashSet<char>(baseKnowledge.excludedLetters);
            this.notInThisPositionLetters =
                new HashSet<char>[6] {
                new HashSet<char>(baseKnowledge.notInThisPositionLetters[0]),
                new HashSet<char>(baseKnowledge.notInThisPositionLetters[1]),
                new HashSet<char>(baseKnowledge.notInThisPositionLetters[2]),
                new HashSet<char>(baseKnowledge.notInThisPositionLetters[3]),
                new HashSet<char>(baseKnowledge.notInThisPositionLetters[4]),
                new HashSet<char>(baseKnowledge.notInThisPositionLetters[5]),
            };

            this.potentialSolutions = new HashSet<string>(baseKnowledge.potentialSolutions);
        }
    }

    public class Game
    {
        private string realSolution;

        public HashSet<string> guesses = new HashSet<string>();

        private bool shouldPrint;

        public List<string> Log = new List<string>();

        public Game(bool shouldPrint, string solution)
        {
            this.shouldPrint = shouldPrint;
            this.realSolution = solution;
        }

        public int steps = 0;

        public int Run()
        {
            var knowledge = new Knowledge("-----".ToCharArray(), new HashSet<char>(), new HashSet<char>(), new HashSet<char>[6] { new HashSet<char>(), new HashSet<char>(), new HashSet<char>(), new HashSet<char>(), new HashSet<char>(), new HashSet<char>(), });
            knowledge.potentialSolutions = Words.Solutions.ToHashSet();

            Print("Solution is " + realSolution + "\n\n\n");

            for (int i = 0; i < 10; i++)
            {
                steps++;
                var guessWord = i == 0 ? "oater" : ChooseGuess(knowledge);

                Print("Guessing " + guessWord);
                var correct = Guess(guessWord, knowledge);
                if (correct)
                {
                    Print("Correct! Word is " + guessWord);
                    Print("Got it in " + (i + 1) + " guesses\n------------------------------------------------------------\n");

                    break;
                }
                else
                {
                    FilterWords(knowledge);
                    PrintInfo(knowledge);
                }
            }

            return steps;
        }

        private string ChooseGuess(Knowledge knowledge)
        {
            if(knowledge.potentialSolutions.Count <= 2)
            {
                return knowledge.potentialSolutions.First();
            }

            var bestWord = "";

            if (knowledge.potentialSolutions.Count > 10)
            {
                var bestScore = -10000;
                foreach (var word in Words.allWords)
                {
                    var score = 0;
                    if (guesses.Contains(word)) continue;

                    foreach (var potential in knowledge.potentialSolutions)
                    {
                        score += CompareWords(potential, word, knowledge);

                        if (potential == word)
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
            }
            else
            {
                var bestScore = 1000000;
                foreach (var word in Words.allWords)
                {
                    var worstCaseCount = 0;
                    foreach (var potential in knowledge.potentialSolutions)
                    {
                        var fakeKnowledge = new Knowledge(knowledge, potential);
                        Guess(word, fakeKnowledge);
                        FilterWords(fakeKnowledge);
                        var count = fakeKnowledge.potentialSolutions.Count();
                        if(count > worstCaseCount)
                        {
                            worstCaseCount = count;
                        }
                        if(worstCaseCount >= knowledge.potentialSolutions.Count())
                        {
                            goto skip;
                        }
                    }

                    if(worstCaseCount < bestScore)
                    {
                        bestWord = word;
                        bestScore = worstCaseCount;
                    }

                    skip:;
                }
            }


            return bestWord;
        }

        public int CompareWords(string a, string b, Knowledge knowledge)
        {
            return a.Intersect(b).Except(knowledge.knownPlaces).Count();

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

        private void FilterWords(Knowledge knowledge)
        {
            var filteredPotentialWords = knowledge.potentialSolutions.Where(w => FilterGuess(w, knowledge)).ToHashSet();

            knowledge.potentialSolutions = filteredPotentialWords;
        }

        public bool FilterGuess(string word, Knowledge knowledge)
        {
            var unpositionedCheckCount = 0;
            for (int i = 0; i < word.Length; i++)
            {
                char w = word[i];
                if(knowledge.excludedLetters.Contains(w)) { return false; }

                if (knowledge.notInThisPositionLetters[i].Contains(w))
                {
                    return false;
                }

                char known = knowledge.knownPlaces[i];
                if (known != '-' && known != w) { return false; }

                if (knowledge.unpositionedLetters.Contains(w))
                {
                    unpositionedCheckCount++;
                }
            }

            if(unpositionedCheckCount < knowledge.unpositionedLetters.Count())
            {
                return false;
            }

            return true;
        }

        private void PrintInfo(Knowledge knowledge)
        {
            var solutions = "";
            if (knowledge.potentialSolutions.Count < 10)
            {
                solutions = "(";
                foreach (var sol in knowledge.potentialSolutions)
                {
                    solutions += sol + ", ";
                }
                solutions += ")";
            }

            Print($"Known: {new string(knowledge.knownPlaces)}, Unpositioned: {new string(knowledge.unpositionedLetters.ToArray())}, Excluded: {new string(knowledge.excludedLetters.ToArray())} \n" +
                $"{knowledge.potentialSolutions.Count} potential words. " + solutions + "\n\n");

        }

        private bool Guess(string guess, Knowledge knowledge)
        {
            //Print($"Guessing " + guess + "");

            var solution = knowledge.pretendSolution ?? realSolution;

            guesses.Add(guess);

            if (guess == solution)
            {
                knowledge.knownPlaces = solution.ToCharArray();
                return true;
            }

            for (int i = 0; i < guess.Length; i++)
            {
                char guessChar = (char)guess[i];
                char solutionChar = (char)solution[i];
                
                if (guessChar == solutionChar)
                {
                    knowledge.knownPlaces[i] = guessChar;
                    //unpositionedLetters.Remove(guessChar);

                    continue;
                }

                knowledge.notInThisPositionLetters[i].Add(guessChar);

                if (solution.Contains(guessChar))
                {
                    knowledge.unpositionedLetters.Add(guessChar);
                }
                else
                {
                    knowledge.excludedLetters.Add(guessChar);
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
