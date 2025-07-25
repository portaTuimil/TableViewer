using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using DynamicData;
using DynamicData.Aggregation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using static TableViewer.Views.MainWindow;

namespace TableViewer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Db db = new("Terms");
        Visualizer visualizer = new(db, tableGrid);
    }



    public class Visualizer
    {
        private Db Db { get; set; }
        private List<float>? ColumnLengths { get; set; }

        public Visualizer(Db db, DataGrid tableGrid)
        {
            Db = db;
            ColumnLengths = GetColumnWidths();
            PromptCategories(tableGrid);
        }

        private List<float>? GetColumnWidths()
        {
            if (Db.Values == null || Db.Values.Count == 0)
                return null;

            var columns = new List<float>();
            var rowLength = Db.Values.Count;   
            var colLength = Db.Values[0].Count;
            for(int i = 0; i< rowLength; i++)
            {
                float avLength = 0;
                for (int j = 0; j < Math.Min(20, colLength); j++)
                {
                    avLength += Db.Values[i][j].Length;
                }
                avLength = avLength / Math.Min(20, colLength);
                columns.Add(avLength);
            }

            //Debug.WriteLine((string.Join("\t", columns)));
            return columns;
        }

        private void PromptCategories(DataGrid tableGrid)
        {
            if (Db.Categories == null || Db.Categories.Count == 0 || Db.Values == null || Db.Values.Count == 0)
                return;

            var table = new DataTable();
            table.Columns.Add("Palabra");
            table.Columns.Add("Definición");

            int rowCount = Db.Values[0].Count;

            for (int i = 0; i < rowCount; i++)
            {
                var palabra = Db.Values[0][i];
                var definicion = Db.Values[1].Count > i ? Db.Values[1][i] : "";

                var row = table.NewRow();
                row["Palabra"] = palabra;
                row["Definición"] = definicion;
                table.Rows.Add(row);
            }

            Console.WriteLine($"[DEBUG] Filas: {table.Rows.Count}, Columnas: {table.Columns.Count}");
            tableGrid.ItemsSource = table.DefaultView;
        }
    }



    public class Db
    {
        public string Name { get; private set; }
        private readonly string? Text;
        private readonly List<List<List<string>>>? ContentList;
        public List<string>? Categories { get; private set; }
        public List<List<string>>? Values { get; private set; }


        public Db(string name)
        {
            Name = name;
            Text =GetContent(name + ".csv");
            if (Text != null)
            {
                ContentList = SeparateContent(Text);
                Categories = ContentList[0][0];
                Values = ContentList[1];

                //Log the content of the class:
                /*Debug.WriteLine("Categories:");
                foreach (var category in Categories)
                {
                    Debug.WriteLine($"- {category}");
                }

                Debug.WriteLine("\nValues:");
                for(var i = 0;i < Values[0].Count(); i++)
                {
                    Debug.WriteLine($"- {Values[0][i]}");
                    for (var j = 1;j < Values.Count(); j++)
                    {
                        Debug.WriteLine(Values[j][i].ToString());
                    }
                }*/
            }
        }


        private static string? GetContent(string file)
        {
            try
            {
                string baseDir = AppContext.BaseDirectory;
                string filePath = System.IO.Path.Combine(baseDir, "..", "..", "..", "..", "TableViewer", "Db", file);
                return File.ReadAllText(filePath);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading file: {ex.Message}");
                return null;
            }
        }


        private static List<List<List<string>>> SeparateContent(string Text)
        {
            using var reader = new StringReader(Text);
            string? categories = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(categories))
            {
                return [[new List<string>(), []]];
            }
            return [[categories.Split(',').Select(s => s.Trim()).ToList()], GetValues(reader, categories.Split(',').Count())];
        }


        private static List<List<string>> GetValues(StringReader reader, int entries)
        {
            var lines = new List<List<string>>();
            using (reader)
            {
                string? line;
                for (int i = 0; i < entries; i++) 
                {
                    lines.Add(new List<string>());
                }
                while ((line = reader.ReadLine()) != null)
                {
                    List<string> row = [];
                    if (line.Contains('"'))
                    {
                        string word = "";
                        bool inside = false;

                        foreach (char c in line)
                        {
                            if (c == '"')
                            {
                                inside = !inside;

                            }
                            else if (c == ',')
                            {
                                if (!inside)
                                {
                                    row.Add(word);
                                    word = "";
                                }
                                else
                                {
                                    word += ',';
                                }
                            }
                            else
                            {
                                word += c;
                            }
                        }
                        row.Add(word);
                        for(int i = 0; i< entries; i++)
                        {
                            lines[i].Add(row[i]);
                        }
                    }
                    else
                    {
                        row = line.Split(',').Select(c => c.Trim()).ToList();
                        for (int i = 0; i < entries; i++)
                        {
                            lines[i].Add(row[i]);
                        }
                    }
                }
            }
            return lines;
        } 
    }
}
