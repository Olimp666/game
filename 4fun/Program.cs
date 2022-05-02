using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
namespace _4fun
{
    class Program
    {
        static int ScreenWidth = 200;
        static int ScreenHeight = 50;
        static int MapHeight = 20;
        static int MapWidth = 32;
        static double Depth = 8;
        static double Fov = Math.PI / 3.5;

        static double _playerX = 1.5;
        static double _playerY = 1.5;
        static double _playerA = Math.PI / 2;

        static StringBuilder Map = new StringBuilder();
        static List<StringBuilder> MapStrings = new List<StringBuilder>();
        static List<(double, double, double)> Trajectory = new List<(double, double, double)>();
        static List<(double, double, double)> TrajectoryExpanded = new List<(double, double, double)>();

        static void Main(string[] args)
        {
            Console.SetWindowSize(ScreenWidth, ScreenHeight);
            Console.SetBufferSize(ScreenWidth, ScreenHeight);
            Console.CursorVisible = false;
            GenerateMap();
            ExpandTrajectory();
            InitMap();
            var screen = new char[ScreenWidth * ScreenHeight];
            bool manual = false;
            while (true)
            {
                InitMap();
                if (manual)
                {
                    if (Console.KeyAvailable)
                    {
                        ConsoleKey consoleKey = Console.ReadKey(true).Key;
                        if (consoleKey == ConsoleKey.LeftArrow)
                            _playerA += 0.1;
                        if (consoleKey == ConsoleKey.RightArrow)
                            _playerA -= 0.1;
                        if (consoleKey == ConsoleKey.UpArrow)
                        {
                            _playerX += Math.Sin(_playerA) * 0.25;
                            _playerY += Math.Cos(_playerA) * 0.25;
                            char nextmap = Map[(int)_playerY * MapWidth + (int)_playerX];
                            if (nextmap == '#' || nextmap == ' ')
                            {
                                _playerX -= Math.Sin(_playerA) * 0.25;
                                _playerY -= Math.Cos(_playerA) * 0.25;
                            }
                        }
                        if (consoleKey == ConsoleKey.DownArrow)
                        {
                            _playerX -= Math.Sin(_playerA) * 0.25;
                            _playerY -= Math.Cos(_playerA) * 0.25;
                            char nextmap = Map[(int)_playerY * MapWidth + (int)_playerX];
                            if (nextmap == '#' || nextmap == ' ')
                            {
                                _playerX += Math.Sin(_playerA) * 0.25;
                                _playerY += Math.Cos(_playerA) * 0.25;
                            }
                        }
                        if (consoleKey == ConsoleKey.Escape)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    if (TrajectoryExpanded.Count == 0)
                        Environment.Exit(0);
                    _playerX = 0.5 + TrajectoryExpanded[0].Item1;
                    _playerY = 0.5 + TrajectoryExpanded[0].Item2;
                    _playerA = TrajectoryExpanded[0].Item3;
                    TrajectoryExpanded.RemoveAt(0);
                    Thread.Sleep(10);
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

                char[] stats = $"X:{_playerX}, Y:{_playerY}, A:{_playerA}".ToCharArray();
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

                    if (testCell == '#' || testCell == ' ' || testCell == '0' || testCell == '1')
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
            if ((Map[(int)(_playerY + rayY * distanceToWall) * MapWidth + (int)(_playerX + rayX * distanceToWall)]) == '1')
                wallShade = '^';
            else if (isBound)
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
            foreach (var mapString in MapStrings)
            {
                Map.Append(mapString);
            }
        }
        public static void GenerateMap()
        {
            Map.Append("################################");
            Map.Append("################################");
            Map.Append("################################");
            Map.Append("################################");
            Map.Append("################################");
            Map.Append("################################");
            Map.Append("################################");
            Map.Append("################################");
            Map.Append("################################");
            Map.Append("################################");
            Map.Append("################################");
            Map.Append("################################");
            Map.Append("################################");
            Map.Append("################################");
            Map.Append("################################");
            Map.Append("################################");
            Map.Append("################################");
            Map.Append("################################");
            Map.Append("################################");
            Map.Append("################################");

            Random rand = new Random();

            int pointsAmount = rand.Next(MapWidth / 10, MapWidth / 5);
            List<(int, int)> points = new List<(int, int)>(pointsAmount);
            List<int> availableX = new List<int>(MapHeight - 5);
            List<int> availableY = new List<int>(MapWidth - 5);
            for (int i = 0; i < availableX.Capacity; i++)
            {
                availableX.Add(i + 3);
            }
            for (int i = 0; i < availableY.Capacity; i++)
            {
                availableY.Add(i + 3);
            }
            for (int i = 0; i < pointsAmount; i++)
            {
                int x = rand.Next(0, availableX.Count - 1);
                int y = rand.Next(0, availableY.Count - 1);
                points.Add((availableX[x], availableY[y]));

                availableX.RemoveAt(x);
                if (x < availableX.Count) availableX.RemoveAt(x);
                if (x - 1 >= 0) availableX.RemoveAt(x - 1);

                availableY.RemoveAt(y);
                if (y < availableY.Count) availableY.RemoveAt(y);
                if (y - 1 >= 0) availableY.RemoveAt(y - 1);
            }
            int entranceSite = rand.Next(1, 4);
            entranceSite = 1;
            int entranceVal;
            (int, int) entranceCoordinates = default;
            entranceVal = (entranceSite % 2 == 0) ? rand.Next(1, MapHeight - 2) : rand.Next(1, MapWidth - 2);
            if (entranceSite == 1)
            {
                entranceCoordinates = (0, entranceVal);
                points.Add((entranceCoordinates.Item1 + 1, entranceCoordinates.Item2));
                _playerA = 0;
            }
            if (entranceSite == 2)
            {
                entranceCoordinates = (entranceVal, 0);
                points.Add((entranceCoordinates.Item1, entranceCoordinates.Item2 + 1));
                _playerA = Math.PI / 2;
            }

            if (entranceSite == 3)
            {
                entranceCoordinates = (MapHeight - 1, entranceVal);
                points.Add((entranceCoordinates.Item1 - 1, entranceCoordinates.Item2));
                _playerA = Math.PI;
            }

            if (entranceSite == 4)
            {
                entranceCoordinates = (entranceVal, MapWidth - 1);
                points.Add((entranceCoordinates.Item1, entranceCoordinates.Item2 - 1));
                _playerA = 3 / 2 * Math.PI;

            }

            int exitSite = entranceSite + 2;
            exitSite = (exitSite > 4) ? exitSite % 4 : exitSite;
            int exitVal;
            (int, int) exitCoordinates = default;
            exitVal = (exitSite % 2 == 0) ? rand.Next(1, MapHeight - 2) : rand.Next(1, MapWidth - 2);
            if (exitSite == 1)
            {
                exitCoordinates = (0, exitVal);
                points.Add((exitCoordinates.Item1 + 1, exitCoordinates.Item2));
            }

            if (exitSite == 2)
            {
                exitCoordinates = (exitVal, 0);
                points.Add((exitCoordinates.Item1, exitCoordinates.Item2 + 1));
            }

            if (exitSite == 3)
            {
                exitCoordinates = (MapHeight - 1, exitVal);
                points.Add((exitCoordinates.Item1 - 1, exitCoordinates.Item2));
            }

            if (exitSite == 4)
            {
                exitCoordinates = (exitVal, MapWidth - 1);
                points.Add((exitCoordinates.Item1, exitCoordinates.Item2 - 1));
            }

            if (entranceSite == 1)
            {
                points.Sort(new Comparers.XcompareInc());
            }
            if (entranceSite == 2)
            {
                points.Sort(new Comparers.YcompareInc());
            }
            if (entranceSite == 3)
            {
                points.Sort(new Comparers.XcompareDec());
            }
            if (entranceSite == 4)
            {
                points.Sort(new Comparers.YcompareDec());
            }
            foreach (var point in points)
            {
                Map[point.Item1 * MapWidth + point.Item2] = 'P';
            }
            Map[entranceCoordinates.Item1 * MapWidth + entranceCoordinates.Item2] = '0';
            Map[exitCoordinates.Item1 * MapWidth + exitCoordinates.Item2] = '1';

            int curPoint = 1;
            int X = points[0].Item2;
            int Y = points[0].Item1;
            double A = _playerA;
            double defA = _playerA;
            for (int i = curPoint; i < points.Count; i++)
            {
                if (entranceSite == 1)
                {
                    while (Y != points[i].Item1)
                    {
                        Trajectory.Add((X, Y, A));
                        Map[Y * MapWidth + X] = 'T';
                        Y++;
                    }
                    Trajectory.Add((X, Y, A));
                    A += (X > points[i].Item2) ? -Math.PI / 2 : Math.PI / 2;
                    while (X != points[i].Item2)
                    {
                        Trajectory.Add((X, Y, A));
                        Map[Y * MapWidth + X] = 'T';
                        X = (X > points[i].Item2) ? X - 1 : X + 1;
                    }
                    Trajectory.Add((X, Y, A));
                    A = defA;
                }
                if (entranceSite == 2)
                {
                    while (X != points[i].Item2)
                    {
                        Trajectory.Add((X, Y, A));
                        Map[Y * MapWidth + X] = 'T';
                        X++;
                    }
                    Trajectory.Add((X, Y, A));
                    A += (Y > points[i].Item2) ? -Math.PI / 2 : Math.PI / 2;
                    while (Y != points[i].Item1)
                    {
                        Trajectory.Add((X, Y, A));
                        Map[Y * MapWidth + X] = 'T';
                        Y = (Y > points[i].Item1) ? Y - 1 : Y + 1;
                    }
                    Trajectory.Add((X, Y, A));
                    A = defA;
                }
                if (entranceSite == 3)
                {
                    while (Y != points[i].Item1)
                    {
                        Trajectory.Add((X, Y, A));
                        Map[Y * MapWidth + X] = 'T';
                        Y--;
                    }
                    Trajectory.Add((X, Y, A));
                    A += (X > points[i].Item2) ? Math.PI / 2 : -Math.PI / 2;
                    while (X != points[i].Item2)
                    {
                        Trajectory.Add((X, Y, A));
                        Map[Y * MapWidth + X] = 'T';
                        X = (X > points[i].Item2) ? X - 1 : X + 1;
                    }
                    Trajectory.Add((X, Y, A));
                    A = defA;
                }
                if (entranceSite == 4)
                {
                    while (X != points[i].Item2)
                    {
                        Trajectory.Add((X, Y, A));
                        Map[Y * MapWidth + X] = 'T';
                        X--;
                    }
                    Trajectory.Add((X, Y, A));
                    A += (Y > points[i].Item2) ? Math.PI / 2 : -Math.PI / 2;
                    while (Y != points[i].Item1)
                    {
                        Trajectory.Add((X, Y, A));
                        Map[Y * MapWidth + X] = 'T';
                        Y = (Y > points[i].Item1) ? Y - 1 : Y + 1;
                    }
                    Trajectory.Add((X, Y, A));
                    A = defA;
                }
            }
            Trajectory.Add((X, Y, A));

            Map[Y * MapWidth + X] = 'T';


            for (int i = 0; i < MapHeight; i++)
            {
                MapStrings.Add(new StringBuilder(Map.ToString().Substring(i * MapWidth, MapWidth)));
            }
            for (int i = 1; i < MapStrings.Count - 1; i++)
            {
                for (int j = 1; j < MapWidth - 1; j++)
                {
                    if ((MapStrings[i][j] == '#' && rand.Next(1, 3) > 1) || MapStrings[i][j] == 'T')
                    {
                        MapStrings[i][j] = '.';
                    }
                }
            }
        }
        public static void ExpandTrajectory()
        {
            double ChangingVal = default;
            for (int i = 0; i < Trajectory.Count - 1; i++)
            {
                if (Trajectory[i].Item1 != Trajectory[i + 1].Item1)
                {
                    if (Trajectory[i].Item1 > Trajectory[i + 1].Item1)
                    {
                        for (ChangingVal = Trajectory[i].Item1; ChangingVal > Trajectory[i + 1].Item1; ChangingVal -= 0.1)
                        {
                            TrajectoryExpanded.Add((ChangingVal, Trajectory[i].Item2, Trajectory[i].Item3));
                        }
                    }
                    else if (Trajectory[i].Item1 < Trajectory[i + 1].Item1)
                    {
                        for (ChangingVal = Trajectory[i].Item1; ChangingVal < Trajectory[i + 1].Item1; ChangingVal += 0.1)
                        {
                            TrajectoryExpanded.Add((ChangingVal, Trajectory[i].Item2, Trajectory[i].Item3));
                        }
                    }
                }
                else if (Trajectory[i].Item2 != Trajectory[i + 1].Item2)
                {
                    if (Trajectory[i].Item2 > Trajectory[i + 1].Item2)
                    {
                        for (ChangingVal = Trajectory[i].Item2; ChangingVal > Trajectory[i + 1].Item2; ChangingVal -= 0.1)
                        {
                            TrajectoryExpanded.Add((Trajectory[i].Item1, ChangingVal, Trajectory[i].Item3));
                        }
                    }
                    else if (Trajectory[i].Item2 < Trajectory[i + 1].Item2)
                    {
                        for (ChangingVal = Trajectory[i].Item2; ChangingVal < Trajectory[i + 1].Item2; ChangingVal += 0.1)
                        {
                            TrajectoryExpanded.Add((Trajectory[i].Item1, ChangingVal, Trajectory[i].Item3));
                        }
                    }
                }
                else if (Math.Abs(Trajectory[i].Item3 - Trajectory[i + 1].Item3) > 0.001)
                {
                    if (Trajectory[i].Item3 > Trajectory[i + 1].Item3)
                    {
                        for (ChangingVal = Trajectory[i].Item3; ChangingVal > Trajectory[i + 1].Item3; ChangingVal -= 0.1)
                        {
                            TrajectoryExpanded.Add((Trajectory[i].Item1, Trajectory[i].Item2, ChangingVal));
                        }
                    }
                    else if (Trajectory[i].Item3 < Trajectory[i + 1].Item3)
                    {
                        for (ChangingVal = Trajectory[i].Item3; ChangingVal < Trajectory[i + 1].Item3; ChangingVal += 0.1)
                        {
                            TrajectoryExpanded.Add((Trajectory[i].Item1, Trajectory[i].Item2, ChangingVal));
                        }
                    }
                }
            }
            TrajectoryExpanded.Add((Trajectory[Trajectory.Count - 1].Item1, Trajectory[Trajectory.Count - 1].Item2, Trajectory[Trajectory.Count - 1].Item3));
        }
    }


}
