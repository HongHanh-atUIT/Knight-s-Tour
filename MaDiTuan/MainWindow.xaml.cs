using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MaDiTuan
{
    public partial class MainWindow : Window
    {
        const int N = 8;
        int[,] board = new int[N, N];
        int[] dx = { 2, 1, -1, -2, -2, -1, 1, 2 };
        int[] dy = { 1, 2, 2, 1, -1, -2, -2, -1 };
        bool isPaused = false;


        public MainWindow()
        {
            InitializeComponent();

            for (int i = 0; i < N; i++)
            {
                StartXBox.Items.Add(i);
                StartYBox.Items.Add(i);
            }
            StartXBox.SelectedIndex = 0;
            StartYBox.SelectedIndex = 0;

            ResetBoard();
        }

        void ResetBoard()
        {
            for (int i = 0; i < N; i++)
                for (int j = 0; j < N; j++)
                    board[i, j] = -1;

            DrawBoard(board, -1, -1);

            SolveButton.IsEnabled = true;
            RetryButton.IsEnabled = false;
            StartXBox.IsEnabled = true;
            StartYBox.IsEnabled = true;
        }

        private void PauseResumeButton_Click(object sender, RoutedEventArgs e)
        {
            isPaused = !isPaused;
            PauseResumeButton.Content = isPaused ? "Tiếp tục" : "Tạm dừng";
        }

        private async void SolveButton_Click(object sender, RoutedEventArgs e)
        {
            int startX = StartXBox.SelectedIndex;
            int startY = StartYBox.SelectedIndex;

            board[startX, startY] = 0;

            SolveButton.IsEnabled = false;
            RetryButton.IsEnabled = false;
            StartXBox.IsEnabled = false;
            StartYBox.IsEnabled = false;

            var path = new List<(int, int)>();

            bool ok = await Task.Run(() => SolveKnightTour(startX, startY, 1, path));

            if (!ok)
            {
                MessageBox.Show("Không tìm thấy lời giải, xin chia buồn! Hãy thử lại nếu muốn nhé!", "Failed", MessageBoxButton.OK, MessageBoxImage.Information);
                ResetBoard();
                return;
            }

            await AnimatePath(path);
            MessageBox.Show($"Xin chúc mừng! Lời giải đã hoàn tất, hãy thử lại nếu muốn nhé!.", "Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            RetryButton.IsEnabled = true;
        }

        private void RetryButton_Click(object sender, RoutedEventArgs e)
        {
            ResetBoard();
            if (isPaused)
            {
                isPaused = false;
                PauseResumeButton.Content = "Tạm dừng";
            }
        }

        int CountNextValidMoves(int x, int y)
        {
            int count = 0;
            for (int i = 0; i < 8; i++)
            {
                int nx = x + dx[i];
                int ny = y + dy[i];
                if (nx >= 0 && nx < N && ny >= 0 && ny < N && board[nx, ny] == -1)
                    count++;
            }
            return count;
        }


        bool SolveKnightTour(int x, int y, int step, List<(int, int)> path)
        {
            path.Add((x, y));

            if (step == N * N)
                return true;

            var nextMoves = new List<(int nextX, int nextY, int onwardCount)>();

            for (int i = 0; i < 8; i++)
            {
                int newX = x + dx[i];
                int newY = y + dy[i];
                if (newX >= 0 && newX < N && newY >= 0 && newY < N && board[newX, newY] == -1)
                {
                    int onward = CountNextValidMoves(newX, newY);
                    nextMoves.Add((newX, newY, onward));
                }
            }

            nextMoves.Sort((a, b) => a.onwardCount.CompareTo(b.onwardCount));

            foreach (var move in nextMoves)
            {
                board[move.nextX, move.nextY] = step;
                if (SolveKnightTour(move.nextX, move.nextY, step + 1, path))
                    return true;
                board[move.nextX, move.nextY] = -1;
            }

            path.RemoveAt(path.Count - 1); // backtrack
            return false;
        }


        async Task AnimatePath(List<(int x, int y)> path)
        {
            for (int i = 0; i < path.Count; i++)
            {
                int[,] temp = new int[N, N];
                for (int x = 0; x < N; x++)
                    for (int y = 0; y < N; y++)
                        temp[x, y] = -1;

                for (int j = 0; j <= i; j++)
                {
                    var (x, y) = path[j];
                    temp[x, y] = j;
                }

                DrawBoard(temp, path[i].x, path[i].y);
                while (isPaused)
                {
                    await Task.Delay(100);
                }
                await Task.Delay(400);
            }
        }

        void DrawBoard(int[,] b, int knightX, int knightY)
        {
            BoardGrid.Children.Clear();
            BoardGrid.RowDefinitions.Clear();
            BoardGrid.ColumnDefinitions.Clear();

            for (int i = 0; i < N; i++)
            {
                BoardGrid.RowDefinitions.Add(new RowDefinition());
                BoardGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }

            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    Border cell = new Border
                    {
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(1),
                        Background = (i + j) % 2 == 0 ? Brushes.White : Brushes.LightGray
                    };

                    StackPanel content = new StackPanel
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    if (b[i, j] >= 0)
                    {
                        cell.Background = Brushes.LightGreen;

                        if (i == knightX && j == knightY)
                        {
                            TextBlock icon = new TextBlock
                            {
                                Text = "♞",
                                FontSize = 24,
                                Foreground = Brushes.DarkBlue
                            };
                            content.Children.Add(icon);
                        }

                        TextBlock num = new TextBlock
                        {
                            Text = b[i, j].ToString(),
                            FontSize = 14,
                            FontWeight = FontWeights.Bold
                        };
                        content.Children.Add(num);

                        cell.Child = content;
                    }

                    Grid.SetRow(cell, i);
                    Grid.SetColumn(cell, j);
                    BoardGrid.Children.Add(cell);
                }
            }
        }
    }
}
