using System;
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
        static ushort[] rows_c, columns_c, blocks_c;
        static List<int>[] v_domains;
        static int[] v_p, realValues;
        static int[] langs;
        // Additional Globals
        static Random rand;
        static Stopwatch watch;
        static FileStream fileOut;
        static int N, sqrtN;
        delegate void Insert_Logic(int v_pointer);
        static Insert_Logic Insert;
        /// <summary>
        /// Entry Point and startup logic
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            watch = Stopwatch.StartNew();
#if DEBUG
            // Sets language of error messages to English (Development environment's default culture is "jp")
            if (Debugger.IsAttached)
                System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
#endif
            // TODO: Test what value of S and LoopLimit is optimal
            if (args.Length == 0)
            {
                Initialize("Test");
                Output("Test");
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
            Debug(DebugPrints.Sudoku);
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
            Insert_Init(CSP.FC);
            ImportSudoku(filename);
        }
        /// <summary>
        /// Converts a sudoku file to all the approriate datastructures
        /// </summary>
        /// <param name="filename"></param>
        static void ImportSudoku(string filename = "Test")
        {
            // Direct Path to Old location: ($@"E:\University\Computational_Intelligence\Sudoku_solver\{filename}.txt")
            // Generalized Path to location: System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\"), $"{filename}.txt"))
            // Quick Path:
            string[] rows = System.IO.File.ReadAllLines($@"../../../{filename}.txt");
            // Fill a sudoku based on rows
            N = rows.Length;
            sqrtN = (int)Math.Sqrt(N);
            rows_c = new ushort[N];
            columns_c = new ushort[N];
            blocks_c = new ushort[N];
            v_domains = new List<int>[N * N];
            realValues = new int[N * N];
            v_p = Enumerable.Range(0, N * N).ToArray();
            langs = new int[N * N];

            // Initalize all the domains and locate the fixed vals
            List<int> fixed_vals = new List<int>();
            char[] ss;
            char val;
            for (int i = 0; i < rows.Length; i++)
            {
                ss = rows[i].ToCharArray();
                for (int j = 0; j < N; j++)
                {
                    val = ss[j];
                    if (val != '0')
                        fixed_vals.Add(i * N + j);
                    v_domains[i * N + j] = Enumerable.Range(1, 9).ToList();
                    realValues[i * N + j] = val - '0';
                    langs[i * N + j] = 0;
                }
            }
            // Initalize all the constraints cij
            for (int c = 0; c < N; c++)
            {
                rows_c[c] = 0;
                columns_c[c] = 0;
                blocks_c[c] = 0;
                // row / i 
            }
            // Insert all the fixed_vals
            foreach (int f in fixed_vals)
                Insert_Fixed(f, realValues[f]);
        }

        static void HeuristicSort() { v_p.OrderBy(x => v_domains[x].Count); }

        static void Insert_Fixed(int realSlot, int value)
        {
            // Get the starts of both the column and row ur Vi is a part of
            var row_start = realSlot - (realSlot % N);
            var column_start = realSlot % N;
            var block_id = (((realSlot / N) / sqrtN) * sqrtN) + ((realSlot % N) / sqrtN);
            var block_start = (block_id / sqrtN) * (N * sqrtN) + (block_id % sqrtN) * sqrtN;
            int row_i, column_i, block_i;
            // Update every other Dj to adhere to the newly assigned value
            for (int j = 0; j < N; j++)
            {
                row_i = row_start + j;
                column_i = column_start + (j * N);
                block_i = block_start + ((j / sqrtN) * N) + j % sqrtN;

                // Update constraints Cji
                v_domains[row_i].Remove(value);
                v_domains[column_i].Remove(value);
                v_domains[block_i].Remove(value);
            }
            v_domains[realSlot] = new List<int>() { value };
        }

        static void Insert_Init(CSP solve_logic)
        {
            switch (solve_logic)
            {
                case CSP.FC:
                    Insert = FC;
                    break;
                case CSP.FC_:

                    break;
                case CSP.CB:

                    break;
                case CSP.CB_:

                    break;
                default:
                    break;
            }
        }
        enum CSP { FC, FC_, CB, CB_ };
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
                string dashes; string row_print = ""; int pointer;
                //Returns a string ---\\---//--- equal in length to the sudoku
                Console.WriteLine($@"{(dashes = new String('-', (2 * N / 3) - 2))}\\{dashes + "-" + (new String('-', (2 * N) % 3))}//{dashes}");
                for (int i = 0; i < N; i++)
                {
                    for (int j = 0; j < N; j++)
                    {
                        
                        pointer = i * N + j;
                        if (v_domains[pointer].Count() > 1)
                            row_print += "0 ";
                        else
                            row_print += $"{v_domains[pointer][0]} ";
                    }
                    Console.WriteLine(row_print);
                    row_print = "";
                }
                Console.WriteLine("\n");
            }
        }

        enum DebugPrints { Sudoku = 1, Domains = 2, Constraints = 4};
#endif
        #endregion
        #region Solve logic
        /// <summary>
        /// Solves the sudoku
        /// </summary>
        static void Solve()
        {
            watch.Stop();
            Console.WriteLine("Initialized(ms): {0}", watch.ElapsedMilliseconds);
            watch = Stopwatch.StartNew();
            FC();
            CB_solve(new ushort[2] { 1, 2 }, new List<ushort>(), 0);
            watch.Stop();
            Console.WriteLine("ElapsedTime(ms): {0}", watch.ElapsedMilliseconds);
            // Output values:
            Output(watch.ElapsedMilliseconds);
            OutNewLine();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="realSlot">The index i coresponding to Vi</param>
        /// <param name="realValue">The value from the Domain to be checked (unencoded)</param>
        /// <returns></returns>
        static bool ConstraintCheck(int realSlot, int realValue)
        {
            // Encode Value from domain to Binary Check-form
            ushort encodedValue = (ushort)(1 << (realValue - 1));
            // Find the row constraint to check and check wether value already exists
            var row = rows_c[realSlot / N];
            if ((row & encodedValue) > 0) return false;
            // Do the same for the appropriate column constraint
            var column = columns_c[realSlot % N];
            if ((column & encodedValue) > 0) return false;
            // Given the column and row index, calculate the corresponding block and check it for a violation to boot
            var block = blocks_c[((row / sqrtN) * sqrtN) + (column / sqrtN)];
            if ((block & encodedValue) > 0) return false;
            // If no constraints were violated return true
            return true;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="frontier"></param>
        static void FC(int frontier = 0)
        {
            // retreive the pointer to the actual Vi
            var v_pointer = v_p[frontier]; // if v_p not changde it's 1 : 1 map
            var domain = v_domains[v_pointer];
            int value;
            //
            if (domain.Count == 1) return;
            // Try each possible value of the base domain
            for (int i = 0; i < domain.Count; i++)
            {
                langs[v_pointer]++;
                value = domain[i];
                // Check wether the value assignment is valid
                if (ConstraintCheck(v_pointer, value))
                {
                    // Get the starts of both the column and row ur Vi is a part of
                    var row_start = v_pointer - (v_pointer % N);
                    var column_start = v_pointer % N;
                    var block_id = (((v_pointer / N) / sqrtN) * sqrtN) + ((v_pointer % N) / sqrtN);
                    var block_start = (block_id / sqrtN) * (N * sqrtN) + (block_id - (block_id % sqrtN));
                    int row_i, column_i, block_i;
                    // Update every other Dj to adhere to the newly assigned value
                    for (int j = 0; j < N; j++)
                    {
                        row_i = row_start + j;
                        column_i = column_start + (j * N);
                        block_i = block_start + ((j / sqrtN) * N) + j;
                        // Update constraints Cji
                        v_domains[row_i].Remove(value);
                        v_domains[column_i].Remove(value);
                        v_domains[block_i].Remove(value); // TODO: Set these values back if it backtrack
                        // Check wether any Domains are now empty
                        if (v_domains[row_i].Count == 0 || v_domains[column_i].Count == 0 || v_domains[block_i].Count == 0)
                        {
                            v_domains[row_i].Add(value);
                            v_domains[column_i].Add(value);
                            v_domains[block_i].Add(value);
                            return;
                        }
                    }
                    // If no Constraint Problems -> Set the value
                    v_domains[v_pointer] = new List<int>() { value };
                    // Expand the next frontier
                    FC(frontier++);
                    //
                    
                }
            }
        }

        /// <summary>
        /// Recursive function that solves the sudoku using CB. Call with realValues and a list of 1..9 for the first time.
        /// </summary>
        static void CB_solve(ushort[] listOfValues, List<ushort> numbersToTry, int slotIndex = 0) {
            if (numbersToTry.Count == 0) {
                Console.WriteLine("Out of numbers. Backtracking.");
                return;
            }

            while (listOfValues[slotIndex] != 0) {
                slotIndex++;
                if (slotIndex == listOfValues.Length) {
                    Console.WriteLine("Solved.");
                    return;
                }
            }

            ushort num = numbersToTry[0];
            if (ConstraintCheck(slotIndex, num)) {
                listOfValues[slotIndex] = num;
                numbersToTry.RemoveAt(0);
                CB_solve(listOfValues, new List<ushort>() { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, slotIndex + 1);
            }
            else {
                numbersToTry.RemoveAt(0);
                CB_solve(listOfValues, numbersToTry, slotIndex);
            }
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
