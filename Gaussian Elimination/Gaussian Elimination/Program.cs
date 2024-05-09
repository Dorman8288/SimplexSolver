using System.Linq;
using System;
using System.Collections.Generic;
namespace Gaussian_Elimination
{
    internal class Program
    {
        public static void ERO1(double[][] Matrix, double Multiplier, int row)
        {
            for(int i  = 0; i < Matrix[row].Length; i++)
                Matrix[row][i] *= Multiplier;
        }
        public static void ERO2(double[][] Matrix, double Multiplier, int source, int target)
        {
            for (int i = 0; i < Matrix[target].Length; i++)
                Matrix[target][i] += Matrix[source][i] * Multiplier;
        }
        public static void ERO3(double[][] Matrix, int a, int b)
        {
            var temp = Matrix[a];
            Matrix[a] = Matrix[b];
            Matrix[b] = temp;
        }
        public static int GetNonZeroEquality(double[][] Matrix, int column)
        {
            for(int i = 0; i < Matrix.Length; i++)
                if (Matrix[i][column] != 0)
                    return i;
            return -1;
        }
        public static void SolveLinearSystem(double[][] system)
        {
            int n = system.Length;
            int currentRow = 0;
            int currentColumn = 0;
            while (currentRow != n || currentColumn != n)
            {
                double currentEntry = system[currentRow][currentColumn];
                if(Math.Round(currentEntry, 5) == 0)
                {
                    var NonzeroRow = GetNonZeroEquality(system, currentColumn);
                    if (NonzeroRow != -1)
                        ERO3(system, NonzeroRow, currentRow);
                    else
                        currentColumn++;
                    continue;
                }
                ERO1(system, 1/currentEntry, currentRow);
                for (int i = 0; i < n; i++)
                    if (i != currentRow)
                        ERO2(system, -system[i][currentColumn], currentRow, i);
                currentRow++;
                currentColumn++;
            }
        }

        static void Main(string[] args)
        {
            int n = int.Parse(Console.ReadLine());
            double[][] system = new double[n][];
            for(int i = 0; i < n; i++)
                system[i] = Console.ReadLine().Split().Select(x => double.Parse(x)).ToArray();
            SolveLinearSystem(system);
            for (int i = 0; i < n; i++)
                Console.Write(system[i][n] + " ");
        }
    }
}