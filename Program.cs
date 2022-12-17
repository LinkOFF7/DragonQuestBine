using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace bineText
{
    internal class Program
    {
        private static List<TableEntry> table = new List<TableEntry>();
        static void Main(string[] args)
        {
            if (args.Length == 0) return;

            string extension = Path.GetExtension(args[0]);
            if (extension == ".bine")
            {
                Extract(args[0]);
                return;
            }
            else if (extension == ".txt")
            {
                Build(args[0]);
                return;
            }
            else return;
        }

        public struct TableEntry
        {
            public int unk_value;
            public int t_ptr;
        }

        static void Build(string file)
        {
            string[] text = File.ReadAllLines(file);
            using(BinaryWriter writer = new BinaryWriter(File.Create(Path.GetFileNameWithoutExtension(file))))
            {
                writer.BaseStream.Position = text.Length * 2 * 4 + 12;
                foreach (var line in text)
                {
                    TableEntry entry = new TableEntry();
                    entry.unk_value = Int32.Parse(line.Substring(1, 6));
                    entry.t_ptr = (int)writer.BaseStream.Position;
                    table.Add(entry);
                    writer.Write(Encoding.UTF8.GetBytes(line.Substring(8).Replace("<lf>", "\n")));
                }
                writer.BaseStream.Position = 0;
                writer.Write(1);
                writer.Write(table.Count);
                foreach(var entry in table)
                    writer.Write(entry.unk_value);
                writer.Write(table.Count);
                foreach (var entry in table)
                    writer.Write(entry.t_ptr);
            }
        }
        static void Extract(string file)
        {
            using(BinaryReader reader = new BinaryReader(File.OpenRead(file)))
            {
                reader.BaseStream.Position += 4;
                int count1 = reader.ReadInt32();
                for(int i = 0; i < count1; i++)
                {
                    TableEntry entry = new TableEntry();
                    entry.unk_value = reader.ReadInt32();
                    table.Add(entry);
                }
                int count2 = reader.ReadInt32();
                if(count1 != count2)
                {
                    Console.WriteLine("Both table count not equal each other!\nPress any key to abort.");
                    Console.ReadKey();
                    return;
                }
                for (int i = 0; i < count2; i++)
                {
                    TableEntry entry = table[i];
                    entry.t_ptr = reader.ReadInt32();
                    table[i] = entry;
                }
                List<string> text = new List<string>();
                for (int i = 0; i < count2; i++)
                {
                    reader.BaseStream.Position = table[i].t_ptr;
                    if(i != count2 - 1)
                    {
                        int len = table[i+1].t_ptr - table[i].t_ptr;
                        text.Add($"[{table[i].unk_value.ToString("D6")}]" + Encoding.UTF8.GetString(reader.ReadBytes(len)).Replace("\n", "<lf>"));
                        continue;
                    }
                    else
                    {
                        int len = (int)(reader.BaseStream.Length - table[i].t_ptr);
                        text.Add($"[{table[i].unk_value.ToString("D6")}]" + Encoding.UTF8.GetString(reader.ReadBytes(len)).Replace("\n", "<lf>"));
                        break;
                    }
                }
                File.WriteAllLines(file + ".txt", text);
            }
        }
    }
}
