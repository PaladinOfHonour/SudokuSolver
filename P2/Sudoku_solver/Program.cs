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
        // Additional Globals
        static Random rand;
        static Stopwatch watch;
        static FileStream fileOut;
        static int N, sqrtN;
        delegate bool Solve_Logic(int frontier = 0);
        static Solve_Logic CSP_solve;
        static CSP alg_type;

        static int recursionCount = 0;
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
            if (args.Length == 0)
            {
                Initialize("1");
                Output("1");
                Solve();
            }
            else
            {
                var args_int = new int[args.Length - 1];
                for (int i = 0; i < args.Length - 1; i++) args_int[i] = Int32.Parse(args[i]);
                string puzzle = args[args.Length - 1];
                Initialize(puzzle, csp_type: (CSP)args_int[0]);
                Output(recursionCount);
                Solve();
            }
#if DEBUG
            Debug(alg_type);
            while (Console.ReadKey().Key != ConsoleKey.Escape) { }
#endif
        }
        // Program Logic
        #region Initialize
        /// <summary>
        /// Contains all initialisation logic
        /// </summary>
        /// <param name="filename"></param>
        static void Initialize(string filename = "Test", string resultFile = "Result", CSP csp_type = CSP.CB)
        {
            rand = new Random();
            fileOut = new FileStream($@"../../../{resultFile}.txt", FileMode.Append);
            ImportSudoku(filename);
            // Solve init may depend on ImportSudoku
            Solve_Init(csp_type);
            alg_type = csp_type;
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
                    // Set all domains to maximum
                    v_domains[i * N + j] = Enumerable.Range(1, 9).ToList();
                    realValues[i * N + j] = val - '0';
                }
            }
            // Initalize all the constraints cij as empty
            for (int c = 0; c < N; c++)
            {
                rows_c[c] = 0;
                columns_c[c] = 0;
                blocks_c[c] = 0;
            }
            // Insert all the fixed_vals, setting the domains and constraints in the process
            foreach (int f in fixed_vals)
                Insert_Fixed(f, realValues[f]);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="realSlot"></param>
        /// <param name="value"></param>
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

                // Update Domains Dji
                v_domains[row_i].Remove(value);
                v_domains[column_i].Remove(value);
                v_domains[block_i].Remove(value);
            }
            // Update Constraitns Cji
            UpdateConstraints(realSlot, value);
            v_domains[realSlot] = new List<int>() { value };
        }

        static void Solve_Init(CSP solve_logic)
        {
            switch (solve_logic)
            {
                case CSP.FC:
                    CSP_solve = FC;
                    break;
                case CSP.FC_:
                    CSP_solve = FC;
                    break;
                case CSP.CB:
                    CSP_solve = CB;
                    break;
                case CSP.CB_:
                    HeuristicSort();
                    CSP_solve = CB;
                    break;
                default:
                    break;
            }
        }
        enum CSP { FC = 1, FC_ = 2, CB = 4, CB_ = 8 };
        #endregion
        #region Debug
#if DEBUG
        /// <summary>
        /// Prints the current state of Sudoku
        /// </summary>
        static void Debug(Enum to_print)
        {
            int print_data = (int)(object)to_print;
            int test = (int)DebugPrints.BC;
            int test2 = print_data & test;
            bool test3 = test2 == test;
            // Sudoku Print
            if ((print_data & (int)DebugPrints.FC) > 0)
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

            if ((print_data & (int)DebugPrints.BC) > 0)
            {
                string dashes; string row_print = "";
                //Returns a string ---\\---//--- equal in length to the sudoku
                Console.WriteLine($@"{(dashes = new String('-', (2 * N / 3) - 2))}\\{dashes + "-" + (new String('-', (2 * N) % 3))}//{dashes}");
                for (int i = 0; i < N; i++)
                {
                    for (int j = 0; j < N; j++)
                    {
                        row_print += (realValues[i * N + j] + " ");
                    }
                    Console.WriteLine(row_print);
                    row_print = "";
                }
                Console.WriteLine("\n");
            }
        }

        enum DebugPrints { FC = CSP.FC |CSP.FC_ | Domains, BC = CSP.CB | CSP.CB_ | Reals, Domains = 64, Reals = 128};
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
            CSP_solve();
            watch.Stop();
            Console.WriteLine("ElapsedTime(ms): {0}", watch.ElapsedMilliseconds);
            // Output values:
            Output(watch.ElapsedMilliseconds);
            Output(recursionCount);
            OutNewLine();
        }
        /// <summary>
        /// 
        /// </summary>
        static void HeuristicSort() { v_p.OrderBy(x => v_domains[x].Count); }
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
            var row = realSlot / N;
            var column = realSlot % N;
            //
            var row_c = rows_c[row];
            if ((row_c & encodedValue) > 0) return false;
            // Do the same for the appropriate column constraint
            var column_c = columns_c[column];
            if ((column_c & encodedValue) > 0) return false;
            // Given the column and row index, calculate the corresponding block and check it for a violation to boot
            var block_c = blocks_c[((row / sqrtN) * sqrtN) + (column / sqrtN)];
            if ((block_c & encodedValue) > 0) return false;
            // If no constraints were violated return true
            return true;
        }
        /// <summary>
        /// Updates the constraints by XOR-ing with the value;
        /// ergo to reset previous update, repeat same call
        /// </summary>
        /// <param name="realSlot"></param>
        /// <param name="realValue"></param>
        static void UpdateConstraints(int realSlot, int realValue)
        {
            // Calculate the block index and encode the value to an ushort
            ushort encoded_val = (ushort)(1 << (realValue - 1));
            var block_id = (((realSlot / N) / sqrtN) * sqrtN) + ((realSlot % N) / sqrtN);
            // Update all three constraints
            rows_c[realSlot / N] = (ushort)(rows_c[realSlot / N] ^ encoded_val);
            columns_c[realSlot % N] = (ushort)(columns_c[realSlot % N] ^ encoded_val);
            blocks_c[block_id] = (ushort)(blocks_c[block_id] ^ encoded_val);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="frontier"></param>
        static bool FC(int frontier = 0)
        {
            // retreive the pointer to the actual Vi
            var v_pointer = v_p[frontier]; // if v_p not changde it's 1 : 1 map
            var domain = v_domains[v_pointer];
            int value;
            //
            if (domain.Count == 1) return true;
            // Try each possible value of the base domain
            for (int i = 0; i < domain.Count; i++)
            {
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
                            return false;
                        }
                    }
                    // If no Constraint Problems -> Set the value
                    v_domains[v_pointer] = new List<int>() { value };
                    // Expand the next frontier
                    FC(frontier++);
                    //
                    
                }
            }
            return false;
        }

        /// <summary>
        /// Recursive function that solves the sudoku using CB. Call with realValues and a list of 1..9 for the first time.
        /// </summary>
        static bool CB(int frontier = 0)
        {
            if (recursionCount > 100)
            {
                Debug(CSP.CB);
            }
            recursionCount++;
            //
            int pointer = v_p[frontier];
            int prev_value, new_frontier, new_pointer;
            // Try all possible values for this node
            for (int i = 1; i <= N; i++)
            {

                if (pointer == 0)
                {
                    Console.WriteLine();
                }

                // Check wether the value adheres to the constraints
                if (ConstraintCheck(pointer, i))
                {
                    UpdateConstraints(pointer, i);
                    // store the old value for back tracking
                    prev_value = realValues[pointer];
                    realValues[pointer] = i;
                    // Find the next frontier
                    new_frontier = frontier;
                    while (realValues[v_p[new_frontier]] != 0)
                    {
                        new_frontier++;
                        if (new_frontier == realValues.Length)
                        {
                            // All nodes have there values set
                            Console.WriteLine("Solved.");
                            return false;
                            // false means no backtracking
                        }
                    }
                    new_pointer = v_p[new_frontier];
                    // Enter recursion for next frontier and check wether it returns solved
                    if (CB(new_pointer))
                    {
                        realValues[pointer] = prev_value;
                        UpdateConstraints(pointer, i);
                        continue;
                        // try the next value after resetting the state
                    }
                    else
                    {
                        // returned solved
                        return false;
                    }
                }
                // Otherwise try the next value
                else continue;
            }
            // If you get here: all numbers have been tried
            return true;
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
