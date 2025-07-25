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

        public Visualizer(Db db, Grid tableGrid)
        {
            Db = db;
            ColumnLengths = GetColumnWidths();
            PromptCategories(tableGrid);
            PromptValues(tableGrid);
        }

        private List<float>? GetColumnWidths()
        {
            if (Db.Values == null || Db.Values.Count == 0)
                return null;

            var columns = new List<float>();
            var rowLength = Db.Values[0].Count;   
            var colLength = Db.Values.Count;
            for(int i = 0; i< rowLength; i++)
            {
                float avLength = 0;
                for (int j = 0; j < Math.Min(20, colLength); j++)
                {
                    avLength += Db.Values[j][i].Length;
                }
                avLength = avLength / Math.Min(20, colLength);
                columns.Add(avLength);
            }
            return columns;
        }

        private void PromptCategories(Grid tableGrid)
        {
            if (Db.Categories == null || Db.Categories.Count == 0 || Db.Values == null || Db.Values.Count == 0 || ColumnLengths == null)
                return;

            for (int i = 0; i < Db.Categories.Count; i++)
            {
                // Add column definition
                tableGrid.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = new GridLength(ColumnLengths[i]/ColumnLengths.Sum(), GridUnitType.Star)
                });

                // Create the TextBlock
                TextBlock textBlock = new TextBlock
                {
                    Text = Db.Categories[i],
                };

                // Set Grid position
                Grid.SetColumn(textBlock, i);

                // Add to Grid
                tableGrid.Children.Add(textBlock);
            }
        }
        private void PromptValues(Grid tableGrid)
        {
            if (Db.Values == null || Db.Values.Count == 0)
                return;
            for (int i = 0; i < Db.Values.Count; i++)
            {
                for (int j = 0; j < (Db.Values[i]).Count(); j++)
                {
                    Border border = new Border
                    {
                        BorderThickness = new Thickness(0, 1, 0, 0),
                        BorderBrush = Brushes.Gray
                    };
                    TextBlock textBlock = new TextBlock
                    {
                        Text = Db.Values[i][j],
                        TextWrapping = TextWrapping.Wrap,
                    };
                    border.Child = textBlock;
                    Grid.SetColumn(border, j);
                    Grid.SetRow(border, i + 1);
                    tableGrid.RowDefinitions.Add(new RowDefinition());
                    tableGrid.Children.Add(border);
                }
            }
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
            return [[categories.Split(',').Select(s => s.Trim()).ToList()], GetValues(reader)];
        }


        private static List<List<string>> GetValues(StringReader reader)
        {
            var lines = new List<List<string>>();
            using (reader)
            {
                string? line;
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
                        lines.Add(row);
                    }
                    else
                    {
                        row = line.Split(',').Select(c => c.Trim()).ToList();
                        lines.Add(row);
                    }
                }
            }
            return lines;
        } 
    }
}
