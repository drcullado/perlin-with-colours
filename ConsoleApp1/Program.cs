using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

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
            double frequency = 0.5;
            Random rnd = new Random();

            for (int i = 0; i < octaves; i++)
            {
                int newDivisionSize = (int)(baseDivisionSize / frequency);
                double[,] gradients = GenerateGradients((sizeX / newDivisionSize) + 2, (sizeY / newDivisionSize) + 2, rnd);

                for (int y = 0; y < sizeY; y++)
                {
                    for (int x = 0; x < sizeY; x++)
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

        static double[,] Island(int sizeX, int sizeY, int percentBlnd, double[,] input, double waterThreshold)
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
                    if (input[x, y] < 0)
                    {
                        input[x, y] = 0;
                    }
                    else if (input[x, y] > 1)
                    {
                        input[x, y] = 1;
                    }
                    // start value from input, end value from islandGradient, multiply difference by percent blend, add to input
                }
            }

            // Print2(size, islandGradient);
            return input;
        }

        static void Print(int sizeX, int sizeY, double[,] elevationMap, double[,] moistureMap)
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

        static void Print2(int size, double[,] elevationMap)
        {
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
        static void ConstraintInitialise(int[] constraintsList)
        {
            int divisionSize = 50;
            int sizeX = constraintsList[0];
            int sizeY = constraintsList[0];
            int percentBlend = constraintsList[0];
            int cornerVectorSizeX = (int)(((double)sizeX + divisionSize - 1) / divisionSize) + 1;
            int cornerVectorSizeY = (int)(((double)sizeY + divisionSize - 1) / divisionSize) + 1;
        }
        
        static void Main(string[] args)
        {
            SetUpConsole();
            bool exit = false;
            int[] constraintsList = new int[3];
            do
            {
                int seedMenuChoice = 0;
                Console.WriteLine("\r\n███████╗░█████╗░███╗░░██╗████████╗░█████╗░░██████╗██╗░░░██╗  ███╗░░░███╗░█████╗░██████╗░\r\n██╔════╝██╔══██╗████╗░██║╚══██╔══╝██╔══██╗██╔════╝╚██╗░██╔╝  ████╗░████║██╔══██╗██╔══██╗\r\n█████╗░░███████║██╔██╗██║░░░██║░░░███████║╚█████╗░░╚████╔╝░  ██╔████╔██║███████║██████╔╝\r\n██╔══╝░░██╔══██║██║╚████║░░░██║░░░██╔══██║░╚═══██╗░░╚██╔╝░░  ██║╚██╔╝██║██╔══██║██╔═══╝░\r\n██║░░░░░██║░░██║██║░╚███║░░░██║░░░██║░░██║██████╔╝░░░██║░░░  ██║░╚═╝░██║██║░░██║██║░░░░░\r\n╚═╝░░░░░╚═╝░░╚═╝╚═╝░░╚══╝░░░╚═╝░░░╚═╝░░╚═╝╚═════╝░░░░╚═╝░░░  ╚═╝░░░░░╚═╝╚═╝░░╚═╝╚═╝░░░░░\r\n\r\n███╗░░░███╗░█████╗░██╗░░██╗███████╗██████╗░\r\n████╗░████║██╔══██╗██║░██╔╝██╔════╝██╔══██╗\r\n██╔████╔██║███████║█████═╝░█████╗░░██████╔╝\r\n██║╚██╔╝██║██╔══██║██╔═██╗░██╔══╝░░██╔══██╗\r\n██║░╚═╝░██║██║░░██║██║░╚██╗███████╗██║░░██║\r\n╚═╝░░░░░╚═╝╚═╝░░╚═╝╚═╝░░╚═╝╚══════╝╚═╝░░╚═╝");
                Console.WriteLine("");
                seedMenuChoice += SeedMenu();

                if (seedMenuChoice == 1) // seed and constraints given
                {
                    Console.Clear();
                    Console.WriteLine("\r\n███████╗░█████╗░███╗░░██╗████████╗░█████╗░░██████╗██╗░░░██╗  ███╗░░░███╗░█████╗░██████╗░\r\n██╔════╝██╔══██╗████╗░██║╚══██╔══╝██╔══██╗██╔════╝╚██╗░██╔╝  ████╗░████║██╔══██╗██╔══██╗\r\n█████╗░░███████║██╔██╗██║░░░██║░░░███████║╚█████╗░░╚████╔╝░  ██╔████╔██║███████║██████╔╝\r\n██╔══╝░░██╔══██║██║╚████║░░░██║░░░██╔══██║░╚═══██╗░░╚██╔╝░░  ██║╚██╔╝██║██╔══██║██╔═══╝░\r\n██║░░░░░██║░░██║██║░╚███║░░░██║░░░██║░░██║██████╔╝░░░██║░░░  ██║░╚═╝░██║██║░░██║██║░░░░░\r\n╚═╝░░░░░╚═╝░░╚═╝╚═╝░░╚══╝░░░╚═╝░░░╚═╝░░╚═╝╚═════╝░░░░╚═╝░░░  ╚═╝░░░░░╚═╝╚═╝░░╚═╝╚═╝░░░░░\r\n\r\n███╗░░░███╗░█████╗░██╗░░██╗███████╗██████╗░\r\n████╗░████║██╔══██╗██║░██╔╝██╔════╝██╔══██╗\r\n██╔████╔██║███████║█████═╝░█████╗░░██████╔╝\r\n██║╚██╔╝██║██╔══██║██╔═██╗░██╔══╝░░██╔══██╗\r\n██║░╚═╝░██║██║░░██║██║░╚██╗███████╗██║░░██║\r\n╚═╝░░░░░╚═╝╚═╝░░╚═╝╚═╝░░╚═╝╚══════╝╚═╝░░╚═╝");
                    int seed = ValidateInt("Enter a given seed: ", 0, 2147483647); // fix number here
                }
                else if (seedMenuChoice == 2) // old seed, new constraints
                {
                    Console.Clear();
                    Console.WriteLine("\r\n███████╗░█████╗░███╗░░██╗████████╗░█████╗░░██████╗██╗░░░██╗  ███╗░░░███╗░█████╗░██████╗░\r\n██╔════╝██╔══██╗████╗░██║╚══██╔══╝██╔══██╗██╔════╝╚██╗░██╔╝  ████╗░████║██╔══██╗██╔══██╗\r\n█████╗░░███████║██╔██╗██║░░░██║░░░███████║╚█████╗░░╚████╔╝░  ██╔████╔██║███████║██████╔╝\r\n██╔══╝░░██╔══██║██║╚████║░░░██║░░░██╔══██║░╚═══██╗░░╚██╔╝░░  ██║╚██╔╝██║██╔══██║██╔═══╝░\r\n██║░░░░░██║░░██║██║░╚███║░░░██║░░░██║░░██║██████╔╝░░░██║░░░  ██║░╚═╝░██║██║░░██║██║░░░░░\r\n╚═╝░░░░░╚═╝░░╚═╝╚═╝░░╚══╝░░░╚═╝░░░╚═╝░░╚═╝╚═════╝░░░░╚═╝░░░  ╚═╝░░░░░╚═╝╚═╝░░╚═╝╚═╝░░░░░\r\n\r\n███╗░░░███╗░█████╗░██╗░░██╗███████╗██████╗░\r\n████╗░████║██╔══██╗██║░██╔╝██╔════╝██╔══██╗\r\n██╔████╔██║███████║█████═╝░█████╗░░██████╔╝\r\n██║╚██╔╝██║██╔══██║██╔═██╗░██╔══╝░░██╔══██╗\r\n██║░╚═╝░██║██║░░██║██║░╚██╗███████╗██║░░██║\r\n╚═╝░░░░░╚═╝╚═╝░░╚═╝╚═╝░░╚═╝╚══════╝╚═╝░░╚═╝");
                    Console.WriteLine("");
                    constraintsList = ConstraintsMenu(constraintsList);
                    ConstraintInitialise(constraintsList);

                }
                else if (seedMenuChoice == 3) // rnd seed, new constraints
                {
                    Console.Clear();
                    Console.WriteLine("\r\n███████╗░█████╗░███╗░░██╗████████╗░█████╗░░██████╗██╗░░░██╗  ███╗░░░███╗░█████╗░██████╗░\r\n██╔════╝██╔══██╗████╗░██║╚══██╔══╝██╔══██╗██╔════╝╚██╗░██╔╝  ████╗░████║██╔══██╗██╔══██╗\r\n█████╗░░███████║██╔██╗██║░░░██║░░░███████║╚█████╗░░╚████╔╝░  ██╔████╔██║███████║██████╔╝\r\n██╔══╝░░██╔══██║██║╚████║░░░██║░░░██╔══██║░╚═══██╗░░╚██╔╝░░  ██║╚██╔╝██║██╔══██║██╔═══╝░\r\n██║░░░░░██║░░██║██║░╚███║░░░██║░░░██║░░██║██████╔╝░░░██║░░░  ██║░╚═╝░██║██║░░██║██║░░░░░\r\n╚═╝░░░░░╚═╝░░╚═╝╚═╝░░╚══╝░░░╚═╝░░░╚═╝░░╚═╝╚═════╝░░░░╚═╝░░░  ╚═╝░░░░░╚═╝╚═╝░░╚═╝╚═╝░░░░░\r\n\r\n███╗░░░███╗░█████╗░██╗░░██╗███████╗██████╗░\r\n████╗░████║██╔══██╗██║░██╔╝██╔════╝██╔══██╗\r\n██╔████╔██║███████║█████═╝░█████╗░░██████╔╝\r\n██║╚██╔╝██║██╔══██║██╔═██╗░██╔══╝░░██╔══██╗\r\n██║░╚═╝░██║██║░░██║██║░╚██╗███████╗██║░░██║\r\n╚═╝░░░░░╚═╝╚═╝░░╚═╝╚═╝░░╚═╝╚══════╝╚═╝░░╚═╝");
                    Console.WriteLine("");
                    constraintsList = ConstraintsMenu(constraintsList);
                    ConstraintInitialise(constraintsList);
                }

                if (constraintsList[0] != null)
                {
                    exit = true;
                }
            } while (exit == false);

            int divisionSize = 50;
            int sizeX = constraintsList[0];
            int sizeY = constraintsList[0];
            int percentBlend = constraintsList[0];
            int cornerVectorSizeX = (int)(((double)sizeX + divisionSize - 1) / divisionSize) + 1;
            int cornerVectorSizeY = (int)(((double)sizeY + divisionSize - 1) / divisionSize) + 1;

            Random rnd = new Random();
            double[,] cornerVectors = GenerateGradients(cornerVectorSizeX, cornerVectorSizeY, rnd);

            int octaves = 5;
            double persistence = 0.5;
            double lacunarity = 2.0;
            int baseDivisionSize = 32;

            double waterThreshold = 0.2;
            double[,] moisture = GeneratePerlinNoise(sizeX, sizeY, baseDivisionSize, octaves, persistence, lacunarity);
            double[,] elevation = GeneratePerlinNoise(sizeX, sizeY, baseDivisionSize, octaves, persistence, lacunarity);
            elevation = Island(sizeX, sizeY, percentBlend, elevation, waterThreshold);
            // elevation = Normalisation(size, moisture);

            Print(sizeX, sizeY, elevation, moisture);

            Console.WriteLine("Done");
            Console.ReadKey();
        }

        static int ValidateInt(string text, int min, int max)
        {
            while (true)
            {
                int ans;
                // Console.Write(text);
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

        static int ValidateInt2(string text, int min, int max, int option)
        {
            while (true)
            {
                int ans;
                if (int.TryParse(Console.ReadLine(), out ans))
                {
                    if (ans >= min && ans <= max)
                    {
                        Console.CursorTop = option + 16;
                        Console.WriteLine($"{text}{ans}                                            ");
                        return ans;
                    }
                    else
                    {
                        Console.CursorTop = option + 16;
                        Console.Write($"{text}");
                        Console.CursorLeft = text.Length;
                        Console.CursorTop = option + 16;
                        Console.Write($"   Number must be between {min} and {max} inclusive");
                        Console.CursorLeft = text.Length - 1;
                    }
                }
                else
                {
                    Console.CursorTop = option + 16;
                    Console.Write(text);
                    Console.CursorLeft = text.Length;
                    Console.WriteLine("   Input must be an integer");
                    Console.CursorTop = option + 16;
                    Console.CursorLeft = text.Length - 1;
                }
            }
        }

        static int SeedMenu()
        {
            Console.WriteLine("  SEED PREFERENCE");
            Console.WriteLine("> Load a previous seed with constraints");
            Console.WriteLine("  Use a previous seed");
            Console.WriteLine("  Use a new random seed");

            int numOfOptions = 3;
            int option = 0;
            do
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.DownArrow && option < numOfOptions - 1) // down normal
                {
                    Console.CursorTop = option + 16;
                    Console.CursorLeft = 0;
                    Console.WriteLine(" ");
                    option++;
                    Console.CursorTop = option + 16;
                    Console.CursorLeft = 0;
                    Console.WriteLine(">");
                }
                else if (key.Key == ConsoleKey.DownArrow && option == numOfOptions - 1) // down bottom
                {
                    Console.CursorTop = option + 16;
                    Console.CursorLeft = 0;
                    Console.WriteLine(" ");
                    option = 0;
                    Console.CursorTop = 16;
                    Console.CursorLeft = 0;
                    Console.WriteLine(">");
                }
                else if (key.Key == ConsoleKey.UpArrow && option == 0) // up top
                {
                    Console.CursorTop = 16;
                    Console.CursorLeft = 0;
                    Console.WriteLine(" ");
                    option = numOfOptions - 1;
                    Console.CursorTop = option + 16;
                    Console.CursorLeft = 0;
                    Console.WriteLine(">");
                }
                else if (key.Key == ConsoleKey.UpArrow && option > 0) // up normal
                {
                    Console.CursorTop = option + 16;
                    Console.CursorLeft = 0;
                    Console.WriteLine(" ");
                    option--;
                    Console.CursorTop = option + 16;
                    Console.CursorLeft = 0;
                    Console.WriteLine(">");
                }
                else if (key.Key == ConsoleKey.Enter) // enter
                {
                    if (option == 0)
                    {
                        return 1;
                    }
                    else if (option == 1)
                    {
                        return 2;
                    }
                    else if (option == 2)
                    {
                        return 3;
                    }
                }
            } while (true);
        }

        static int[] ConstraintsMenu(int[] constraintsList)
        {
            // Console.WriteLine("  CONSTRAINTS MENU");
            // Console.WriteLine("> Enter x size of grid (10 to 500)                          : ");
            // Console.WriteLine("  Enter blend function percent (0 to 100, 20-40 reccomended): ");
            // Console.WriteLine("  Enter north temperature (0 to 100)                        : ");
            // Console.WriteLine("  Enter south temperature (0 to 100)                        : ");
            // Console.WriteLine("  Generate Map");

            Console.WriteLine("  CONSTRAINTS MENU");
            Console.WriteLine("> Enter x size of grid (10 to 500)                           : ");
            Console.WriteLine("  Enter y size of grid (10 to 500)                           : ");
            Console.WriteLine("  Enter blend function percent (0 to 100, 20-40 reccomended) : ");
            Console.WriteLine("  Generate Map (zoom out as much as possible before selecting) ");

            int numOfOptions = 4;
            int option = 0;
            do
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.DownArrow && option < numOfOptions - 1) // down normal
                {
                    Console.CursorTop = option + 16;
                    Console.CursorLeft = 0;
                    Console.WriteLine(" ");
                    option++;
                    Console.CursorTop = option + 16;
                    Console.CursorLeft = 0;
                    Console.WriteLine(">");
                }
                else if (key.Key == ConsoleKey.DownArrow && option == numOfOptions - 1) // down bottom
                {
                    Console.CursorTop = option + 16;
                    Console.CursorLeft = 0;
                    Console.WriteLine(" ");
                    option = 0;
                    Console.CursorTop = 16;
                    Console.CursorLeft = 0;
                    Console.WriteLine(">");
                }
                else if (key.Key == ConsoleKey.UpArrow && option == 0) // up top
                {
                    Console.CursorTop = 16;
                    Console.CursorLeft = 0;
                    Console.WriteLine(" ");
                    option = numOfOptions - 1;
                    Console.CursorTop = option + 16;
                    Console.CursorLeft = 0;
                    Console.WriteLine(">");
                }
                else if (key.Key == ConsoleKey.UpArrow && option > 0) // up normal
                {
                    Console.CursorTop = option + 16;
                    Console.CursorLeft = 0;
                    Console.WriteLine(" ");
                    option--;
                    Console.CursorTop = option + 16;
                    Console.CursorLeft = 0;
                    Console.WriteLine(">");
                }
                else if (key.Key == ConsoleKey.Enter && option != numOfOptions - 1) // enter
                {
                    if (option == numOfOptions - 1)
                    {
                        return constraintsList;
                    }
                    else if (option == 0)
                    {
                        Console.CursorTop = option + 16;
                        Console.CursorLeft = 63;
                        int choice = ValidateInt2("> Enter x size of grid (10 to 500)                          : ", 10, 500, option);
                        constraintsList[option] = choice;
                        Console.CursorTop = option + 16;
                    }
                    else if (option == 1)
                    {
                        Console.CursorTop = option + 16;
                        Console.CursorLeft = 63;
                        int choice = ValidateInt2("> Enter y size of grid (10 to 500)                          : ", 10, 500, option);
                        constraintsList[option] = choice;
                        Console.CursorTop = option + 16;
                    }
                    else if (option == 2)
                    {
                        Console.CursorTop = option + 16;
                        Console.CursorLeft = 63;
                        int choice = ValidateInt2("  Enter blend function percent (0 to 100, 20-40 reccomended): ", 0, 100, option);
                        constraintsList[option] = choice;
                        Console.CursorTop = option + 16;
                    }
                }
                else if (key.Key == ConsoleKey.Enter && option == numOfOptions - 1) // generate map
                {
                    return constraintsList;
                }
                else if (key.Key == ConsoleKey.Escape && option < 1) // escape
                {
                    constraintsList = new int[numOfOptions];
                    return constraintsList;
                }

            } while (true);
        }
    }
}