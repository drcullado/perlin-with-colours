using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;//
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace Nea_as_of_december
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

        public struct vectorAngles { public double X, Y; }

        public static class PerlinNoise
        {
            private static double Fade(double t)
            {
                return t * t * t * (t * (t * 6 - 15) + 10);
            }

            private static double Lerp(double x, double y, double z)
            {
                return x + z * (y - x);
            }

            private static double DotProduct(double dx, double dy, double sin, double cos)
            {
                return sin * dx + cos * dy;
            }

            private static double Perlin(double x, double y, vectorAngles[,] cornerVectors, int divisionSize)
            {
                int x0 = (int)(Math.Floor(x / divisionSize));
                int y0 = (int)(Math.Floor(y / divisionSize));
                int x1 = x0 + 1, y1 = y0 + 1;
                double dx = (x / divisionSize) - x0, dy = (y / divisionSize) - y0;

                double dotProd00 = DotProduct(dx, dy, cornerVectors[x0, y0].X, cornerVectors[x0, y0].Y);
                double dotProd10 = DotProduct(dx - 1, dy, cornerVectors[x1, y0].X, cornerVectors[x1, y0].Y);
                double dotProd01 = DotProduct(dx, dy - 1, cornerVectors[x0, y1].X, cornerVectors[x0, y1].Y);
                double dotProd11 = DotProduct(dx - 1, dy - 1, cornerVectors[x1, y1].X, cornerVectors[x1, y1].Y);

                double u = Fade(dx), v = Fade(dy);
                double val = Lerp(Lerp(dotProd00, dotProd10, u), Lerp(dotProd01, dotProd11, u), v);
                return (val + 1) * 0.5;
            }

            public static vectorAngles[,] GenerateGradients(int sizeX, int sizeY, Random rndSeed)
            {
                vectorAngles[,] outputGrid = new vectorAngles[sizeX, sizeY];
                for (int y = 0; y < sizeY; y++)
                {
                    for (int x = 0; x < sizeX; x++)
                    {
                        double angle = rndSeed.NextDouble() * Math.PI * 2;
                        outputGrid[x, y].X = Math.Cos(angle);
                        outputGrid[x, y].Y = Math.Sin(angle);
                    }
                }
                return outputGrid;
            }

            private static double[,] Normalisation(int sizeX, int sizeY, double[,] input)
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

            public static double[,] Island(int sizeX, int sizeY, int percentBlnd, double[,] input, double waterThreshold)
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
                    }
                }
                return input;
            }

            public static double[,] GeneratePerlinNoise(int seed, int sizeX, int sizeY, int baseDivisionSize, int octaves, double persistence, double lacunarity)
            {
                double[,] outputMap = new double[sizeX, sizeY];
                double maxPossible = 0;
                double amplitude = 1; // strength of the octave
                double frequency = 0.5; // scale of the noise
                Random rnd = new Random(seed);

                for (int i = 0; i < octaves; i++)
                {
                    int newDivisionSize = (int)(baseDivisionSize / frequency);
                    vectorAngles[,] gradients = GenerateGradients((sizeX / newDivisionSize) + 2, (sizeY / newDivisionSize) + 2, rnd);

                    for (int y = 0; y < sizeY; y++)
                    {
                        for (int x = 0; x < sizeX; x++)
                        {
                            outputMap[x, y] += Perlin(x, y, gradients, newDivisionSize) * amplitude;
                        }
                    }
                    maxPossible += amplitude;
                    amplitude *= persistence; // rate of strength change
                    frequency *= lacunarity; // rate of detail increase
                }
                Normalisation(sizeX, sizeY, outputMap);
                return outputMap;
            }

            public static void GenerateGraph(int[] constraintsList, double seed, int seedMenuChoice)
            {
                int divisionSize = 50;
                int sizeX = constraintsList[0];
                int sizeY = constraintsList[1];
                int percentBlend = constraintsList[2];
                int cornerVectorSizeX = (int)(((double)sizeX + divisionSize - 1) / divisionSize) + 1;
                int cornerVectorSizeY = (int)(((double)sizeY + divisionSize - 1) / divisionSize) + 1;

                Random rndSeed = new Random((int)seed);
                vectorAngles[,] cornerVectors = PerlinNoise.GenerateGradients(cornerVectorSizeX, cornerVectorSizeY, rndSeed);

                int octaves = 5;
                double persistence = 0.5;
                double lacunarity = 2.0;
                int baseDivisionSize = 32;

                double waterThreshold = 0.2;
                double[,] moisture = PerlinNoise.GeneratePerlinNoise((int)seed, sizeX, sizeY, baseDivisionSize, octaves, persistence, lacunarity);
                double[,] elevation = PerlinNoise.GeneratePerlinNoise((int)seed, sizeX, sizeY, baseDivisionSize, octaves, persistence, lacunarity);
                elevation = PerlinNoise.Island(sizeX, sizeY, percentBlend, elevation, waterThreshold);

                Print(sizeX, sizeY, elevation, moisture);

                string val0 = "0";
                string val1 = "0";
                string val2 = "0";
                string val3 = "0";
                string seedString = ((int)seed).ToString();
                if (constraintsList[0] < 100)
                {
                    val0 += constraintsList[0];
                }
                else
                {
                    val0 = constraintsList[0].ToString();
                }
                if (constraintsList[1] < 100)
                {
                    val1 += constraintsList[1];
                }
                else
                {
                    val1 = constraintsList[1].ToString();
                }
                if (constraintsList[2] < 100)
                {
                    val2 += constraintsList[2];
                }
                else
                {
                    val2 = constraintsList[2].ToString();
                }
                if (seed < 1000000000)
                {
                    for (int i = 9 - seedString.Length; i > 0; i--)
                    {
                        val3 += "0";
                    }
                    val3 += seedString;
                }
                else
                {
                    val3 = ((int)seed).ToString();
                }



                if (seedMenuChoice == 1 || seedMenuChoice == 2)
                {
                    Console.WriteLine($"Seed: {val0}{val1}{val2}{val3}");
                }
                Console.WriteLine("Done");
            }
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
                            Console.Write($"\u001b[48;2;{210};{210};{155}m  ");
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

        static void ConstraintInitialise(int[] constraintsList)
        {
            int divisionSize = 50;
            int sizeX = constraintsList[0];
            int sizeY = constraintsList[1];
            int percentBlend = constraintsList[2];
            int cornerVectorSizeX = (int)(((double)sizeX + divisionSize - 1) / divisionSize) + 1;
            int cornerVectorSizeY = (int)(((double)sizeY + divisionSize - 1) / divisionSize) + 1;
        }
        
        static void Main(string[] args)
        {
            SetUpConsole();
            bool exit = false;
            int[] constraintsList = new int[4];
            double seed = 0;
            int randomSeed = 0;
            int seedMenuChoice = 0;

            int again;
            do
            {
                do
                {
                    Console.Clear();
                    Console.WriteLine("\r\n███████╗░█████╗░███╗░░██╗████████╗░█████╗░░██████╗██╗░░░██╗  ███╗░░░███╗░█████╗░██████╗░\r\n██╔════╝██╔══██╗████╗░██║╚══██╔══╝██╔══██╗██╔════╝╚██╗░██╔╝  ████╗░████║██╔══██╗██╔══██╗\r\n█████╗░░███████║██╔██╗██║░░░██║░░░███████║╚█████╗░░╚████╔╝░  ██╔████╔██║███████║██████╔╝\r\n██╔══╝░░██╔══██║██║╚████║░░░██║░░░██╔══██║░╚═══██╗░░╚██╔╝░░  ██║╚██╔╝██║██╔══██║██╔═══╝░\r\n██║░░░░░██║░░██║██║░╚███║░░░██║░░░██║░░██║██████╔╝░░░██║░░░  ██║░╚═╝░██║██║░░██║██║░░░░░\r\n╚═╝░░░░░╚═╝░░╚═╝╚═╝░░╚══╝░░░╚═╝░░░╚═╝░░╚═╝╚═════╝░░░░╚═╝░░░  ╚═╝░░░░░╚═╝╚═╝░░╚═╝╚═╝░░░░░\r\n\r\n███╗░░░███╗░█████╗░██╗░░██╗███████╗██████╗░\r\n████╗░████║██╔══██╗██║░██╔╝██╔════╝██╔══██╗\r\n██╔████╔██║███████║█████═╝░█████╗░░██████╔╝\r\n██║╚██╔╝██║██╔══██║██╔═██╗░██╔══╝░░██╔══██╗\r\n██║░╚═╝░██║██║░░██║██║░╚██╗███████╗██║░░██║\r\n╚═╝░░░░░╚═╝╚═╝░░╚═╝╚═╝░░╚═╝╚══════╝╚═╝░░╚═╝");
                    Console.WriteLine("");
                    seedMenuChoice = SeedMenu();

                    if (seedMenuChoice == 1) // using a seed
                    {
                        Console.Clear();
                        Console.WriteLine("\r\n███████╗░█████╗░███╗░░██╗████████╗░█████╗░░██████╗██╗░░░██╗  ███╗░░░███╗░█████╗░██████╗░\r\n██╔════╝██╔══██╗████╗░██║╚══██╔══╝██╔══██╗██╔════╝╚██╗░██╔╝  ████╗░████║██╔══██╗██╔══██╗\r\n█████╗░░███████║██╔██╗██║░░░██║░░░███████║╚█████╗░░╚████╔╝░  ██╔████╔██║███████║██████╔╝\r\n██╔══╝░░██╔══██║██║╚████║░░░██║░░░██╔══██║░╚═══██╗░░╚██╔╝░░  ██║╚██╔╝██║██╔══██║██╔═══╝░\r\n██║░░░░░██║░░██║██║░╚███║░░░██║░░░██║░░██║██████╔╝░░░██║░░░  ██║░╚═╝░██║██║░░██║██║░░░░░\r\n╚═╝░░░░░╚═╝░░╚═╝╚═╝░░╚══╝░░░╚═╝░░░╚═╝░░╚═╝╚═════╝░░░░╚═╝░░░  ╚═╝░░░░░╚═╝╚═╝░░╚═╝╚═╝░░░░░\r\n\r\n███╗░░░███╗░█████╗░██╗░░██╗███████╗██████╗░\r\n████╗░████║██╔══██╗██║░██╔╝██╔════╝██╔══██╗\r\n██╔████╔██║███████║█████═╝░█████╗░░██████╔╝\r\n██║╚██╔╝██║██╔══██║██╔═██╗░██╔══╝░░██╔══██╗\r\n██║░╚═╝░██║██║░░██║██║░╚██╗███████╗██║░░██║\r\n╚═╝░░░░░╚═╝╚═╝░░╚═╝╚═╝░░╚═╝╚══════╝╚═╝░░╚═╝");
                        seed = ValidateSeed("Enter a given seed: ", 2147483647, constraintsList);
                        randomSeed = (int)seed;
                        Random random = new Random(randomSeed);

                    }
                    else if (seedMenuChoice == 2) // not using a seed
                    {
                        Console.Clear();
                        Console.WriteLine("\r\n███████╗░█████╗░███╗░░██╗████████╗░█████╗░░██████╗██╗░░░██╗  ███╗░░░███╗░█████╗░██████╗░\r\n██╔════╝██╔══██╗████╗░██║╚══██╔══╝██╔══██╗██╔════╝╚██╗░██╔╝  ████╗░████║██╔══██╗██╔══██╗\r\n█████╗░░███████║██╔██╗██║░░░██║░░░███████║╚█████╗░░╚████╔╝░  ██╔████╔██║███████║██████╔╝\r\n██╔══╝░░██╔══██║██║╚████║░░░██║░░░██╔══██║░╚═══██╗░░╚██╔╝░░  ██║╚██╔╝██║██╔══██║██╔═══╝░\r\n██║░░░░░██║░░██║██║░╚███║░░░██║░░░██║░░██║██████╔╝░░░██║░░░  ██║░╚═╝░██║██║░░██║██║░░░░░\r\n╚═╝░░░░░╚═╝░░╚═╝╚═╝░░╚══╝░░░╚═╝░░░╚═╝░░╚═╝╚═════╝░░░░╚═╝░░░  ╚═╝░░░░░╚═╝╚═╝░░╚═╝╚═╝░░░░░\r\n\r\n███╗░░░███╗░█████╗░██╗░░██╗███████╗██████╗░\r\n████╗░████║██╔══██╗██║░██╔╝██╔════╝██╔══██╗\r\n██╔████╔██║███████║█████═╝░█████╗░░██████╔╝\r\n██║╚██╔╝██║██╔══██║██╔═██╗░██╔══╝░░██╔══██╗\r\n██║░╚═╝░██║██║░░██║██║░╚██╗███████╗██║░░██║\r\n╚═╝░░░░░╚═╝╚═╝░░╚═╝╚═╝░░╚═╝╚══════╝╚═╝░░╚═╝");
                        Console.WriteLine("");
                        constraintsList = ConstraintsMenu(constraintsList);
                        ConstraintInitialise(constraintsList);
                        Random rnd = new Random();
                        randomSeed = rnd.Next(0, 2147483647);
                        Random random = new Random(randomSeed);
                    }

                    if (constraintsList[0] != 0)
                    {
                        exit = true;
                    }
                } while (exit == false);

                PerlinNoise.GenerateGraph(constraintsList, randomSeed, seedMenuChoice);
                Console.Write("Generate another map? (0 = no, 1 = yes): ");
                int.TryParse(Console.ReadLine(), out again);
                if (again == 0)
                {
                    Environment.Exit(0);
                }
                else if (again == 1)
                {
                    
                }
                else
                {
                    while (true)
                    {
                        Console.Write("Generate another map? (0 = no, 1 = yes): ");
                    }
                }
            } while (again == 1);
        }

        static int ValidateSeed(string text, double seed, int[] constraintsList)
        {
            bool valid = false;
            do
            {
                Console.Clear();
                Console.WriteLine("\r\n███████╗░█████╗░███╗░░██╗████████╗░█████╗░░██████╗██╗░░░██╗  ███╗░░░███╗░█████╗░██████╗░\r\n██╔════╝██╔══██╗████╗░██║╚══██╔══╝██╔══██╗██╔════╝╚██╗░██╔╝  ████╗░████║██╔══██╗██╔══██╗\r\n█████╗░░███████║██╔██╗██║░░░██║░░░███████║╚█████╗░░╚████╔╝░  ██╔████╔██║███████║██████╔╝\r\n██╔══╝░░██╔══██║██║╚████║░░░██║░░░██╔══██║░╚═══██╗░░╚██╔╝░░  ██║╚██╔╝██║██╔══██║██╔═══╝░\r\n██║░░░░░██║░░██║██║░╚███║░░░██║░░░██║░░██║██████╔╝░░░██║░░░  ██║░╚═╝░██║██║░░██║██║░░░░░\r\n╚═╝░░░░░╚═╝░░╚═╝╚═╝░░╚══╝░░░╚═╝░░░╚═╝░░╚═╝╚═════╝░░░░╚═╝░░░  ╚═╝░░░░░╚═╝╚═╝░░╚═╝╚═╝░░░░░\r\n\r\n███╗░░░███╗░█████╗░██╗░░██╗███████╗██████╗░\r\n████╗░████║██╔══██╗██║░██╔╝██╔════╝██╔══██╗\r\n██╔████╔██║███████║█████═╝░█████╗░░██████╔╝\r\n██║╚██╔╝██║██╔══██║██╔═██╗░██╔══╝░░██╔══██╗\r\n██║░╚═╝░██║██║░░██║██║░╚██╗███████╗██║░░██║\r\n╚═╝░░░░░╚═╝╚═╝░░╚═╝╚═╝░░╚═╝╚══════╝╚═╝░░╚═╝");
                Console.WriteLine("");
                Console.Write(text);
                string input = Console.ReadLine();

                if (input.Length == 19)
                {
                    if (int.TryParse(input.Substring(9, 10), out constraintsList[3]) == false)
                    {
                        Console.CursorTop = 15;
                        Console.Write($"{text}                                   ");
                        Console.CursorLeft = text.Length;
                        Console.Write($"   The seed is not in the correct format");
                        Console.CursorLeft = text.Length - 1;
                    }
                    else
                    {
                        constraintsList[3] = int.Parse(input.Substring(9, 10));

                        if (int.TryParse(input.Substring(0, 3), out constraintsList[0]) == false || int.TryParse(input.Substring(3, 3), out constraintsList[0]) == false || int.TryParse(input.Substring(6, 3), out constraintsList[0]) == false)
                        {
                            Console.CursorTop = 16;
                            Console.Write($"{text}");
                            Console.CursorLeft = text.Length;
                            Console.CursorTop = 16;
                            Console.Write($"   The seed is not in the correct format");
                            Console.CursorLeft = text.Length - 1;
                        }
                        else
                        {
                            constraintsList[0] = int.Parse((input.Substring(0, 3)));
                            constraintsList[1] = int.Parse((input.Substring(3, 3)));
                            constraintsList[2] = int.Parse((input.Substring(6, 3)));
                            ConstraintInitialise(constraintsList);
                            valid = true;
                        }
                    }

                }
                else
                {
                    Console.CursorTop = 0;
                    Console.Write($"{text}");
                    Console.CursorLeft = text.Length + 19;
                    Console.CursorTop = 15;
                    Console.Write("Length of the seed given is invalid");
                    Console.CursorLeft = text.Length - 1;
                }
                
            } while (valid == false);
            return constraintsList[3];
        }

        static int ValidateInt(string text, int min, int max, int option)
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
            Console.WriteLine("> Load a previous seed");
            Console.WriteLine("  Use a new random seed");

            int numOfOptions = 2;
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
                    else 
                    {
                        return 2;
                    }
                }
            } while (true);
        }

        static int[] ConstraintsMenu(int[] constraintsList)
        {
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
                        int choice = ValidateInt("> Enter x size of grid (10 to 500)                           : ", 10, 500, option);
                        constraintsList[option] = choice;
                        Console.CursorTop = option + 16;
                    }
                    else if (option == 1)
                    {
                        Console.CursorTop = option + 16;
                        Console.CursorLeft = 63;
                        int choice = ValidateInt("> Enter y size of grid (10 to 500)                           : ", 10, 500, option);
                        constraintsList[option] = choice;
                        Console.CursorTop = option + 16;
                    }
                    else if (option == 2)
                    {
                        Console.CursorTop = option + 16;
                        Console.CursorLeft = 63;
                        int choice = ValidateInt("  Enter blend function percent (0 to 100, 20-40 reccomended):  ", 0, 100, option);
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
                    Console.Clear();
                    constraintsList = new int[numOfOptions];
                    return constraintsList;
                }

            } while (true);
        }
    }
}