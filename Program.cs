using System.Text;
using Raylib_cs;

class Program
{
    public static void Main() => new Program().Run();

    public Random R = new Random();
    public double Bpm = 20;
    public Sound X;

    public double MinBpm = 20;
    public double MaxBpm = 280;
    public double BpmStep = 20;

    private const int BIG_FONT_SIZE = 128;
    private const int BIG_LINE_SPACING = 20;
    private const int NORMAL_FONT_SIZE = 32;
    private const int NORMAL_LINE_SPACING = 10;
    private const int SMALL_FONT_SIZE = 8;
    private int Width = 300;
    private int Height = 300;

    public int[] Scores;
    public int ScoreCount;
    public int ScoreIndex;

    public void RandomBpm()
    {
        var steps = (int)((MaxBpm - MinBpm) / BpmStep);
        var step = R.Next(steps + 1);
        Bpm = MinBpm + (step * BpmStep);
    }

    enum State
    {
        Guess,
        Correct,
        Incorrect,
        Settings,
    }

    public void Centered(string m, int index = 0, int total = 1, int font_size = BIG_FONT_SIZE, int line_spacing = BIG_LINE_SPACING)
    {
        var w = Raylib.GetRenderWidth();
        var h = Raylib.GetRenderHeight();
        var ratio = w / Width;
        font_size *= ratio;
        line_spacing *= ratio;
        var width = Raylib.MeasureText(m, font_size);
        var internalVoff = index * (font_size + line_spacing);
        var globalVoff = (total * font_size + (total - 1) * line_spacing) / 2;
        Raylib.DrawText(m, (w / 2) - (width / 2), (h / 2) - globalVoff + internalVoff, font_size, Color.White);
    }

    public void Run()
    {
        Raylib.InitWindow(Width, Height, "BPMer");
        Raylib.InitAudioDevice();
        Raylib.SetWindowFocused();

        X = Raylib.LoadSound("./x.mp3");
        StringBuilder sb = new StringBuilder();

        Scores = new int[10];

        var state = State.Settings;
        RandomBpm();

        Raylib.SetTargetFPS(60);
        double lastClick = 0;
        int settingsIndex = 0;

        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();

            if (Raylib.IsKeyPressed(KeyboardKey.F))
            {
                Raylib.ToggleBorderlessWindowed();
            }

            if (state != State.Settings && Raylib.IsKeyPressed(KeyboardKey.Tab))
            {
                settingsIndex = 0;
                state = State.Settings;
            }
            else
            {
                switch (state)
                {
                    case State.Settings:
                        Raylib.ClearBackground(Color.Purple);
                        var options = new[] { "STEP: ", "MAX: ", "MIN: ", "[GO]" };
                        var values = new[] { BpmStep, MaxBpm, MinBpm };

                        for (int i = 0; i < 4; i++)
                        {
                            sb.Clear();

                            if (i == 3 && settingsIndex == 3) sb.Append('<');
                            sb.Append(options[i]);
                            if (i == 3 && settingsIndex == 3) sb.Append('>');
                            if (i < 3)
                            {
                                if (i == settingsIndex) sb.Append('<');
                                sb.Append(values[i]);
                                if (i == settingsIndex) sb.Append('>');
                            }

                            Centered(sb.ToString(), i, 4, NORMAL_FONT_SIZE, NORMAL_LINE_SPACING);
                        }

                        int diff = 0;

                        if (Raylib.IsKeyPressed(KeyboardKey.Tab) || (settingsIndex == 3 &&
                                                                     (Raylib.IsKeyPressed(KeyboardKey.Enter) ||
                                                                      Raylib.IsKeyPressed(KeyboardKey.Space))))
                        {
                            sb.Clear();
                            RandomBpm();
                            state = State.Guess;
                        }
                        else if (Raylib.IsKeyPressed(KeyboardKey.Up))
                        {
                            settingsIndex = (4 + settingsIndex - 1) % 4;
                        }
                        else if (Raylib.IsKeyPressed(KeyboardKey.Down))
                        {
                            settingsIndex = (settingsIndex + 1) % 4;
                        }
                        else if (settingsIndex != 3)
                        {
                            if (Raylib.IsKeyPressed(KeyboardKey.Left))
                            {
                                diff -= 1;
                            }
                            else if (Raylib.IsKeyPressed(KeyboardKey.Right))
                            {
                                diff += 1;
                            }
                        }

                        if (diff != 0)
                        {
                            if (settingsIndex == 0)
                            {
                                BpmStep += diff;
                                if (BpmStep < 1) BpmStep = 1;
                            }
                            else if (settingsIndex == 1)
                            {
                                MaxBpm += BpmStep * diff;
                                if (MaxBpm < MinBpm) MaxBpm = MinBpm;
                                if (MaxBpm < BpmStep) MaxBpm = BpmStep;
                            }
                            else if (settingsIndex == 2)
                            {
                                MinBpm += BpmStep * diff;
                                if (MinBpm > MaxBpm) MinBpm = MaxBpm;
                                if (MinBpm < BpmStep) MinBpm = BpmStep;
                            }
                        }

                        break;
                    case State.Guess:
                        double rate = 60.0 / Bpm;
                        double dt = Raylib.GetTime() - lastClick;
                        if (dt >= rate)
                        {
                            Raylib.PlaySound(X);
                            lastClick = Raylib.GetTime();
                        }

                        Raylib.ClearBackground(Color.Blue);

                        int key;
                        do
                        {
                            if (Raylib.IsKeyPressed(KeyboardKey.Enter) && sb.Length > 0)
                            {
                                var guess = int.Parse(sb.ToString());
                                var score = (int)(Math.Abs(Bpm - guess));
                                state = score == 0 ? State.Correct : State.Incorrect;

                                Scores[ScoreIndex] = score;
                                if (ScoreCount < Scores.Length) ScoreCount++;
                                ScoreIndex = (ScoreIndex + 1) % Scores.Length;

                                sb.Clear();
                                break;
                            }

                            if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && sb.Length > 0)
                            {
                                sb.Remove(sb.Length - 1, 1);
                            }

                            key = Raylib.GetCharPressed();
                            if (key is >= '0' and <= '9' && sb.Length < 3)
                            {
                                sb.Append((char)key);
                            }

                        } while (key != 0);

                        Centered(sb.ToString());
                        break;
                    default:
                        var correct = state == State.Correct;
                        Raylib.ClearBackground(correct ? Color.Green : Color.Red);

                        var bpms = ((int)Bpm).ToString();
                        Centered(bpms);

                        if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.Space))
                        {
                            RandomBpm();
                            state = State.Guess;
                        }

                        break;
                }

                double avgScore = 0;
                for (int i = 0; i < ScoreCount; i++)
                {
                    avgScore += Scores[i];
                }

                if (ScoreCount > 0)
                    avgScore /= ScoreCount;

                Raylib.DrawText($"Avg error: {(int)avgScore} bpm", 2, 2, SMALL_FONT_SIZE, Color.LightGray);
            }

            Raylib.EndDrawing();
        }

        Raylib.CloseAudioDevice();
        Raylib.CloseWindow();
    }
}