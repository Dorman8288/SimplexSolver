using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace Simplex
{
    internal class Program
    {
        public enum SolutionCode { Bounded, Unbounded, Infeasible}
        // ConstraintType
        // 0 = '='
        // 1 = '<'
        // 2 = '>'
        static public double[][] GetStandardLP(double[][] Constraints, int[] ConstraintType, double[] RightHandSides, double[] Objective)
        {
            long ConstraintCount = Constraints.Length;
            long VariableCount = Objective.Length + ConstraintType.Where(x => x == 1 || x == 2).Count();

            double[][] Tableaus = new double[ConstraintCount + 1][];
            for (int i = 0; i < ConstraintCount + 1; i++)
                Tableaus[i] = new double[VariableCount + 1];

            //row 0 
            Tableaus[0][0] = 1;
            for(int i = 1; i < Objective.Length; i++)
                Tableaus[0][i] = -Objective[i];
            Tableaus[0][VariableCount] = Objective[0];
            //Constraints
            int AdditionalVariableIndex = Constraints[0].Length + 1;
            for(int i = 0; i < ConstraintCount; i++)
            {
                for(int j = 0; j < Constraints[i].Length; j++)
                    Tableaus[i + 1][j + 1] = Constraints[i][j];
                //SlackVariable
                if (ConstraintType[i] == 1)
                {
                    Tableaus[i + 1][AdditionalVariableIndex] = 1;
                    AdditionalVariableIndex++;
                }
                if (ConstraintType[i] == 2)
                {
                    Tableaus[i + 1][AdditionalVariableIndex] = -1;
                    AdditionalVariableIndex++;
                }
                //RHS
                Tableaus[i + 1][VariableCount] = RightHandSides[i];
            }
            return Tableaus;
        }
        public static bool LPisOptimal(double[][] LP)
        {
            for (int i = 1; i < LP[0].Length - 1; i++)
                if (LP[0][i] < 0)
                    return false;
            return true;
        }
        public static int GetEnteringVariable(double[][] LP)
        {
            for(int i = 1; i < LP[0].Length - 1; i++)
                if (LP[0][i] < 0)
                    return i;
            return -1;
        }
        public static int PreformRatioTest(double[][] LP, int SelectedVariable)
        {
            int RHS = LP[0].Length - 1;
            int Winner = -1;
            double WinnerRatio = double.MaxValue;
            for(int i = 1; i < LP.Length; i++)
            {
                if (LP[i][SelectedVariable] > 0)
                {
                    double Ratio = LP[i][RHS] / LP[i][SelectedVariable];
                    if(Ratio < WinnerRatio)
                    {
                        WinnerRatio = Ratio;
                        Winner = i;
                    }
                }
            }
            return Winner;
        }
        public static void MakeIntoBasis(double[][] LP, int Variable, int row)
        {
            double coefficient = LP[row][Variable];
            for(int i = 0; i < LP[row].Length; i++)
                LP[row][i] /= coefficient;
            for(int i = 0; i < LP.Length; i++)
            {
                if(i != row)
                {
                    double FalseCoefficient = LP[i][Variable];
                    for(int j = 0; j < LP[i].Length; j++)
                        LP[i][j] -= FalseCoefficient * LP[row][j];
                }
            }
        }
        public static Tuple<SolutionCode, double[]> MaximizeZ(double[][] LP)
        {
            while (true)
            {
                if (LPisOptimal(LP))
                    return new Tuple<SolutionCode, double[]>(SolutionCode.Bounded, ExtractSolution(LP));
                var EnteringVariable = GetEnteringVariable(LP);
                var RatioTestWinner = PreformRatioTest(LP, EnteringVariable);
                if (RatioTestWinner == -1)
                    return new Tuple<SolutionCode, double[]>(SolutionCode.Unbounded, null);
                MakeIntoBasis(LP, EnteringVariable, RatioTestWinner);
            }
        }
        public static double[] ExtractSolution(double[][] LP)
        {
            int VariableCount = LP[0].Length - 1;
            double[] Solution = new double[VariableCount];
            for (int i = 0; i < VariableCount; i++)
            {
                int ColumnVariable = -1;
                for (int j = 0; j < LP.Length; j++)
                {
                    if (LP[j][i] != 0 && LP[j][i] != 1)
                    {
                        ColumnVariable = -1;
                        break;
                    }
                    if (LP[j][i] == 1)
                        if (ColumnVariable == -1)
                            ColumnVariable = j;
                        else
                        {
                            ColumnVariable = -1;
                            break;
                        }
                }
                if (ColumnVariable != -1)
                    Solution[i] = LP[ColumnVariable][VariableCount];
            }
            return Solution;
        }
        public static Tuple<SolutionCode, double[]> MinimizeZ(double[][] LP)
        {
            for (int i = 1; i < LP[0].Length; i++)
                LP[0][i] *= -1;
            var SolutionForMax = MaximizeZ(LP);
            if (SolutionForMax.Item1 == SolutionCode.Bounded)
            {
                double[] SolutionForMin = SolutionForMax.Item2;
                SolutionForMin[0] *= -1;
                return new Tuple<SolutionCode, double[]>(SolutionCode.Bounded, SolutionForMin);
            }
            return SolutionForMax;
        }
        public static double[][] GetBigMTransform(double[][] Constraints, int[] ConstraintType, double[] RightHandSides, double[] Objective)
        {
            int ConstraintCount = Constraints.Length;
            int VariableCount = Constraints[0].Length;
            double BigM = Math.Pow(10, 10);
            List<int> AdditionalVariableLocations = new List<int>();
            for(int i = 0; i < ConstraintCount; i++)
            {
                if (RightHandSides[i] < 0)
                {
                    RightHandSides[i] *= -1;
                    if (ConstraintType[i] == 1)
                        ConstraintType[i] = 2;
                    else if (ConstraintType[i] == 2)
                        ConstraintType[i] = 1;
                    for(int j = 0; j < VariableCount; j++)
                        Constraints[i][j] *= -1;
                }
                if (ConstraintType[i] == 0 || ConstraintType[i] == 2)
                    AdditionalVariableLocations.Add(i);
            }
            var StandardForm = GetStandardLP(Constraints, ConstraintType, RightHandSides, Objective);
            double[][] BigMForm = new double[ConstraintCount + 1][];
            int BigMVariableCount = StandardForm[0].Length + AdditionalVariableLocations.Count;
            for (int i = 0; i < ConstraintCount + 1; i++)
                BigMForm[i] = new double[BigMVariableCount];
            //row 0 
            for (int i = 0; i < StandardForm[0].Length - 1; i++)
                BigMForm[0][i] = StandardForm[0][i];
            for (int i = 0; i < AdditionalVariableLocations.Count; i++)
                BigMForm[0][StandardForm[0].Length + i - 1] = BigM;
            BigMForm[0][BigMVariableCount - 1] = StandardForm[0][StandardForm[0].Length - 1];
            //Constraints
            for(int i = 1; i < StandardForm.Length; i++)
            {
                for(int j = 0; j < StandardForm[i].Length - 1; j++)
                    BigMForm[i][j] = StandardForm[i][j];
                BigMForm[i][BigMVariableCount - 1] = StandardForm[i][StandardForm[i].Length - 1];
            }
            for(int i = 0; i < AdditionalVariableLocations.Count; i++)
            {
                var row = AdditionalVariableLocations[i] + 1;
                BigMForm[row][StandardForm[0].Length - 1 + i] = 1;
                for(int j = 0; j < BigMVariableCount; j++)
                    BigMForm[0][j] -= BigM * BigMForm[row][j];
            }
            return BigMForm;
        }
        public static void DisplayMatrix(double[][] M)
        {
            for(int i = 0; i < M.Length; i++)
            {
                for(int j = 0;j < M[i].Length; j++)
                    Console.Write(M[i][j] + " ");
                Console.WriteLine();
            }
        }
        public static Tuple<SolutionCode, double[]> RunSimplex(double[][] Constraints, int[] ConstraintType, double[] RightHandSides, double[] Objective, bool Maximize)
        {
            if (!Maximize)
                for (int i = 0; i < Objective.Length; i++)
                    Objective[i] *= -1;
            var LP = GetBigMTransform(Constraints, ConstraintType, RightHandSides, Objective);
            var solution = MaximizeZ(LP);
            var Assignments = solution.Item2;
            if (Assignments == null)
                return solution;
            if (!Maximize)
                Assignments[0] *= -1;
            int SlackVariableCount = ConstraintType.Where(x => x == 1 || x == 2).Count();
            for (int i = 1 + Constraints[0].Length + SlackVariableCount; i < Assignments.Length; i++)
                if (Math.Round(Assignments[i], 5) != 0)
                    return new Tuple<SolutionCode, double[]>(SolutionCode.Infeasible, null);
            return new Tuple<SolutionCode, double[]>(solution.Item1, Assignments.ToList().GetRange(0, 1 + Constraints[0].Length).ToArray());
        }
        static void Main(string[] args)
        {
            var buffer = Console.ReadLine().Split();
            long ConstraintSize = long.Parse(buffer[0]);
            long VariableSize = long.Parse(buffer[1]);
            double[][] Constraints = new double[ConstraintSize][];
            for(int i = 0; i < ConstraintSize; i++)
                Constraints[i] = new double[VariableSize];
            for(int i = 0; i < ConstraintSize; i++)
                Constraints[i] = Console.ReadLine().Split().Select(x => double.Parse(x)).ToArray();
            double[] RHS = Console.ReadLine().Split().Select(x => double.Parse(x)).ToArray();
            int[] Types = Enumerable.Range(0, (int)ConstraintSize).Select(x => 1).ToArray();
            double[] Objective = new double[VariableSize + 1];
            buffer = Console.ReadLine().Split();
            Objective[0] = 0;
            for(int i = 1; i <= VariableSize; i++)
                Objective[i] = double.Parse(buffer[i - 1]);
            var Solution = RunSimplex(Constraints, Types, RHS, Objective, true);
            if (Solution.Item1 == SolutionCode.Bounded)
            {
                Console.WriteLine("Bounded solution");
                var Assignments = Solution.Item2;
                for (int i = 1; i < Assignments.Length; i++)
                    Console.Write(Assignments[i] + " ");
            }
            else if (Solution.Item1 == SolutionCode.Unbounded)
                Console.WriteLine("Infinity");
            else
                Console.WriteLine("No solution");
        }
    }
}