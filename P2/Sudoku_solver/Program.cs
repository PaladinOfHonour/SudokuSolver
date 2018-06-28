﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.IO;
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
        static FileStream fileOut;
        static int N, sqrN;
        /// <summary>
        /// Entry Point and startup logic
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            watch = Stopwatch.StartNew();
#if DEBUG
            // Sets language of error messages to English (Development enviroment's default culture is "jp")
            if (Debugger.IsAttached)
                System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
#endif
            // TODO: Test what value of S and LoopLimit is optimal
            if (args.Length == 0)
            {
                Initialize("TestEasy");
                Output("TestEasy");
                Solve();
            }
            else
            {
                var args_int = new int[args.Length - 1];
                for (int i = 0; i < args.Length - 1; i++) args_int[i] = Int32.Parse(args[i]);
                string puzzle = args[args.Length - 1];
                Initialize(puzzle);
                Output(puzzle);
                Solve();
            }
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
        static void Initialize(string filename = "Test", string resultFile = "Result")
        {
            rand = new Random();
            fileOut = new FileStream($@"../../../{resultFile}.txt", FileMode.Append);
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
            // TODO: Fix read in format -> skip first line if !number and use.. row(line) to Char assuming "0022341" etc

            // Converts a string of numbers seperated by whitespace to an int[] of said numbers
            Func<string, int[]> conv = (s) => {
                var ss = s.ToCharArray();
                int[] res = new int[ss.Length];
                for (int i = 0; i < ss.Length; i++)
                {
                    res[i] = ss[i] - '0';
                }
                return res;
            };
            // Direct Path to Old location: ($@"E:\University\Computational_Intelligence\Sudoku_solver\{filename}.txt")
            // Generalized Path to location: System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\"), $"{filename}.txt"))
            // Quick Path:
            string[] rows = System.IO.File.ReadAllLines(@"../../../" + filename + ".txt");
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
        static void Solve()
        {
            watch.Stop();
            Console.WriteLine("Initialized(ms): {0}", watch.ElapsedMilliseconds);
            watch = Stopwatch.StartNew();
            watch.Stop();
            Console.WriteLine("ElapsedTime(ms): {0}", watch.ElapsedMilliseconds);
            // Output values:
            Output(watch.ElapsedMilliseconds);
            OutNewLine();
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
        #region Research
        static void Output<T>(T result) where T : IFormattable
        {
            var to_buffer = result.ToString();
            fileOut.Write(Encoding.ASCII.GetBytes(to_buffer + ";"), 0, to_buffer.Length + 1);
        }
        static void Output(string result) {
            fileOut.Write(Encoding.ASCII.GetBytes(result + ";"), 0, result.Length + 1);
        }
        static void OutNewLine() {
            fileOut.Write(Encoding.ASCII.GetBytes("\n"), 0, 1);
        }
        #endregion
    }
}
