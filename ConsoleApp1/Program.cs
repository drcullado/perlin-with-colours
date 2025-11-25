using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
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


        static double[,] GenerateGradients(int sizeX, int sizeY, Random rnd)
        {
            double[,] outputGrid = new double[sizeX, sizeY];
            for (int y = 0; y < sizeY; y++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    outputGrid[x, y] = rnd.NextDouble() * Math.PI * 2;
                }
            }
            return outputGrid;
        }


        static double[,] GeneratePerlinNoise(int sizeX, int sizeY, int baseDivisionSize, int octaves, double persistence, double lacunarity)
        {
            double[,] outputMap = new double[sizeX, sizeY];
            double maxPossible = 0;
            double amplitude = 1;
            double frequency = 1;
            Random rnd = new Random();

            for (int i = 0; i < octaves; i++)
            {
                int newDivisionSize = (int)(baseDivisionSize / frequency);
                double[,] gradients = GenerateGradients((sizeX / newDivisionSize) + 2, (sizeX / newDivisionSize) + 2, rnd);

                for (int y = 0; y < sizeY; y++)
                {
                    for (int x = 0; x < sizeX; x++)
                    {
                        outputMap[x, y] += Perlin(x, y, gradients, newDivisionSize) * amplitude;
                    }
                }
                maxPossible += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            Normalisation(sizeX, sizeY, outputMap);
            return outputMap;
        }

        static double[,] Normalisation(int sizeX, int sizeY, double[,] input)
        {
            double min = 1000000, max = -1000000;

            for (int y = 0; y < sizeY; y++)
            {
                for (int x = 0; x < sizeX; x++)
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

            for (int y = 0; y < sizeY; y++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    input[x, y] = (input[x, y] - min) / (max - min);
                    input[x, y] = Math.Pow(input[x, y], 2);
                }
            }

            return input;
        }


        static double[,] Island(int sizeX, int sizeY, int percentBlnd, double[,] input)
        {
            double[,] islandGradient = new double[sizeX, sizeY];
            double middleX = sizeX / 2;
            double middleY = sizeY / 2;
            double maxDistance = Math.Sqrt(Math.Pow(middleX, 2) + Math.Pow(middleY, 2));
            double percentBlend = (double)percentBlnd / 100;

            for (int y = 0; y < sizeY; y++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    double distX = (x - middleX) / middleX;
                    double distY = (y - middleY) / middleY;
                    islandGradient[x, y] = (1 - Math.Pow(distX, 2)) * (1 - Math.Pow(distY, 2));

                    input[x, y] += (islandGradient[x, y] - input[x, y]) * percentBlend;
                    if (input[x,y] < 0)
                    {
                        input[x, y] = 0;
                    }
                    else if (input[x, y] > 1)
                    {
                        input[x, y] = 1;
                    }
                }
            }

            // Print2(size, islandGradient);
            return input;
        }



        static void Print(int sizeX, int sizeY, double[,] elevationMap, double[,] moistureMap, double waterThreshold)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    int val = (int)((elevationMap[x, y] * 255));

                    if (elevationMap[x, y] < 0.15)
                    {
                        Console.Write($"\u001b[48;2;{22};{22};{69}m  "); // deep water
                    }
                    else if (elevationMap[x, y] < 0.2)
                    {
                        Console.Write($"\u001b[48;2;{68};{68};{122}m  "); // shallow water
                    }


                    else if (elevationMap[x, y] < 0.3)
                    {
                        if (moistureMap[x, y] < 0.16)
                        {
                            Console.Write($"\u001b[48;2;{210};{185};{139}m  ");
                        }
                        else if (moistureMap[x, y] < 0.5)
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
                            Console.Write($"\u001b[48;2;{210};{210};{159}m  ");
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

            for (int y = 0; y < sizeY; y++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    int val = (int)((elevationMap[x, y] * 255));
                    Console.Write($"\u001b[48;2;{val};{val};{val}m  ");
                }
                Console.WriteLine("\u001b[0m");
            }
        }

        //static void Print2(int sizeX, int sizeY, double[,] elevationMap)
        //{
        //    for (int y = 0; y < sizeY; y++)
        //    {
        //        for (int x = 0; x < sizeX; x++)
        //        {
        //            int val = (int)((elevationMap[x, y] * 255));
        //            Console.Write($"\u001b[48;2;{val};{val};{val}m  ");
        //        }
        //        Console.WriteLine("\u001b[0m");
        //    }
        //}


        static void Main(string[] args)
        {
            SetUpConsole();

            bool seedUse = ValidateYN("Do you have a set seed to use? (y/n): ");
            if (seedUse == true)
            {
                int seed = ValidateInt("Enter seed to use: ", 0, 2147483647);
            }


            int sizeX = ValidateInt("Enter x size of grid (10 to 500): ", 10, 500);
            int sizeY = ValidateInt("Enter y size of grid (10 to 500): ", 10, 500);
            // Console.Write("Enter division size (pixels per cell): ");
            // int divisionSize = int.Parse(Console.ReadLine());
            int divisionSize = 50;
            int percentBlend = ValidateInt("Enter blend function percent (0 to 100, 20-40 reccomended): ", 0, 100);
            Console.WriteLine("Zoom out of console as much as possible then press ENTER when ready");
            Console.ReadKey();

            double waterThreshold = 0.2;

            int cornerVectorSizeX = (int)(((double)sizeX + divisionSize - 1) / divisionSize) + 1;
            int cornerVectorSizeY = (int)(((double)sizeX + divisionSize - 1) / divisionSize) + 1;
            Random rnd = new Random();
            double[,] cornerVectors = GenerateGradients(cornerVectorSizeX, cornerVectorSizeY, rnd);

            int octaves = 5;
            double persistence = 0.5;
            double lacunarity = 2.0;
            int baseDivisionSize = 32;

            double[,] moisture = GeneratePerlinNoise(sizeX, sizeY, baseDivisionSize, octaves, persistence, lacunarity);
            double[,] elevation = GeneratePerlinNoise(sizeX, sizeY, baseDivisionSize, octaves, persistence, lacunarity);
            elevation = Island(sizeX, sizeY, percentBlend, elevation);
            // moisture = Island(sizeX, sizeY, percentBlend, elevation);
            // elevation = Normalisation(size, moisture);

            Print(sizeX, sizeY, elevation, moisture, waterThreshold);
            Console.WriteLine("Done");
            Console.ReadKey();
        }

        static int ValidateInt(string text, int min, int max)
        {
            while (true)
            {
                int ans;
                Console.Write(text);
                if (int.TryParse(Console.ReadLine(), out ans))
                {
                    if (ans >= min && ans <= max)
                    {
                        return ans;
                    }
                    else
                    {
                        Console.Clear();
                        Console.WriteLine($"Number must be between {min} and {max} inclusive");
                    }
                }
                else
                {
                    Console.Clear();
                    Console.WriteLine("Input must be an integer");
                }
            }
        }

        static bool ValidateYN(string text)
        {
            while (true)
            {
                Console.Write(text);
                string ans = Console.ReadLine().Trim().ToLower().Substring(0,1);
                if (ans == "y")
                {
                    return true;
                }
                else if (ans == "n")
                {
                    return false;
                }
                else 
                {
                    Console.Clear();
                    Console.WriteLine("Input must be 'y' or 'n'");
                }
            }
        }
    }
}