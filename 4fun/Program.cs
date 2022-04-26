using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace _4fun
{
    class Program
    {
        static int ScreenWidth = 150;
        static int ScreenHeight = 40;
        static int MapHeight = 20;
        static int MapWidth = 32;
        static double Depth = 8;
        static double Fov = Math.PI / 3.5;

        static double _playerX = 3.0;
        static double _playerY = 3.0;
        static double _playerA;

        static StringBuilder Map = new StringBuilder();

        static async Task Main(string[] args)
        {
            ScreenWidth = Convert.ToInt32(args[0]);
            ScreenHeight = Convert.ToInt32(args[1]);
            Console.SetWindowSize(ScreenWidth, ScreenHeight);
            Console.SetBufferSize(ScreenWidth, ScreenHeight);
            Console.CursorVisible = false;

            InitMap();

            var screen = new char[ScreenWidth * ScreenHeight];

            DateTime dateTimeFrom = DateTime.Now;

            while (true)
            {
                double elapsedTime = 0.005;

                if (Console.KeyAvailable)
                {
                    InitMap();

                    ConsoleKey consoleKey = Console.ReadKey(true).Key;

                    switch (consoleKey)
                    {
                        case ConsoleKey.LeftArrow:
                            _playerA += 0.1;
                            break;
                        case ConsoleKey.RightArrow:
                            _playerA -= 0.1;
                            break;
                        case ConsoleKey.UpArrow:
                            {
                                _playerX += Math.Sin(_playerA) * 0.25;
                                _playerY += Math.Cos(_playerA) * 0.25;

                                if (Map[(int)_playerY * MapWidth + (int)_playerX] == '#')
                                {
                                    _playerX -= Math.Sin(_playerA) * 0.25;
                                    _playerY -= Math.Cos(_playerA) * 0.25;
                                }

                                break;
                            }

                        case ConsoleKey.DownArrow:
                            {
                                _playerX -= Math.Sin(_playerA) * 0.25;
                                _playerY -= Math.Cos(_playerA) * 0.25;

                                if (Map[(int)_playerY * MapWidth + (int)_playerX] == '#')
                                {
                                    _playerX += Math.Sin(_playerA) * 0.25;
                                    _playerY += Math.Cos(_playerA) * 0.25;
                                }

                                break;
                            }
                    }

                    if (consoleKey == ConsoleKey.Escape)
                    {
                        break;
                    }
                }

                var rayCastingResult = new List<Dictionary<int, char>>();
                for (int x = 0; x < ScreenWidth; x++)
                {
                    var x1 = x;
                    rayCastingResult.Add(CastRay(x1));
                }

                foreach (Dictionary<int, char> dictionary in rayCastingResult)
                {
                    foreach (var key in dictionary.Keys)
                    {
                        screen[key] = dictionary[key];
                    }
                }

                char[] stats = $"X: {_playerX}, Y: {_playerY}, A: {_playerA}".ToCharArray();
                stats.CopyTo(screen, 0);

                for (int x = 0; x < MapWidth; x++)
                {
                    for (int y = 0; y < MapHeight; y++)
                    {
                        screen[(y + 1) * ScreenWidth + x] = Map[y * MapWidth + x];
                    }
                }

                screen[(int)(_playerY + 1) * ScreenWidth + (int)_playerX] = 'P';

                Console.SetCursorPosition(0, 0);
                Console.Write(screen, 0, ScreenWidth * ScreenHeight);
            }
        }

        public static Dictionary<int, char> CastRay(int x)
        {
            var result = new Dictionary<int, char>();

            double rayAngle = (_playerA + Fov / 2) - x * Fov / ScreenWidth;

            double distanceToWall = 0;
            bool hitWall = false;
            bool isBound = false;
            double wallSize = 1;

            double rayY = Math.Cos(rayAngle);
            double rayX = Math.Sin(rayAngle);

            while (!hitWall && distanceToWall < Depth)
            {
                distanceToWall += 0.1;

                int testX = (int)(_playerX + rayX * distanceToWall);
                int testY = (int)(_playerY + rayY * distanceToWall);

                if (testX < 0 || testX >= Depth + _playerX || testY < 0 || testY >= Depth + _playerY)
                {
                    hitWall = true;
                    distanceToWall = Depth;
                }
                else
                {
                    char testCell = Map[testY * MapWidth + testX];

                    if (testCell == '#')
                    {
                        hitWall = true;

                        var boundsVectorsList = new List<(double X, double Y)>();

                        for (int tx = 0; tx < 2; tx++)
                        {
                            for (int ty = 0; ty < 2; ty++)
                            {
                                double vx = testX + tx - _playerX;
                                double vy = testY + ty - _playerY;

                                double vectorModule = Math.Sqrt(vx * vx + vy * vy);
                                double cosAngle = (rayX * vx / vectorModule) + (rayY * vy / vectorModule);
                                boundsVectorsList.Add((vectorModule, cosAngle));
                            }
                        }

                        boundsVectorsList = boundsVectorsList.OrderBy(v => v.X).ToList();

                        double boundAngle = 0.03 / distanceToWall;

                        if (Math.Acos(boundsVectorsList[0].Y) < boundAngle || Math.Acos(boundsVectorsList[1].Y) < boundAngle)
                        {
                            isBound = true;
                        }
                    }
                    else
                    {
                        Map[testY * MapWidth + testX] = '*';
                    }
                }
            }

            int ceiling = (int)(ScreenHeight / 2.0 - ScreenHeight * Fov / distanceToWall);
            int floor = ScreenHeight - ceiling;

            ceiling += (int)(ScreenHeight - ScreenHeight * wallSize);

            char wallShade;

            if (isBound)
                wallShade = ' ';
            else if (distanceToWall <= Depth / 4.0)
                wallShade = '\u2588';
            else if (distanceToWall < Depth / 3.0)
                wallShade = '\u2593';
            else if (distanceToWall < Depth / 2.0)
                wallShade = '\u2592';
            else if (distanceToWall < Depth)
                wallShade = '\u2591';
            else
                wallShade = ' ';

            for (int y = 0; y < ScreenHeight; y++)
            {
                if (y < ceiling)
                    result[y * ScreenWidth + x] = ' ';
                else if (y > ceiling && y <= floor)
                    result[y * ScreenWidth + x] = wallShade;
                else
                {
                    char floorShade;
                    double b = 1.0 - (y - ScreenHeight / 2.0) / (ScreenHeight / 2.0);

                    if (b < 0.25)
                        floorShade = '#';
                    else if (b < 0.5)
                        floorShade = 'x';
                    else if (b < 0.75)
                        floorShade = '-';
                    else if (b < 0.9)
                        floorShade = '.';
                    else
                        floorShade = ' ';

                    result[y * ScreenWidth + x] = floorShade;
                }
            }

            return result;
        }

        public static void InitMap()
        {
            Map.Clear();
            Map.Append("################################");
            Map.Append("#......#.....#.................#");
            Map.Append("#......#.....#.................#");
            Map.Append("#......#.....#.................#");
            Map.Append("#......#.....#.............#...#");
            Map.Append("#......#.....#.............#...#");
            Map.Append("#......#.....#.............#...#");
            Map.Append("##.....#...................#####");
            Map.Append("##.....#.......................#");
            Map.Append("##.....#.......................#");
            Map.Append("#####..######.......########...#");
            Map.Append("#.......#......................#");
            Map.Append("#...#####.........#............#");
            Map.Append("#...#.............#............#");
            Map.Append("#...#.............#............#");
            Map.Append("#...#.............#............#");
            Map.Append("#...#.....#########............#");
            Map.Append("#...###..###......#............#");
            Map.Append("#.................#............#");
            Map.Append("################################");
        }
    }

}
