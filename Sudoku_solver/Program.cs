using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Sudoku_solver
{
    class Sudoku_Solver
    {
        // Four different datastructures for representing the Sudoku State
        static int[][] sudokuRows;
        static int[][] sudokuColumns;
        static int[][] blocks;
        static bool[,] fixedVals;
        // Additional Globals
        static Random rand;
        static Stopwatch watch;
        static int N, sqrN;
        /// <summary>
        /// Entry Point and startup logic
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            watch = new Stopwatch();
            watch.Start();
            // Sets language of error messages to English (Development enviroment's default culture is "jp")
#if DEBUG
            if (Debugger.IsAttached)
                System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
#endif
            Initialize("TestEasy");
            // TODO: Test what value of S and LoopLimit is optimal
            Solve(2, 10000, 50);
            Console.ReadKey();
#if DEBUG
            Debug(DebugPrints.Sudoku | DebugPrints.Blocks);
            while (Console.ReadKey().Key != ConsoleKey.Escape) { }
#endif
        }
        // Program Logic
        #region Initialize
        /// <summary>
        /// Contains all initialisation logic
        /// </summary>
        /// <param name="filename"></param>
        static void Initialize(string filename = "Test")
        {
            rand = new Random();
            ImportSudoku(filename);
            Blockify();
            FillSudoku();
        }
        /// <summary>
        /// Converts a sudoku file to all the approriate datastructures
        /// </summary>
        /// <param name="filename"></param>
        static void ImportSudoku(string filename = "Test")
        {
            // Converts a string of numbers seperated by whitespace to an int[] of said numbers
            Func<string, int[]> conv = (s) => {
                var ss = s.Split();
                int[] res = new int[ss.Length];
                int parsed;
                for (int i = 0; i < ss.Length; i++)
                {
                    if (Int32.TryParse(ss[i], out parsed)) res[i] = parsed;
                    else throw new ArgumentException("ERROR: Unsuccesful parse on a sudoku file entry");
                }
                return res;
            };
            // Direct Path to Old location: ($@"E:\University\Computational_Intelligence\Sudoku_solver\{filename}.txt")
            string[] rows = System.IO.File.ReadAllLines(System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\"), $"{filename}.txt")));
            // Fill a sudoku based on rows
            sudokuRows = new int[rows.Length][];
            for (int j = 0; j < rows.Length; j++) { sudokuRows[j] = conv(rows[j]); if (sudokuRows[j].Length != rows.Length) throw new Exception("NxM sudoku; N != M"); }
            // Init fixed vals
            fixedVals = new bool[rows.Length, rows.Length];
            // Fill a sudoku based on collumns
            sudokuColumns = new int[rows.Length][];
            int[] column = new int[rows.Length];
            for (int k = 0; k < rows.Length; k++)
            {
                for (int l = 0; l < rows.Length; l++)
                {
                    // for every row take the k'th element and add to column
                    column[l] = sudokuRows[l][k];
                    // Fill fixed vals while your at it
                    if (column[l] != 0) fixedVals[k, l] = true;
                    else fixedVals[k, l] = false;
                }
                // Swap / Clear buffers
                sudokuColumns[k] = column; column = new int[rows.Length];
            }
        }
        /// <summary>
        /// Fills all the blanco numbers with random numbers, Needs Blockify to be called first
        /// </summary>
        /// <param name="type">Fill Method; either Randoml or Deterministic</param>
        static void FillSudoku([Optional, DefaultParameterValue(FillType.Deterministic)] FillType type)
        {
            List<int> number_range;
            int[] xy = new int[2];
            for (int b = 0; b < N; b++)
            {
                // range of numbers that blanco's can become
                number_range = Enumerable.Range(1, blocks.Length).ToList();
                // remove all fixed numbers from the number pool
                for (int c = 0; c < N; c++) {xy = BlockToSudokuCoord(b,c);  if (fixedVals[xy[0], xy[1]]) number_range.Remove(blocks[b][c]); }
                // loop through alll the blocks
                for (int x = 0; x < N; x++)
                {
                    // if blanco value, choose new unique and remove unique from pool
                    if (blocks[b][x] == 0)
                    {
                        if (type == FillType.Random)
                        {
                            var newx = rand.Next(0, number_range.Count());
                            blocks[b][x] = number_range[newx];
                            number_range.Remove(number_range[newx]);
                        }
                        else
                        {
                            var newx = number_range.Last();
                            blocks[b][x] = newx;
                            number_range.Remove(newx);
                        }
                    }
                }
            }
        }
        enum FillType {Random, Deterministic}
        /// <summary>
        /// Divides the sudoku into equal blocks
        /// </summary>
        /// <returns>an array of N blocks</returns>
        static void Blockify()
        {
            // Check wether the given sudoku is of a proper sudoku format
            double square;
            int x_off, y_off;
            N = sudokuRows.Length;
            if ((square = Math.Sqrt(N)) % 1 != 0) throw new ArgumentException("Sudoku nxn: Sqrt(n) is not a whole number");
            // divide the sudoku into N blocks of Sqrt(N)
            blocks = new int[N][];
            int[] block = new int[N];
            sqrN = (int)square;
            // blocks loop; i -> block index
            for (int i = 0; i < N; i++)
            {
                x_off = i % sqrN; y_off = i / sqrN;
                // block loop; j -> block value index
                for (int j = 0; j < N; j++)
                {
                    // indexer that fills blocks left to right, top to bottom
                    block[j] = sudokuRows[j / sqrN + y_off * sqrN][j % sqrN + x_off * sqrN];
                }
                blocks[i] = block;
                block = new int[N];
            }
        }
        #endregion
        #region Debug
#if DEBUG
        /// <summary>
        /// Prints the current state of Sudoku
        /// </summary>
        static void Debug(DebugPrints to_print)
        {
            // Sudoku Print
            if ((to_print & DebugPrints.Sudoku) == DebugPrints.Sudoku)
            {
                int[] row; string dashes;
                //Returns a string ---\\---//--- equal in length to the sudoku
                Console.WriteLine($@"{(dashes = new String('-', (2 * sudokuRows.Length / 3) - 2))}\\{dashes + "-" + (new String('-', (2 * sudokuRows.Length) % 3))}//{dashes}");
                for (int i = 0; i < sudokuRows.Length; i++)
                {
                    for (int j = 0; j < (row = sudokuRows[i]).Length; j++) Console.Write(row[j] + " ");
                    Console.WriteLine();
                }
                Console.WriteLine("\n");
            }
            // Blocks print
            if ((to_print & DebugPrints.Blocks) == DebugPrints.Blocks)
            {
                int block_row, row, b_x, b_y, value;
                // Aesthetic
                string dashes;
                Console.WriteLine($@"{(dashes = new String('-', (2 * sudokuRows.Length / 3) - 2))}[[{dashes + "-" + (new String('-', (2 * sudokuRows.Length) % 3))}]]{dashes}");
                // blocks loop
                for (int bi = 0; bi < N; bi++)
                {
                    block_row = bi / sqrN;
                    row = bi % sqrN;
                    // block loop
                    for (int bj = 0; bj < N; bj++)
                    {
                        // Blocks to printable format indexer
                        b_x = bj / sqrN + block_row * sqrN; b_y = bj % sqrN + row * sqrN;
                        value = blocks[b_x][b_y];
                        var xy = BlockToSudokuCoord(b_x, b_y);
                        if (fixedVals[xy[0], xy[1]]) { Console.ForegroundColor = ConsoleColor.Magenta; Console.Write(value + " "); Console.ForegroundColor = ConsoleColor.Gray; }
                        else Console.Write(value + " ");
                    }
                    Console.WriteLine("");
                }
            }
        }

        enum DebugPrints { Sudoku = 1, Blocks = 2};
#endif
        #endregion
        #region Solve logic
        /// <summary>
        /// Solves the sudoku
        /// </summary>
        /// <param name="random_s">How often to call ranndomwalk when stuck</param>
        static void Solve(int random_s, int loop_limit, int plateau_limit)
        {
            watch.Stop();
            Console.WriteLine("Initialized(ms): {0}", watch.ElapsedMilliseconds);
            watch.Reset();
            watch.Start();
            ILS(random_s, loop_limit, plateau_limit);
            watch.Stop();
            Console.WriteLine("EndScore: " + Evaluate());
            Console.WriteLine("ElapsedTime(ms): {0}", watch.Elapsed.Milliseconds);
        }


        /// <summary>
        /// Combines hill climbing with a randomwalk to solve the sudoku
        /// </summary>
        /// <param name="random_s">How often to call random walk when stuck</param>
        static void ILS(int random_s, int loop_limit, int plateau_limit)
        {
            // state values
            int score = 1; int prev_best = int.MaxValue; int loop = 0; int plateau = 0;
            // Goto Label for 
            Repeat:
            while (score != 0)
            {
                if (plateau > plateau_limit)
                {
#if DEBUG
                    if (watch.Elapsed.Milliseconds % 100 < 10) Console.WriteLine("Plateau..  score: " + score);
#endif
                    plateau = 0;
                    break;
                }
                score = HillClimbing();
                if (score == prev_best)
                {
                    plateau++;
                    prev_best = score;
                    continue;
                }
                if (score > prev_best)
                {
                    break;
                }
                else
                {
                    prev_best = score;
                }
            }
            if (score == 0) return;
            // Normally one would store the local optimum here, but for sudoku we want score = 0 and nothing else
            if (loop < loop_limit)
            {
                RandomWalk(random_s);
                prev_best = Evaluate();
                loop++;
#if DEBUG
                if (watch.Elapsed.Milliseconds % 100 < 10) Console.WriteLine("loop" + loop + "    score: " + prev_best);
#endif
                goto Repeat;
            }
            Console.WriteLine("Loop Limit reached");
        }

        /// <summary>
        /// Tries to find the optimal state => the solution
        /// </summary>
        static int HillClimbing()
        {
            //if (TERMINATION) something terminate;
            var b_index = rand.Next(9);        // * Randomly chosen Block
            int score;                          // * New score after swap
            int block_best = int.MaxValue;      // * The best score possible by changes in this block
            int[] best_swap = new int[2];       // * The corresponding swap required for block_best
            // 
            for (int v = 0; v < N; v++)
            {
                // Swap two: both in rows as collumns : Fixed vaues are < 0
                for (int k = v; k < N; k++) // only need to swap with values after you as values before already swapped with the current value
                {
                    if (Swap(v, k, b_index))
                    {
                        score = Evaluate();
                        if (score < block_best) { block_best = score; best_swap = new int[2] { v, k }; }
                        // Reset state
                        Swap(k, v, b_index);
                    }
                }
            }
            // Finish up
            Swap(best_swap[0], best_swap[1], b_index);
            return block_best;
 
        }
        /// <summary>
        /// Adds chaos to climbing to allow to jump out of local maxima
        /// </summary>
        /// <param name="s">amount of random swaps</param>
        static void RandomWalk(int s)
        {
            int valA = 0; int valB = 0; int block = 0;
            // if this gets stuck -> Try to swap outside just blocks
            for (int r = 0; r < s; r++)
            {
                block = rand.Next(0, N);
                NoSwap:
                // Pick random block and block values
                while (valA == valB)
                {
                    valA = rand.Next(0, N);
                    valB = rand.Next(0, N);
                }
                // Swap them or retry if fixed
                if (!Swap(valA, valB, block)) {valA = valB; goto NoSwap; }
            }

        }

        // Supporting functions
        /// <summary>
        /// Returns a score based on the amount on missing numbers [1 .. 9] in each row & column
        /// </summary>
        /// <returns></returns>
        static int Evaluate()
        {
            int CheckTuples(int[][] to_check)
            {
                int score = 0;
                HashSet<int> uniqueNums;
                for (int i = 0; i < to_check.Length; i++)
                {
                    // Cast to Hashset and get all unique numbers
                    //add the difference between unqiue nums and tuple length to score
                    uniqueNums = new HashSet<int>(to_check[i]);
                    score += to_check.Count() - uniqueNums.Count();
                }
                return score;
            }
            return CheckTuples(sudokuColumns) + CheckTuples(sudokuRows);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ind_a"></param>
        /// <param name="ind_b"></param>
        /// <param name="b_index"></param>
        static bool Swap(int ind_a, int ind_b, int b_index)
        {
            // Get Sudoku Coor
            var a_xy = BlockToSudokuCoord(b_index, ind_a);
            var b_xy = BlockToSudokuCoord(b_index, ind_b);
            if (!(fixedVals[a_xy[0], a_xy[1]] | fixedVals[b_xy[0], b_xy[1]]))
            {
                // Store value a and swap
                var temp = blocks[b_index][ind_a];
                blocks[b_index][ind_a] = blocks[b_index][ind_b];
                blocks[b_index][ind_b] = temp;
                // Get Sudoku Coor
                // Swap Rows: indexed by y(1) -> x(0)
                sudokuRows[a_xy[1]][a_xy[0]] = sudokuRows[b_xy[1]][b_xy[0]];
                sudokuRows[b_xy[1]][b_xy[0]] = temp;
                // Swap Columns: indexed by x(0) -> y(1)
                sudokuColumns[a_xy[0]][a_xy[1]] = sudokuColumns[b_xy[0]][b_xy[1]];
                sudokuColumns[b_xy[0]][b_xy[1]] = temp;

                return true;
            }
            return false;
        }
        /// <summary>
        /// Returns the x,y coordinates of a given block and blockvalue
        /// </summary>
        /// <param name="b_index"></param>
        /// <param name="v_index"></param>
        /// <returns>int[2] := [x,y]</returns>
        static int[] BlockToSudokuCoord(int b_index, int v_index)
        {
            // Block induced offsets
            var x_offset = (b_index % sqrN) * sqrN;
            var y_offset = (b_index / sqrN) * sqrN;
            // In-block induced offsets
            var x = v_index % sqrN;
            var y = v_index / sqrN;
            // return [x,y]
            return new int[2] { x_offset + x, y_offset + y };
        }
#endregion
    }
}
