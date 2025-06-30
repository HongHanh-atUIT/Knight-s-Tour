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

            bool ok = await SolveKnightTour(startX, startY, 1, path);

            if (!ok)
            {
                MessageBox.Show("Không tìm thấy lời giải, xin chia buồn! Nhấn retry nếu muốn thử lại!", "Failed", MessageBoxButton.OK, MessageBoxImage.Information);
                ResetBoard();
                return;
            }

            MessageBox.Show($"Xin chúc mừng! Lời giải đã hoàn tất, Nhấn retry nếu muốn thử lại!.", "Successful", MessageBoxButton.OK, MessageBoxImage.Information);
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
            return CountNextValidMoves(x, y, board);
        }

        int CountNextValidMoves(int x, int y, int[,] b)
        {
            int count = 0;
            for (int i = 0; i < 8; i++)
            {
                int nx = x + dx[i];
                int ny = y + dy[i];
                if (nx >= 0 && nx < N && ny >= 0 && ny < N && b[nx, ny] == -1)
                    count++;
            }
            return count;
        }

        async Task<bool> SolveKnightTour(int x, int y, int step, List<(int, int)> path)
        {
            path.Add((x, y));
            board[x, y] = step - 1;

            var nextMoves = new List<(int x, int y, int onward)>();
            for (int d = 0; d < 8; d++)
            {
                int nx = x + dx[d];
                int ny = y + dy[d];
                if (nx >= 0 && nx < N && ny >= 0 && ny < N && board[nx, ny] == -1)
                {
                    int onward = CountNextValidMoves(nx, ny);
                    nextMoves.Add((nx, ny, onward));
                }
            }
            nextMoves.Sort((a, b) => a.onward.CompareTo(b.onward));

            (int x, int y)? nextMove = null; //cho phép null
            if (nextMoves.Count > 0)
                nextMove = (nextMoves[0].x, nextMoves[0].y);

            await Dispatcher.InvokeAsync(() => DrawBoard(board, x, y, nextMoves, nextMove));
            while (isPaused) await Task.Delay(100);
            await Task.Delay(500);

            if (step == N * N)
                return true;

            foreach (var move in nextMoves)
            {
                if (await SolveKnightTour(move.x, move.y, step + 1, path))
                    return true;
            }

            board[x, y] = -1;
            path.RemoveAt(path.Count - 1);

            await Dispatcher.InvokeAsync(() => DrawBoard(board, x, y));
            await Task.Delay(300);
            return false;
        }

        void DrawBoard(int[,] b, int knightX, int knightY, List<(int x, int y, int onward)> nextMoves = null, (int x, int y)? nextMove = null)
        {
            BoardGrid.Children.Clear();
            BoardGrid.RowDefinitions.Clear();
            BoardGrid.ColumnDefinitions.Clear();

            for (int i = 0; i < N; i++)
            {
                BoardGrid.RowDefinitions.Add(new RowDefinition());
                BoardGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }

            bool IsNextMove(int row, int col, out int onward)
            {
                onward = -1;
                if (nextMoves != null)
                {
                    foreach (var move in nextMoves)
                    {
                        if (move.x == row && move.y == col)
                        {
                            onward = move.onward;
                            return true;
                        }
                    }
                }
                return false;
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
                                Text = "\u265E",
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
                    }
                    else
                    {
                        int onward;
                        if (IsNextMove(i, j, out onward))
                        {
                            if (nextMove.HasValue && nextMove.Value.x == i && nextMove.Value.y == j)
                                cell.Background = Brushes.LightPink;
                            else
                                cell.Background = Brushes.LightBlue;

                            TextBlock hint = new TextBlock
                            {
                                Text = onward.ToString(),
                                FontSize = 14,
                                FontWeight = FontWeights.Bold,
                                Foreground = Brushes.DarkRed
                            };
                            content.Children.Add(hint);
                        }
                    }

                    cell.Child = content;
                    Grid.SetRow(cell, i);
                    Grid.SetColumn(cell, j);
                    BoardGrid.Children.Add(cell);
                }
            }
        }
    }
}
