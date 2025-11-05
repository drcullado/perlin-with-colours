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

            Normalisation(size, outputMap);
            return outputMap;
        }

        static double[,] Normalisation(int size, double[,] input)
        {
            double min = 1000000, max = -1000000;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (input[x, y] < min)
                    {
                        min = input[x, y];
                    }
                    if (input[x, y] > max)
                    {
                        max = input[x, y];
                    }
                }
            }

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    input[x, y] = (input[x, y] - min) / (max - min);
                    input[x, y] = Math.Pow(input[x, y], 2);
                }
            }

            return input;
        }


        static void Island(int size, double[,] input)
        {
            double[,] islandGradient = new double[size, size];
            double maxDistance = Math.Sqrt(Math.Pow(size, 2) + Math.Pow(size, 2));
            double minDistance = Math.Sqrt(Math.Pow(size/2, 2) + Math.Pow(size/2, 2));
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                   
                }
            }
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
            // int choice = Menu();
            
            Console.Write("Enter x size of grid: ");
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


        static int Menu()
        {
            int choice = 0;
            Console.WriteLine("Choose generative algorithm to use: ");
            Console.WriteLine();
            Console.WriteLine("1. Perlin Noise");
            Console.WriteLine("2. ");
            Console.WriteLine("9. Close program");
            Console.SetCursorPosition(36, 0);

            while (int.TryParse(Console.ReadLine(), out choice) == false)
            {
                Console.Clear();
                Console.WriteLine("Invalid option chosen");
                Console.WriteLine("Choose generative algorithm to use: ");
                Console.WriteLine();
                Console.WriteLine("1. Perlin Noise");
                Console.WriteLine("2. ");
                Console.SetCursorPosition(1, 36);   
            }
                choice = int.Parse(Console.ReadLine());
                return choice;
        }


    }
}