﻿using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CrosswordApp
{
    /// <summary>
    ///     Логика взаимодействия для CrosswordSolvingPage.xaml
    /// </summary>
    public partial class CrosswordSolvingPage : Page
    {
        Crossword crossword;

        readonly Stopwatch stopwatch;

        public CrosswordSolvingPage(Crossword crossword)
        {
            InitializeComponent();

            this.crossword = crossword;

            FillGrid(24);

            CrosswordNameTextBlock.Text = this.crossword.name;

            foreach (var placement in crossword.placements)
                if (placement.isVertical)
                    DefinitionsVerticalTextBlock.Text +=
                        $"{placement.index} - {crossword.words[placement.wordIndex].definition}\n\n";
                else
                    DefinitionsHorizontalTextBlock.Text +=
                        $"{placement.index} - {crossword.words[placement.wordIndex].definition}\n\n";

            stopwatch = new Stopwatch();
            stopwatch.Start();
        }

        void FillGrid(int cellSize)
        {
            var size = crossword.Size;

            CrosswordGrid.Children.Clear();
            CrosswordGrid.RowDefinitions.Clear();
            for (var i = 0; i < size.y; ++i)
                CrosswordGrid.RowDefinitions.Add(new RowDefinition {Height = new GridLength(cellSize)});
            CrosswordGrid.ColumnDefinitions.Clear();
            for (var i = 0; i < size.x; ++i)
                CrosswordGrid.ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(cellSize)});

            var cells = new bool[size.x, size.y];
            foreach (var placement in crossword.placements)
            {
                for (var i = 0; i < placement.Width; ++i)
                for (var j = 0; j < placement.Height; ++j)
                {
                    (int x, int y) pos = (placement.x + i, placement.y + j);
                    if (cells[pos.x, pos.y])
                        continue;

                    var a = new TextBox
                    {
                        VerticalContentAlignment = VerticalAlignment.Center,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        Style = Resources["CrosswordLetterTextBox"] as Style,
                        Background = new SolidColorBrush(Color.FromRgb(44, 44, 44)),
                        BorderThickness = new Thickness(0.5),
                        BorderBrush = Brushes.White,
                        Foreground = Brushes.White,
                        FontWeight = FontWeights.SemiBold
                    };
                    a.TextChanged += (sender, args) =>
                    {
                        if (!(sender is TextBox tb)) return;
                        var selectionStart = tb.SelectionStart;

                        var formattedText = tb.Text.ToLower();
                        for (var k = 0; k < formattedText.Length;)
                        {
                            var ch = formattedText[k];
                            if (ch < 'а' || ch > 'я')
                            {
                                formattedText = formattedText.Remove(k, 1);
                                if (k < selectionStart)
                                    --selectionStart;
                            }
                            else
                            {
                                ++k;
                            }
                        }

                        if (formattedText != tb.Text)
                        {
                            tb.Text = formattedText;
                            tb.SelectionStart = selectionStart;
                        }
                    };
                    a.GotFocus += (sender, args) =>
                    {
                        if (!(sender is TextBox tb)) return;
                        tb.SelectAll();
                    };
                    a.PreviewMouseLeftButtonDown += (sender, args) =>
                    {
                        if (!(sender is TextBox tb)) return;
                        if (tb.IsKeyboardFocusWithin) return;
                        args.Handled = true;
                        tb.Focus();
                    };
                    a.PreviewKeyDown += (sender, args) =>
                    {
                        if (!(sender is TextBox tb)) return;
                        var x = (int) tb.GetValue(Grid.ColumnProperty);
                        var y = (int) tb.GetValue(Grid.RowProperty);
                        var x1 = x;
                        var y1 = y;
                        switch (args.Key)
                        {
                            case Key.Left:
                                x1--;
                                break;
                            case Key.Right:
                                x1++;
                                break;
                            case Key.Up:
                                y1--;
                                break;
                            case Key.Down:
                                y1++;
                                break;
                            case Key.Tab:
                                args.Handled = true;
                                return;
                            default:
                                return;
                        }

                        TextBox tb1 = null;
                        foreach (var t in CrosswordGrid.Children)
                        {
                            if (!(t is TextBox textBox)) continue;
                            var tx = (int) textBox.GetValue(Grid.ColumnProperty);
                            var ty = (int) textBox.GetValue(Grid.RowProperty);
                            if (tx != x1 || ty != y1) continue;
                            tb1 = textBox;
                            break;
                        }

                        args.Handled = true;
                        tb1?.Focus();
                    };
                    a.MaxLength = 1;

                    a.SetValue(Grid.RowProperty, pos.y);
                    a.SetValue(Grid.ColumnProperty, pos.x);
                    CrosswordGrid.Children.Add(a);

                    cells[pos.x, pos.y] = true;
                }

                var b = new TextBlock
                {
                    Text = placement.index.ToString(),
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(2, 2, 0, 0),
                    FontSize = 7,
                    IsHitTestVisible = false,
                    Foreground = Brushes.White
                };
                b.SetValue(Grid.RowProperty, placement.y);
                b.SetValue(Grid.ColumnProperty, placement.x);
                CrosswordGrid.Children.Add(b);
            }
        }

        void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var messageBoxResult = MessageBox.Show(
                "Вы уверены, что хотите вернуться в меню? Ваш прогресс в этом кроссворде не сохранится.",
                "Выход в меню",
                MessageBoxButton.YesNo);
            if (messageBoxResult != MessageBoxResult.Yes)
                return;

            (Parent as MainWindow).Content = new MainMenuPage();
        }

        void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            var messageBoxResult = MessageBox.Show(
                "Вы уверены, что хотите закончить прохождение кроссворда?",
                "Завершение прохождения",
                MessageBoxButton.YesNo);
            if (messageBoxResult != MessageBoxResult.Yes)
                return;

            stopwatch.Stop();
            (Parent as MainWindow).Content = new SolvingResultPage(crossword, GetEnteredLetters(), stopwatch.Elapsed);
        }

        char[,] GetEnteredLetters()
        {
            var size = crossword.Size;

            var cells = new char[size.x, size.y];
            foreach (var letterTextBox in CrosswordGrid.Children)
            {
                if (!(letterTextBox is TextBox tb))
                    continue;
                var x = (int) tb.GetValue(Grid.ColumnProperty);
                var y = (int) tb.GetValue(Grid.RowProperty);
                if (tb.Text.Length < 1)
                    continue;
                cells[x, y] = Correct(tb.Text.ToLower()[0]);
            }

            return cells;
        }

        char Correct(char initial)
        {
            return initial == 'ё' ? 'е' : initial;
        }
    }
}