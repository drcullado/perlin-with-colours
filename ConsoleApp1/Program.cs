using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.SqlTypes;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

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

        public static class PoissonDisk
        {
            public struct Point
            {
                public double X, Y;
                public Point(double x, double y)
                {
                    X = x; Y = y;
                }
            }

            public static List<Point> GeneratePoissonPoints(int width, int height, double radius, int k = 30, int? seed = null)
            {
                Random rnd = seed != null ? new Random(seed.Value) : new Random();

                double cellSize = radius / Math.Sqrt(2);
                int gridWidth = (int)Math.Ceiling(width / cellSize);
                int gridHeight = (int)Math.Ceiling(height / cellSize);

                Point?[,] grid = new Point?[gridWidth, gridHeight];

                List<Point> active = new List<Point>();
                List<Point> points = new List<Point>();

                Point first = new Point(rnd.NextDouble() * width, rnd.NextDouble() * height);
                points.Add(first);
                active.Add(first);
                grid[(int)(first.X / cellSize), (int)(first.Y / cellSize)] = first;

                while (active.Count > 0)
                {
                    int index = rnd.Next(active.Count);
                    Point checkPoint = active[index];
                    bool found = false;

                    for (int i = 0; i < k; i++)
                    {
                        double angle = rnd.NextDouble() * Math.PI * 2;
                        double dist = radius + radius * rnd.NextDouble();
                        double nx = checkPoint.X + Math.Cos(angle) * dist;
                        double ny = checkPoint.Y + Math.Sin(angle) * dist;

                        if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                            continue;

                        Point otherPoint = new Point(nx, ny);
                        if (IsFarEnough(otherPoint, grid, cellSize, radius))
                        {
                            points.Add(otherPoint);
                            active.Add(otherPoint);
                            grid[(int)(nx / cellSize), (int)(ny / cellSize)] = otherPoint;
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                        active.RemoveAt(index);
                }

                return points;
            }

            private static bool IsFarEnough(Point point, Point?[,] grid, double cellSize, double radius)
            {
                int dx = (int)(point.X / cellSize);
                int dy = (int)(point.Y / cellSize);
                int search = 2; 

                for (int y = dy - search; y <= dy + search; y++)
                {
                    for (int x = dx - search; x <= dx + search; x++)
                    {
                        if (x < 0 || x >= grid.GetLength(0) ||
                            y < 0 || y >= grid.GetLength(1))
                            continue;

                        if (grid[x, y].HasValue)
                        {
                            Point other = grid[x, y].Value;
                            double vx = point.X - other.X;
                            double vy = point.Y - other.Y;
                            if (dx * dx + dy * dy < radius * radius)
                                return false;
                        }
                    }
                }

                return true;
            }
        }

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
            
            private static double Perlin(double x, double y, vectorAngles[,] cornerVectors, int divisionSize)
            {
                int x0 = (int)(Math.Floor(x / divisionSize));
                int y0 = (int)(Math.Floor(y / divisionSize));
                int x1 = x0 + 1, y1 = y0 + 1;
                double dx = (x / divisionSize) - x0, dy = (y / divisionSize) - y0;

                double dotProd00 = (cornerVectors[x0, y0].X * dx) + (cornerVectors[x0, y0].Y * dy);
                double dotProd10 = (cornerVectors[x1, y0].X * (dx - 1)) + (cornerVectors[x1, y0].Y * dy);
                double dotProd01 = (cornerVectors[x0, y1].X * dx) + (cornerVectors[x0, y1].Y * (dy - 1));
                double dotProd11 = (cornerVectors[x1, y1].X * (dx - 1)) + (cornerVectors[x1, y1].Y * (dy - 1));

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

            public static double[,] GeneratePerlinNoise(int sizeX, int sizeY, int baseDivisionSize, int octaves, double persistence, double lacunarity)
            {
                double[,] outputMap = new double[sizeX, sizeY];
                double maxPossible = 0;
                double amplitude = 1;
                double frequency = 0.5;
                Random rnd = new Random();

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
                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                Normalisation(sizeX, sizeY, outputMap);
                return outputMap;
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
                        // start value from input, end value from islandGradient, multiply difference by percent blend, add to input
                    }
                }
                return input;
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
            double seed = 0;
            Random random = new Random();
            int seedMenuChoice = 0;


            do
            {
                Console.Clear();
                Console.WriteLine("\r\n███████╗░█████╗░███╗░░██╗████████╗░█████╗░░██████╗██╗░░░██╗  ███╗░░░███╗░█████╗░██████╗░\r\n██╔════╝██╔══██╗████╗░██║╚══██╔══╝██╔══██╗██╔════╝╚██╗░██╔╝  ████╗░████║██╔══██╗██╔══██╗\r\n█████╗░░███████║██╔██╗██║░░░██║░░░███████║╚█████╗░░╚████╔╝░  ██╔████╔██║███████║██████╔╝\r\n██╔══╝░░██╔══██║██║╚████║░░░██║░░░██╔══██║░╚═══██╗░░╚██╔╝░░  ██║╚██╔╝██║██╔══██║██╔═══╝░\r\n██║░░░░░██║░░██║██║░╚███║░░░██║░░░██║░░██║██████╔╝░░░██║░░░  ██║░╚═╝░██║██║░░██║██║░░░░░\r\n╚═╝░░░░░╚═╝░░╚═╝╚═╝░░╚══╝░░░╚═╝░░░╚═╝░░╚═╝╚═════╝░░░░╚═╝░░░  ╚═╝░░░░░╚═╝╚═╝░░╚═╝╚═╝░░░░░\r\n\r\n███╗░░░███╗░█████╗░██╗░░██╗███████╗██████╗░\r\n████╗░████║██╔══██╗██║░██╔╝██╔════╝██╔══██╗\r\n██╔████╔██║███████║█████═╝░█████╗░░██████╔╝\r\n██║╚██╔╝██║██╔══██║██╔═██╗░██╔══╝░░██╔══██╗\r\n██║░╚═╝░██║██║░░██║██║░╚██╗███████╗██║░░██║\r\n╚═╝░░░░░╚═╝╚═╝░░╚═╝╚═╝░░╚═╝╚══════╝╚═╝░░╚═╝");
                Console.WriteLine("");
                seedMenuChoice += SeedMenu();

                if (seedMenuChoice == 1) // using a seed
                {
                    Console.Clear();
                    Console.WriteLine("\r\n███████╗░█████╗░███╗░░██╗████████╗░█████╗░░██████╗██╗░░░██╗  ███╗░░░███╗░█████╗░██████╗░\r\n██╔════╝██╔══██╗████╗░██║╚══██╔══╝██╔══██╗██╔════╝╚██╗░██╔╝  ████╗░████║██╔══██╗██╔══██╗\r\n█████╗░░███████║██╔██╗██║░░░██║░░░███████║╚█████╗░░╚████╔╝░  ██╔████╔██║███████║██████╔╝\r\n██╔══╝░░██╔══██║██║╚████║░░░██║░░░██╔══██║░╚═══██╗░░╚██╔╝░░  ██║╚██╔╝██║██╔══██║██╔═══╝░\r\n██║░░░░░██║░░██║██║░╚███║░░░██║░░░██║░░██║██████╔╝░░░██║░░░  ██║░╚═╝░██║██║░░██║██║░░░░░\r\n╚═╝░░░░░╚═╝░░╚═╝╚═╝░░╚══╝░░░╚═╝░░░╚═╝░░╚═╝╚═════╝░░░░╚═╝░░░  ╚═╝░░░░░╚═╝╚═╝░░╚═╝╚═╝░░░░░\r\n\r\n███╗░░░███╗░█████╗░██╗░░██╗███████╗██████╗░\r\n████╗░████║██╔══██╗██║░██╔╝██╔════╝██╔══██╗\r\n██╔████╔██║███████║█████═╝░█████╗░░██████╔╝\r\n██║╚██╔╝██║██╔══██║██╔═██╗░██╔══╝░░██╔══██╗\r\n██║░╚═╝░██║██║░░██║██║░╚██╗███████╗██║░░██║\r\n╚═╝░░░░░╚═╝╚═╝░░╚═╝╚═╝░░╚═╝╚══════╝╚═╝░░╚═╝");
                    seed = ValidateSeed("Enter a given seed: ", "2147000000"); // fix number here
                }
                else if (seedMenuChoice == 2) // not using a seed
                {
                    Console.Clear();
                    Console.WriteLine("\r\n███████╗░█████╗░███╗░░██╗████████╗░█████╗░░██████╗██╗░░░██╗  ███╗░░░███╗░█████╗░██████╗░\r\n██╔════╝██╔══██╗████╗░██║╚══██╔══╝██╔══██╗██╔════╝╚██╗░██╔╝  ████╗░████║██╔══██╗██╔══██╗\r\n█████╗░░███████║██╔██╗██║░░░██║░░░███████║╚█████╗░░╚████╔╝░  ██╔████╔██║███████║██████╔╝\r\n██╔══╝░░██╔══██║██║╚████║░░░██║░░░██╔══██║░╚═══██╗░░╚██╔╝░░  ██║╚██╔╝██║██╔══██║██╔═══╝░\r\n██║░░░░░██║░░██║██║░╚███║░░░██║░░░██║░░██║██████╔╝░░░██║░░░  ██║░╚═╝░██║██║░░██║██║░░░░░\r\n╚═╝░░░░░╚═╝░░╚═╝╚═╝░░╚══╝░░░╚═╝░░░╚═╝░░╚═╝╚═════╝░░░░╚═╝░░░  ╚═╝░░░░░╚═╝╚═╝░░╚═╝╚═╝░░░░░\r\n\r\n███╗░░░███╗░█████╗░██╗░░██╗███████╗██████╗░\r\n████╗░████║██╔══██╗██║░██╔╝██╔════╝██╔══██╗\r\n██╔████╔██║███████║█████═╝░█████╗░░██████╔╝\r\n██║╚██╔╝██║██╔══██║██╔═██╗░██╔══╝░░██╔══██╗\r\n██║░╚═╝░██║██║░░██║██║░╚██╗███████╗██║░░██║\r\n╚═╝░░░░░╚═╝╚═╝░░╚═╝╚═╝░░╚═╝╚══════╝╚═╝░░╚═╝");
                    Console.WriteLine("");
                    constraintsList = ConstraintsMenu(constraintsList);
                    ConstraintInitialise(constraintsList);
                }
                else if (seedMenuChoice == 3) // random generation
                {
                    Console.WriteLine("Zoom out before continuing and press any key when ready");
                    Console.ReadKey();
                    Console.Clear();
                    Console.WriteLine("\r\n███████╗░█████╗░███╗░░██╗████████╗░█████╗░░██████╗██╗░░░██╗  ███╗░░░███╗░█████╗░██████╗░\r\n██╔════╝██╔══██╗████╗░██║╚══██╔══╝██╔══██╗██╔════╝╚██╗░██╔╝  ████╗░████║██╔══██╗██╔══██╗\r\n█████╗░░███████║██╔██╗██║░░░██║░░░███████║╚█████╗░░╚████╔╝░  ██╔████╔██║███████║██████╔╝\r\n██╔══╝░░██╔══██║██║╚████║░░░██║░░░██╔══██║░╚═══██╗░░╚██╔╝░░  ██║╚██╔╝██║██╔══██║██╔═══╝░\r\n██║░░░░░██║░░██║██║░╚███║░░░██║░░░██║░░██║██████╔╝░░░██║░░░  ██║░╚═╝░██║██║░░██║██║░░░░░\r\n╚═╝░░░░░╚═╝░░╚═╝╚═╝░░╚══╝░░░╚═╝░░░╚═╝░░╚═╝╚═════╝░░░░╚═╝░░░  ╚═╝░░░░░╚═╝╚═╝░░╚═╝╚═╝░░░░░\r\n\r\n███╗░░░███╗░█████╗░██╗░░██╗███████╗██████╗░\r\n████╗░████║██╔══██╗██║░██╔╝██╔════╝██╔══██╗\r\n██╔████╔██║███████║█████═╝░█████╗░░██████╔╝\r\n██║╚██╔╝██║██╔══██║██╔═██╗░██╔══╝░░██╔══██╗\r\n██║░╚═╝░██║██║░░██║██║░╚██╗███████╗██║░░██║\r\n╚═╝░░░░░╚═╝╚═╝░░╚═╝╚═╝░░╚═╝╚══════╝╚═╝░░╚═╝");
                    Console.WriteLine("");
                    constraintsList[0] = random.Next(0, 100); ;
                    constraintsList[1] = 100;
                    constraintsList[2] = random.Next(0, 100);
                }

                if (constraintsList[0] != 0)
                {
                    exit = true;
                }
            } while (exit == false);

            int divisionSize = 50;
            int sizeX = constraintsList[0];
            int sizeY = constraintsList[1];
            int percentBlend = constraintsList[2];
            int cornerVectorSizeX = (int)(((double)sizeX + divisionSize - 1) / divisionSize) + 1;
            int cornerVectorSizeY = (int)(((double)sizeY + divisionSize - 1) / divisionSize) + 1;

            Random rndSeed = new Random((int)seed); // make sure its the proper number here
            vectorAngles[,] cornerVectors = PerlinNoise.GenerateGradients(cornerVectorSizeX, cornerVectorSizeY, rndSeed);

            int octaves = 5;
            double persistence = 0.5;
            double lacunarity = 2.0;
            int baseDivisionSize = 32;

            double waterThreshold = 0.2;
            double[,] moisture = PerlinNoise.GeneratePerlinNoise(sizeX, sizeY, baseDivisionSize, octaves, persistence, lacunarity);
            double[,] elevation = PerlinNoise.GeneratePerlinNoise(sizeX, sizeY, baseDivisionSize, octaves, persistence, lacunarity);
            elevation = PerlinNoise.Island(sizeX, sizeY, percentBlend, elevation, waterThreshold);

            double treeRadius = 6.0;  
            List<PoissonDisk.Point> treePoints =
                PoissonDisk.GeneratePoissonPoints(sizeX, sizeY, treeRadius, 30, (int)seed);

            List<PoissonDisk.Point> finalTrees = new List<PoissonDisk.Point>();
            foreach (var p in treePoints)
            {
                if (elevation[(int)p.X, (int)p.Y] > 0.3)
                    finalTrees.Add(p);
            }


            Print(sizeX, sizeY, elevation, moisture);

            if (seedMenuChoice == 1 || seedMenuChoice == 2)
            {
                Console.WriteLine($"Seed: {seed}");
            }
                Console.WriteLine("Done");
            Console.ReadKey();
        }

        static int ValidateSeed(string text, string max)
        {
            do
            {
                Console.Clear();
                Console.WriteLine("\r\n███████╗░█████╗░███╗░░██╗████████╗░█████╗░░██████╗██╗░░░██╗  ███╗░░░███╗░█████╗░██████╗░\r\n██╔════╝██╔══██╗████╗░██║╚══██╔══╝██╔══██╗██╔════╝╚██╗░██╔╝  ████╗░████║██╔══██╗██╔══██╗\r\n█████╗░░███████║██╔██╗██║░░░██║░░░███████║╚█████╗░░╚████╔╝░  ██╔████╔██║███████║██████╔╝\r\n██╔══╝░░██╔══██║██║╚████║░░░██║░░░██╔══██║░╚═══██╗░░╚██╔╝░░  ██║╚██╔╝██║██╔══██║██╔═══╝░\r\n██║░░░░░██║░░██║██║░╚███║░░░██║░░░██║░░██║██████╔╝░░░██║░░░  ██║░╚═╝░██║██║░░██║██║░░░░░\r\n╚═╝░░░░░╚═╝░░╚═╝╚═╝░░╚══╝░░░╚═╝░░░╚═╝░░╚═╝╚═════╝░░░░╚═╝░░░  ╚═╝░░░░░╚═╝╚═╝░░╚═╝╚═╝░░░░░\r\n\r\n███╗░░░███╗░█████╗░██╗░░██╗███████╗██████╗░\r\n████╗░████║██╔══██╗██║░██╔╝██╔════╝██╔══██╗\r\n██╔████╔██║███████║█████═╝░█████╗░░██████╔╝\r\n██║╚██╔╝██║██╔══██║██╔═██╗░██╔══╝░░██╔══██╗\r\n██║░╚═╝░██║██║░░██║██║░╚██╗███████╗██║░░██║\r\n╚═╝░░░░░╚═╝╚═╝░░╚═╝╚═╝░░╚═╝╚══════╝╚═╝░░╚═╝");
                Console.WriteLine("");
                Console.Write(text);
                string input = Console.ReadLine();
                int seed = 0;
                int constraints = 0;

                if (input.Length == 20)
                {
                    int.TryParse(Console.ReadLine().Substring(10, 10), out constraints);
                    if (constraints >= 0 && constraints <= 2147483647)
                    {
                        string constraintsString = input.Substring(10, 10);
                        int[] constrainsList = new int[4];
                        constrainsList[0] = int.Parse(constraintsString.Substring(0, 3));
                        constrainsList[1] = int.Parse(constraintsString.Substring(3, 3));
                        constrainsList[2] = int.Parse(constraintsString.Substring(6, 2));
                        constrainsList[3] = int.Parse(constraintsString.Substring(8, 2));
                        ConstraintInitialise(constrainsList);

                        // confirm or return to menu or smth after inputting
                    }
                    else
                    {
                        Console.CursorTop = 16;
                        Console.Write($"{text}");
                        Console.CursorLeft = text.Length;
                        Console.CursorTop = 16;
                        Console.Write($"   The last 10 digits of the seed given do not correspond to correct constraints");
                        Console.CursorLeft = text.Length - 1;
                    }
                }
                if (input.Length == 10 || input.Length == 20)
                {
                    int.TryParse(Console.ReadLine().Substring(0, 10), out seed);
                    if (seed >= 0 && seed <= 2147483647)
                    {
                        // might need to clear and retype the title here ?
                        return int.Parse(input);
                        // confirm or return to menu or smth after inputting
                    }
                    else
                    {
                        Console.CursorTop = 16;
                        Console.Write($"{text}");
                        Console.CursorLeft = text.Length;
                        Console.CursorTop = 16;
                        Console.Write($"   The first 10 digits of the seed given are not a valid integer value");
                        Console.CursorLeft = text.Length - 1;
                    }
                }
                else
                {
                    Console.WriteLine("Length of the seed given is invalid");
                }
            } while (true);
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
            Console.WriteLine("  Fully random generation");

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
                    else 
                    {
                        return 3;
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