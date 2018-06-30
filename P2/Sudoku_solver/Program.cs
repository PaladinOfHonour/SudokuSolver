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
        static ushort[] v_domains;
        static int[] v_p;
        // Additional Globals
        static Random rand;
        static Stopwatch watch;
        static FileStream fileOut;
        static int N, sqrtN;
        delegate void Insert_Logic(int v_pointer, ushort domain);
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
            ushort domain = (ushort)(Math.Pow(2, N) - 1);
            sqrtN = (int)Math.Sqrt(N);
            rows_c = new ushort[N];
            columns_c = new ushort[N];
            blocks_c = new ushort[N];
            v_domains = new ushort[N * N];

            // Initalize all the domains and locate the fixed vals
            List<Tuple<int, int>> fixed_vals = new List<Tuple<int,int>>();
            char[] ss;
            char val;
            for (int i = 0; i < rows.Length; i++)
            {
                ss = rows[i].ToCharArray();
                for (int j = 0; j < N; j++)
                {
                    val = ss[j];
                    if (val != '0')
                        fixed_vals.Add(new Tuple<int, int>(i * N + j, val - '0'));
                    v_domains[i * N + j] = domain;
                }
            }
            // Initalize all the constraints cij
            for (int c = 0; c < N; c++)
            {
                rows_c[c] = 0;
                columns_c[c] = 0;
                blocks_c[c] = 0;
            }
            // Insert all the fixed_vals
            foreach (Tuple<int,int> f in fixed_vals)
                Insert(f.Item1, (ushort)f.Item2);
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
                string dashes; ushort domain_value; char[] row_print = new char[2 * N - 1]; int pointer;
                // Fill in the spaces
                for (int c = 1; c < N; c += 2)
                    row_print[c] = ' ';
                //Returns a string ---\\---//--- equal in length to the sudoku
                Console.WriteLine($@"{(dashes = new String('-', (2 * N / 3) - 2))}\\{dashes + "-" + (new String('-', (2 * N) % 3))}//{dashes}");
                for (int i = 0; i < N * N; i++)
                {
                    for (int j = 0; j < N; j++)
                    {
                        pointer = i * N + j;
                        domain_value = v_domains[pointer];
                        // Updated to more efficient method -> https://stackoverflow.com/questions/3160659/innovative-way-for-checking-if-number-has-only-one-on-bit-in-signed-int
                        // As the Domain !contain 0, no need to check for edge case
                        if ((domain_value & domain_value - 1) == 0) row_print[pointer] = (char)domain_value;
                        else row_print[pointer] = '0';
                    }
                    Console.WriteLine();
                }
                Console.WriteLine("\n");
            }
            /*
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
            */
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
            // SOLVE LOGIC
            watch.Stop();
            Console.WriteLine("ElapsedTime(ms): {0}", watch.ElapsedMilliseconds);
            // Output values:
            Output(watch.ElapsedMilliseconds);
            OutNewLine();
        }

        static bool ConstraintCheck(ushort[] listOfValues, int slotIndex, ushort insertValue) {
            var row = rows_c[slotIndex / N];
            if ((row | insertValue) > 0) return false;
            var column = columns_c[slotIndex % N];
            if ((column | insertValue) > 0) return false;
            // BLOCK CHECKING
            return true;
        }
        
        static void FC(int frontier, ushort domain)
        {
            ushort value = (ushort)(1 << (frontier - 1));
            var pointer = v_p[frontier];
            var row = rows_c[pointer / sqrtN];
            if ((row | value) > 0)
                throw new Exception("CONSTRAINT");
            var column = columns_c[pointer % sqrtN];
            if ((column | value) > 0)
                throw new Exception("CONSTRAINT");

            v_domains[pointer] = value;
            // vp_++;
        }

        /// <summary>
        /// Recursive function that solves the sudoku using CB. Call with v_domains and a list of 1..9 for the first time.
        /// </summary>
        static void CB_solve(ushort[] listOfValues, List<ushort> numbersToTry, int slotIndex = 0) {
            while (listOfValues[slotIndex] != 0) {
                slotIndex++;
                if (slotIndex == listOfValues.Length) {
                    Console.WriteLine("Solved.");
                    return;
                }
            }

            ushort num = numbersToTry[0];
            if (ConstraintCheck(listOfValues, slotIndex, num)) {
                listOfValues[slotIndex] = num;
                CB_solve(listOfValues, new List<ushort>() { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, slotIndex + 1);
            }
            else {
                numbersToTry.RemoveAt(0);
                CB_solve(listOfValues, numbersToTry, slotIndex);
            }
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
                // Swap Rows: indexed  by y(1) -> x(0)
                sudokuRows[a_xy[1]][a_xy[0]] = sudokuRows[b_xy[1]][b_xy[0]];
                sudokuRows[b_xy[1]][b_xy[0]] = temp;
                // Swap Columns: indexed by x(0) -> y(1)
                sudokuColumns[a_xy[0]][a_xy[1]] = sudokuColumns[b_xy[0]][b_xy[1]];
                sudokuColumns[b_xy[0]][b_xy[1]] = temp;

                return true;
            }
            return false;
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
