using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Buses
{
    public class PathAnalysis : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
        
        //Данные из файла
        //Кол-во автобусов
        int Buses;
        //Времена начала движения автобусов по маршрутам
        List<DateTime> TimeStarts;
        //Цены в автобусах
        List<int> Prices;
        //Список всех переходов между остановками
        List<Dictionary<int, int>> Route;
        //Список суммы всех переходов в маршруте
        List<int> WaysAllTime;
        //Список времени между остановками в маршруте
        List<List<int>> WaysTime;
        //Список остановок в маршрутах
        List<List<int>> Ways;

        private string m_Path = "";
        private string m_CheapPath = "";
        private string m_FastPath = "";
        private int m_Stops = 0;
        private List<int> m_StopsList;
        
        /// <summary>
        /// Путь к файлу
        /// </summary>
        public string Path
        {
            get { return m_Path; }
            set
            {
                m_Path = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Информация о дешевом пути
        /// </summary>
        public string CheapPath
        {
            get { return m_CheapPath; }
            set
            {
                m_CheapPath = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Информация о быстром пути
        /// </summary>
        public string FastPath
        {
            get { return m_FastPath; }
            set
            {
                m_FastPath = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Кол-во остановок
        /// </summary>
        public int Stops
        {
            get { return m_Stops; }
            set
            {
                m_Stops = value;
                OnPropertyChanged();
            }
        }
        
        /// <summary>
        /// Список всех остановок
        /// </summary>
        public List<int> StopsList
        {
            get { return m_StopsList; }
            set
            {
                m_StopsList = value;
                OnPropertyChanged();
            }
        }


        public PathAnalysis(string path)
        {
            Path = path;
            OpenFile();
        }

        /// <summary>
        /// Парсинг файла
        /// </summary>
        private void OpenFile()
        {
            var file = File.ReadAllLines(Path);

            Buses = Convert.ToInt32(file[0]);
            Stops = Convert.ToInt32(file[1]);
            StopsList = Enumerable.Range(1, Stops).ToList();

            TimeStarts = new List<DateTime>();
            foreach (var price in file[2].Split(" "))
            {
                TimeStarts.Add(Convert.ToDateTime(price));
            }

            Prices = new List<int>();
            foreach (var price in file[3].Split(" "))
            {
                Prices.Add(Convert.ToInt32(price));
            }

            Route = new List<Dictionary<int, int>>();
            Ways = new List<List<int>>();
            WaysTime = new List<List<int>>();
            WaysAllTime = new List<int>();

            for (int i = 4; i < file.Length; i++)
            {
                Route.Add(new Dictionary<int, int>());
                Ways.Add(new List<int>());
                WaysTime.Add(new List<int>());
                WaysAllTime.Add(0);

                var route1 = file[i].Split(" ");
                int count = Convert.ToInt32(route1[0]);

                for (int j = 0; j < count; j++)
                {
                    Route[i - 4].Add(Convert.ToInt32(route1[1 + j]), Convert.ToInt32(route1[1 + j + count]));
                    Ways[i - 4].Add(Convert.ToInt32(route1[1 + j]));
                    WaysTime[i - 4].Add(Convert.ToInt32(route1[1 + j + count]));
                    WaysAllTime[i - 4] += Convert.ToInt32(route1[1 + j + count]);
                }
            }
        }

        /// <summary>
        /// Поиск в дереве нужного узла
        /// </summary>
        /// <param name="startStop"></param>
        /// <param name="end"></param>
        /// <param name="time"></param>
        /// <param name="isFaster"></param>
        /// <returns>isFaster ? Fastest : Cheapest</returns>
        public Node TreeSearch(int startStop, int end, DateTime time, bool isFaster)
        {
            Node start = new Node(startStop, -1, 0);
            start.TimeNow = time;

            Node Fastest = null;
            Node Cheapest = null;

            var allNodes = new Stack<Node>();
            allNodes.Push(start);
            
            //Проходим по дереву
            while (allNodes.Count > 0)
            {
                var node = allNodes.Pop();

                for (int i = 0; i < Ways.Count; i++)
                {
                    var way = Ways[i];

                    var index = way.IndexOf(node.Name);

                    //Если такая остановка есть в этом маршруте,
                    //то добавить следующую остановку у данного автобуса
                    if (index != -1)
                    {
                        int nextIndex = index + 1 == way.Count ? way[0] : way[index + 1];

                        //Время надо переопределить
                        var next = new Node(nextIndex, i);

                        if (node.AddChild(next))
                        {
                            //Если путь поменяется, то надо совершить пересадку
                            if (node.Way != next.Way)
                            {
                                //Автобусы и их маршруты тоже с 1 считаются
                                next.Transfers.Add(node.Name, next.Way + 1);

                                //Плата за новый автобус
                                next.Price += Prices[next.Way];

                                //Тут высчитывается ожидание автобуса
                                //Учитывается цикличность его движения
                                //И дополнительно время с начала цикла до подъезда к остановке
                                //Плюс еще можно не попасть в цикл, тогда надо ждать следующий
                                var awaiting = TimeStarts[i];
                                var startMinute = awaiting.Hour * 60 + awaiting.Minute;
                                var nowMinute = node.TimeNow.Hour * 60 + node.TimeNow.Minute;

                                //Ожидание рейса (автобус даже не начал свою работу)
                                if (startMinute > nowMinute)
                                {
                                    nowMinute = startMinute;
                                }

                                var repeats = (nowMinute - startMinute) / WaysAllTime[i];

                                var busTime = startMinute + WaysAllTime[i] * repeats;

                                var timeOnWayIteration = 0;
                                var timeBusArrives = 0;

                                foreach (var elem in Route[i])
                                {            
                                    //Время автобуса приезда к следующей остановке
                                    if (elem.Key == nextIndex)
                                    {
                                        if (busTime + timeBusArrives < nowMinute)
                                        {
                                            //Значит в этот интервал автобуса не попадаем
                                            //Надо ждать следующего цикла (прохода с начала маршрута)
                                            timeOnWayIteration += WaysAllTime[i];
                                        }

                                        break;
                                    }

                                    //Время прибытия автобуса на текущую остановку
                                    if (elem.Key == index + 1)
                                    {
                                        timeBusArrives = timeOnWayIteration;
                                        //Затем прибавляется время от текущей к следующей
                                    }

                                    timeOnWayIteration += elem.Value;
                                }

                                busTime += timeOnWayIteration;

                                //После 24 часов все прекращается
                                if (busTime / 60 >= 24)
                                {
                                    continue;
                                }

                                next.TimeNow = new DateTime(next.TimeNow.Year, next.TimeNow.Month, next.TimeNow.Day,
                                    busTime / 60, busTime % 60, 0);
                            }
                            else
                            {
                                //Тут добавляется время до следующей остановки без пересадки 

                                //Циклический переход на начало пути
                                int prev = Route[i].Last().Value;

                                foreach (var elem in Route[i])
                                {
                                    if (elem.Key == nextIndex)
                                    {
                                        break;
                                    }

                                    prev = elem.Value;
                                }

                                var newMinutes = next.TimeNow.Minute + prev;
                                var newHour = next.TimeNow.Hour + newMinutes / 60;

                                //После 24 часов все прекращается
                                if (newHour >= 24)
                                {
                                    continue;
                                }

                                next.TimeNow = new DateTime(next.TimeNow.Year, next.TimeNow.Month, next.TimeNow.Day,
                                    newHour, newMinutes % 60, 0);
                            }

                            allNodes.Push(next);

                            //Самый дешевый
                            if (!isFaster && end == next.Name && (Cheapest == null || Cheapest?.Price > next.Price))
                            {
                                Cheapest = next;
                            }

                            //Самый быстрый
                            if (isFaster && end == next.Name && (Fastest == null || Fastest?.TimeNow > next.TimeNow))
                            {
                                Fastest = next;                                
                            }
                        }
                    }
                }
            }

            return isFaster ? Fastest : Cheapest;
        }

        /// <summary>
        /// Самый дешевый путь
        /// </summary>
        public void FindCheap(int startStop, int end, DateTime time)
        {
            if (startStop == end)
            {
                CheapPath = "Ехать никуда не нужно";
                return;
            }

            var node = TreeSearch(startStop, end, time, false);

            if (node == null)
            {
                CheapPath = "Невозможно проложить маршрут";
            }
            else
            {
                CheapPath = node.ToString();
            }
        }

        /// <summary>
        /// Самый быстрый путь
        /// </summary>
        public void FindFast(int startStop, int end, DateTime time)
        {
            if (startStop == end)
            {
                FastPath = "Ехать никуда не нужно";
                return;
            }

            var node = TreeSearch(startStop, end, time, true);

            if (node == null)
            {
                FastPath = "Невозможно проложить маршрут";
            }
            else
            {
                FastPath = node.ToString();
            }
        }
    }
}
