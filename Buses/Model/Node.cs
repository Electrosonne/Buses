using System;
using System.Collections.Generic;
using System.Text;

namespace Buses
{
    public class Node
    {
        public Node(int name, int way, int price = 0)
        {
            Name = name;
            Way = way;
            Price = price;
            TimeNow = DateTime.Now;
            Children = new List<Node>();
            Path = new List<int>();
            Path.Add(Name);
            Transfers = new Dictionary<int, int>();
        }

        public int Name { get; private set; }

        public int Way { get; private set; }

        public DateTime TimeNow { get; set; }

        public int Price { get; set; }

        public List<Node> Children { get; private set; }

        public List<int> Path { get; private set; }

        public Dictionary<int, int> Transfers { get; set; }

        public bool AddChild(Node child)
        {
            foreach (var name in Path)
            {
                if (child.Name == name)
                {
                    return false;
                }
            }

            Children.Add(child);

            child.Path = new List<int>();
            child.Transfers = new Dictionary<int, int>();
            child.Price = Price;
            child.TimeNow = TimeNow;

            foreach (var path in Path)
            {
                child.Path.Add(path);
            }

            foreach (var transfer in Transfers)
            {
                child.Transfers.Add(transfer.Key, transfer.Value);
            }

            child.Path.Add(child.Name);

            return true;
        }

        

        public override string ToString()
        {
            string result = "Путь: ";

            for (int i = 0; i < Path.Count; i++)
            {
                result += Path[i];

                if (Transfers.ContainsKey(Path[i]))
                {
                    result += $" (сесть на {Transfers[Path[i]]} автобус) ";
                }

                if (i + 1 != Path.Count)
                {
                    result += "->";
                }
            }

            result += $" Цена {Price} Время прибытия {TimeNow.ToString("HH:mm")}";

            return result;
        }
    }
}
