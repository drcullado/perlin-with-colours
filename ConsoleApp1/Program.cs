using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace perlin_6
{
    internal class Program
    {
        public static void SetUpConsole()
        {
            IntPtr StandardHandle = GetStdHandle(-11);
            IntPtr handle = GetConsoleWindow();
            GetConsoleMode(StandardHandle, out uint mode);
            SetConsoleMode(StandardHandle, mode | 0x0004);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
        [DllImport("kernel32.dll", SetLastError = true)]

        private static extern IntPtr GetStdHandle(int nStdHandle);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();




        static double Fade(double t)
        {
            return t * t * t * (t * (t * 6 - 15) + 10);
        }


        static double Lerp(double x, double y, double z)
        {
            return x + z * (y - x);
        }


        static double Perlin(double x, double y, double[,] cornerVectors, int divisionSize)
        {
            int x0 = (int)(Math.Floor(x / divisionSize));
            int y0 = (int)(Math.Floor(y / divisionSize));
            int x1 = x0 + 1, y1 = y0 + 1;
            double dx = (x / divisionSize) - x0, dy = (y / divisionSize) - y0;

            double dotProd00 = Math.Cos(cornerVectors[x0, y0]) * dx + Math.Sin(cornerVectors[x0, y0]) * dy;
            double dotProd10 = Math.Cos(cornerVectors[x1, y0]) * (dx - 1) + Math.Sin(cornerVectors[x1, y0]) * dy;
            double dotProd01 = Math.Cos(cornerVectors[x0, y1]) * dx + Math.Sin(cornerVectors[x0, y1]) * (dy - 1);
            double dotProd11 = Math.Cos(cornerVectors[x1, y1]) * (dx - 1) + Math.Sin(cornerVectors[x1, y1]) * (dy - 1);

            double u = Fade(dx), v = Fade(dy);
            double val = Lerp(Lerp(dotProd00, dotProd10, u), Lerp(dotProd01, dotProd11, u), v);
            return (val + 1) * 0.5;
        }


        static double[,] GenerateGradients(int size, Random rnd)
        {
            double[,] outputGrid = new double[size,size];
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    outputGrid[x, y] = rnd.NextDouble() * Math.PI * 2;
                }
            }
            return outputGrid;
        }


        static void Octave(double[,] elevation, double[,] outputMap, double wavelength, int size)
        {
            for (int y = 0; y < size - 1; y++)
            {
                for (int x = 0; x < size - 1; x++)
                {
                    elevation[x, y] = outputMap[x, y] * wavelength;
                }
            }
        }


        static double[,] GeneratePerlinNoise(int size, int baseDivisionSize, int octaves, double persistence, double lacunarity)
        {
            double[,] outputMap = new double[size, size];
            double maxPossible = 0;
            double amplitude = 1;
            double frequency = 1;
            Random rnd = new Random();

            for (int i = 0; i < octaves; i++)
            {
                int newDivisionSize = (int)(baseDivisionSize / frequency);
                double[,] gradients = GenerateGradients((size / newDivisionSize) + 2, rnd);

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        outputMap[x, y] += Perlin(x, y, gradients, newDivisionSize) * amplitude;
                    }
                }
                maxPossible += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            double min = 1000000, max = -1000000;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (outputMap[x, y] < min)
                    {
                        min = outputMap[x, y];
                    }
                    if (outputMap[x, y] > max)
                    {
                        max = outputMap[x, y];
                    }
                }
            }

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    outputMap[x, y] = (outputMap[x, y] - min) / (max - min);
                    outputMap[x, y] = Math.Pow(outputMap[x, y], 2);
                }
            }
            return outputMap;
        }


        static void Print(int size, double[,] elevationMap, double[,] moistureMap)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int val = (int)((elevationMap[x, y] * 255));

                    if (elevationMap[x,y] < 0.15)
                    {
                        Console.Write($"\u001b[48;2;{68};{68};{122}m  ");
                    }
                    else if (elevationMap[x, y] < 0.2)
                    {
                        Console.Write($"\u001b[48;2;{160};{144};{119}m  ");
                    }


                    else if (elevationMap[x, y] < 0.3)
                    {
                        if (moistureMap[x,y] < 0.16)
                        {
                            Console.Write($"\u001b[48;2;{210};{185};{139}m  ");
                        }
                        else if (moistureMap[x,y] < 0.5)
                        {
                            Console.Write($"\u001b[48;2;{136};{170};{85}m  ");
                        }
                        else if (moistureMap[x, y] < 0.83)
                        {
                            Console.Write($"\u001b[48;2;{85};{153};{68}m  ");
                        }
                        else
                        {
                            Console.Write($"\u001b[48;2;{150};{40};{33}m  ");
                        }
                    }
                    else if (elevationMap[x, y] < 0.6)
                    {
                        if (moistureMap[x, y] < 0.16)
                        {
                            Console.Write($"\u001b[48;2;{210};{210};{111559}m  ");
                        }
                        else if (moistureMap[x, y] < 0.5)
                        {
                            Console.Write($"\u001b[48;2;{136};{170};{85}m  ");
                        }
                        else if (moistureMap[x, y] < 0.84)
                        {
                            Console.Write($"\u001b[48;2;{103};{148};{89}m  ");
                        }
                        else
                        {
                            Console.Write($"\u001b[48;2;{68};{136};{85}m  ");
                        }
                    }
                    else if (elevationMap[x, y] < 0.8)
                    {
                        if (moistureMap[x, y] < 0.33)
                        {
                            Console.Write($"\u001b[48;2;{201};{210};{155}m  ");
                        }
                        else if (moistureMap[x, y] < 0.66)
                        {
                            Console.Write($"\u001b[48;2;{136};{153};{119}m  ");
                        }
                        else
                        {
                            Console.Write($"\u001b[48;2;{153};{170};{119}m  ");
                        }
                    }
                    else 
                    {
                        if (moistureMap[x, y] < 0.1)
                        {
                            Console.Write($"\u001b[48;2;{85};{85};{85}m  ");
                        }
                        else if (moistureMap[x, y] < 0.2)
                        {
                            Console.Write($"\u001b[48;2;{136};{136};{136}m  ");
                        }
                        else if (moistureMap[x, y] < 0.5)
                        {
                            Console.Write($"\u001b[48;2;{187};{187};{170}m  ");
                        }
                        else
                        {
                            Console.Write($"\u001b[48;2;{221};{221};{228}m  ");
                        }
                    }
                }
                Console.WriteLine("\u001b[0m");
            }

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int val = (int)((elevationMap[x, y] * 255));
                    Console.Write($"\u001b[48;2;{val};{val};{val}m  ");
                }
                Console.WriteLine("\u001b[0m");
            }
        }

        static void Main(string[] args)
        {
            SetUpConsole();

            Console.Write("Enter grid size: ");
            int size = int.Parse(Console.ReadLine());
            Console.Write("Enter division size (pixels per cell): ");
            int divisionSize = int.Parse(Console.ReadLine());

            int cornerVectorSize = (int)(((double)size + divisionSize - 1) / divisionSize) + 1;
            Random rnd = new Random();
            double[,] cornerVectors = GenerateGradients(cornerVectorSize, rnd);

            int octaves = 5;
            double persistence = 0.5;
            double lacunarity = 2.0;
            int baseDivisionSize = 32;

            double[,] moisture = GeneratePerlinNoise(size, baseDivisionSize, octaves, persistence, lacunarity);
            double[,] elevation = GeneratePerlinNoise(size, baseDivisionSize, octaves, persistence, lacunarity);
            Print(size, elevation, moisture);

            Console.WriteLine("Done");
            Console.ReadKey();
        }
    }
}