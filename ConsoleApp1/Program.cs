using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GeneticSearch
{
    class Program
    {
        struct GeneticData
        {
            public string protein;      // название белка
            public string organism;     // название организма
            public string amino_acids;  // цепочка аминокислот (декодированная)
        }

        // Декодирование RLE: "3Q" -> "QQQ"
        static string RLDecoding(string amino_acids)
        {
            StringBuilder decoded = new StringBuilder();

            for (int i = 0; i < amino_acids.Length; i++)
            {
                char ch = amino_acids[i];

                if (char.IsDigit(ch))
                {
                    int count = ch - '0';

                    if (i + 1 < amino_acids.Length)
                    {
                        char letter = amino_acids[i + 1];

                        for (int j = 0; j < count; j++)
                            decoded.Append(letter);

                        i++; // пропускаем букву, которую уже добавили
                    }
                }
                else
                {
                    decoded.Append(ch);
                }
            }

            return decoded.ToString();
        }

        // RLE-кодирование (может пригодиться, хотя в выводе не используется)
        static string RLEncoding(string amino_acids)
        {
            StringBuilder encoded = new StringBuilder();
            int i = 0;

            while (i < amino_acids.Length)
            {
                char ch = amino_acids[i];
                int count = 1;

                while (i + count < amino_acids.Length && amino_acids[i + count] == ch)
                    count++;

                if (count > 2)
                    encoded.Append(count).Append(ch);
                else
                    for (int j = 0; j < count; j++)
                        encoded.Append(ch);

                i += count;
            }

            return encoded.ToString();
        }

        // Чтение sequences.txt и декодирование аминокислот
        static List<GeneticData> ReadData(string filename)
        {
            var data = new List<GeneticData>();

            using (var reader = new StreamReader(filename))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    string[] parts = line.Split('\t');
                    if (parts.Length < 3) continue;

                    GeneticData gd;
                    gd.protein = parts[0];
                    gd.organism = parts[1];
                    gd.amino_acids = RLDecoding(parts[2]);

                    data.Add(gd);
                }
            }

            return data;
        }

        // Обработка команд и запись результата в выходной файл
        static void ProcessCommands(string commandsFile, string outputFile, List<GeneticData> data)
        {
            using (var writer = new StreamWriter(outputFile))
            {
                writer.WriteLine("Zhegzdrin Maksim");
                writer.WriteLine("Genetic Searching");

                string separator = new string('-', 74);
                writer.WriteLine(separator);

                int counter = 1;

                using (var reader = new StreamReader(commandsFile))
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        string[] parts = line.Split('\t');
                        string operation = parts[0];
                        string param1 = parts.Length > 1 ? parts[1] : "";
                        string param2 = parts.Length > 2 ? parts[2] : "";

                        if (operation == "search")
                        {
                            string decodedSearch = RLDecoding(param1);

                            writer.Write($"{counter:D3}   {operation}   {decodedSearch}");
                            writer.WriteLine();

                            DoSearch(writer, data, decodedSearch);
                        }
                        else if (operation == "diff")
                        {
                            writer.Write($"{counter:D3}   {operation}   {param1}   {param2}");
                            writer.WriteLine();

                            DoDiff(writer, data, param1, param2);
                        }
                        else if (operation == "mode")
                        {
                            writer.Write($"{counter:D3}   {operation}   {param1}");
                            writer.WriteLine();

                            DoMode(writer, data, param1);
                        }
                        else
                        {
                            writer.Write($"{counter:D3}   {operation}   {param1}");
                            if (parts.Length > 2)
                                writer.Write($"   {param2}");
                            writer.WriteLine();
                        }

                        writer.WriteLine(separator);
                        counter++;
                    }
                }
            }
        }

        // Операция search
        static void DoSearch(StreamWriter writer, List<GeneticData> data, string searchSeq)
        {
            var matches = data.Where(p => p.amino_acids.Contains(searchSeq)).ToList();

            writer.WriteLine("organism                 protein");

            if (matches.Count == 0)
            {
                writer.WriteLine("NOT FOUND");
            }
            else
            {
                foreach (var m in matches)
                {
                    writer.WriteLine($"{m.organism,-25}{m.protein}");
                }
            }
        }

        // Операция diff
        static void DoDiff(StreamWriter writer, List<GeneticData> data, string name1, string name2)
        {
            var p1 = data.FirstOrDefault(p => p.protein == name1);
            var p2 = data.FirstOrDefault(p => p.protein == name2);

            List<string> missing = new List<string>();

            if (p1.protein == null) missing.Add(name1);
            if (p2.protein == null) missing.Add(name2);

            writer.WriteLine("amino-acids difference:");

            if (missing.Count > 0)
            {
                writer.WriteLine("MISSING: " + string.Join(", ", missing));
                return;
            }

            string s1 = p1.amino_acids;
            string s2 = p2.amino_acids;

            int minLen = Math.Min(s1.Length, s2.Length);
            int diff = 0;

            for (int i = 0; i < minLen; i++)
            {
                if (s1[i] != s2[i])
                    diff++;
            }

            diff += Math.Abs(s1.Length - s2.Length);

            writer.WriteLine(diff);
        }

        // Операция mode
        static void DoMode(StreamWriter writer, List<GeneticData> data, string name)
        {
            var protein = data.FirstOrDefault(p => p.protein == name);

            writer.WriteLine("amino-acid occurs:");

            if (protein.protein == null)
            {
                writer.WriteLine("MISSING: " + name);
                return;
            }

            var freq = new Dictionary<char, int>();

            foreach (char c in protein.amino_acids)
            {
                if (freq.ContainsKey(c))
                    freq[c]++;
                else
                    freq[c] = 1;
            }

            char bestChar = freq
                .OrderByDescending(kv => kv.Value)
                .ThenBy(kv => kv.Key)
                .First()
                .Key;

            int bestCount = freq[bestChar];

            writer.WriteLine($"{bestChar,-11}{bestCount}");
        }

        static void Main(string[] args)
        {
            string sequencesFile = "sequences.txt";
            string commandsFile = "commands.txt";
            string outputFile = "genedata.txt";

            // Можно передать имена файлов через аргументы командной строки:
            // GeneticSearch.exe sequences.1.txt commands.1.txt genedata.txt
            if (args.Length >= 1) sequencesFile = args[0];
            if (args.Length >= 2) commandsFile = args[1];
            if (args.Length >= 3) outputFile = args[2];

            List<GeneticData> data = ReadData(sequencesFile);
            ProcessCommands(commandsFile, outputFile, data);
        }
    }
}