using DungeonCrawlerSample;
using System;
using System.Collections.Generic;

namespace MohawkTerminalGame
{
    public class TerminalGame
    {
        public enum GameMode { Story, Adventure }

        public GameMode activeGameMode = GameMode.Story; // Start in Story mode

        // Map and player variables
        TerminalGridWithColor map;
        ColoredText background = new(@" ", ConsoleColor.White, ConsoleColor.Black);
        ColoredText player = new(@"𐀪", ConsoleColor.White, ConsoleColor.Black);

        int playerHealth = 100;
        int playerX = 1;
        int playerY = 8;
        int oldPlayerX;
        int oldPlayerY;
        bool inputChanged;

        // Slimes
        List<Slime> slimes = new();
        int frameCounter = 0;

        // Flags
        bool playerDied = false;           // Was Story mode triggered by death?
        bool storyScreenDrawn = false;     // Prevent flicker in story mode

        /// Simple slime class
        class Slime
        {
            public int x, y;
            public ColoredText sprite;
            public int moveCooldown = 60;
            public int currentFrame = 0;

            public int hitCooldownFrames = 30;
            public int currentHitFrame = 0;

            public int health = 30;

            public Slime(int x, int y)
            {
                this.x = x;
                this.y = y;
                sprite = new ColoredText("ଳ", ConsoleColor.White, ConsoleColor.Black);
            }
        }

        public void Setup()
        {
            Program.TerminalExecuteMode = TerminalExecuteMode.ExecuteTime;
            Program.TerminalInputMode = TerminalInputMode.EnableInputDisableReadLine;
            Program.TargetFPS = 60;

            Terminal.SetTitle("Dungeon Crawler Sample");
            Terminal.CursorVisible = false;

            map = new(39, 18, background);

            DrawMap();
            DrawCharacter(playerX, playerY, player);

            // Add slimes
            slimes.Add(new Slime(10, 3));
            slimes.Add(new Slime(18, 7));
            slimes.Add(new Slime(20, 4));
            slimes.Add(new Slime(31, 4));

            foreach (var slime in slimes)
                DrawCharacter(slime.x, slime.y, slime.sprite);
        }

        public void Execute()
        {
            if (activeGameMode == GameMode.Adventure)
            {
                frameCounter++;

                CheckMovePlayer();
                if (inputChanged)
                {
                    ResetCell(oldPlayerX, oldPlayerY);
                    DrawCharacter(playerX, playerY, player);
                    inputChanged = false;
                }

                PlayerAttack();

                foreach (var slime in slimes)
                {
                    slime.currentFrame++;
                    if (slime.currentFrame >= slime.moveCooldown)
                    {
                        MoveSlime(slime);
                        slime.currentFrame = 0;
                    }

                    if (slime.currentHitFrame > 0)
                        slime.currentHitFrame--;
                }

                CheckSlimeCollisions();

                // If all slimes dead and player alive, switch to story mode
                if (slimes.Count == 0 && !playerDied)
                {
                    playerDied = false;
                    SwitchToStoryMode();
                }
            }
            else if (activeGameMode == GameMode.Story)
            {
                DisplayStoryScreen();
            }
        }

        void CheckMovePlayer()
        {
            inputChanged = false;
            oldPlayerX = playerX;
            oldPlayerY = playerY;

            int newX = playerX;
            int newY = playerY;

            if (Input.IsKeyPressed(ConsoleKey.RightArrow)) newX++;
            if (Input.IsKeyPressed(ConsoleKey.LeftArrow)) newX--;
            if (Input.IsKeyPressed(ConsoleKey.DownArrow)) newY++;
            if (Input.IsKeyPressed(ConsoleKey.UpArrow)) newY--;

            newX = Math.Clamp(newX, 0, map.Width - 1);
            newY = Math.Clamp(newY, 0, map.Height - 1);

            if (IsWalkable(newX, newY))
            {
                playerX = newX;
                playerY = newY;
                if (oldPlayerX != playerX || oldPlayerY != playerY)
                    inputChanged = true;
            }
        }

        void PlayerAttack()
        {
            if (Input.IsKeyPressed(ConsoleKey.Spacebar))
            {
                List<Slime> slimesToRemove = new();

                foreach (var slime in slimes)
                {
                    if ((Math.Abs(slime.x - playerX) == 1 && slime.y == playerY) ||
                        (Math.Abs(slime.y - playerY) == 1 && slime.x == playerX))
                    {
                        slime.health -= 5;
                        Console.SetCursorPosition(0, map.Height + 2);
                        Console.WriteLine($"Hit slime at ({slime.x},{slime.y})! Health: {slime.health}   ");

                        if (slime.health <= 0)
                            slimesToRemove.Add(slime);
                    }
                }

                foreach (var dead in slimesToRemove)
                {
                    ResetCell(dead.x, dead.y);
                    slimes.Remove(dead);
                }
            }
        }

        void MoveSlime(Slime slime)
        {
            ResetCell(slime.x, slime.y);

            int dx = Math.Sign(playerX - slime.x);
            int dy = Math.Sign(playerY - slime.y);

            if (dx != 0 && dy != 0)
            {
                if (Random.Bool()) dy = 0;
                else dx = 0;
            }

            int newX = slime.x + dx;
            int newY = slime.y + dy;

            if (IsWalkable(newX, slime.y) && !(newX == playerX && slime.y == playerY))
                slime.x = newX;

            if (IsWalkable(slime.x, newY) && !(slime.x == playerX && newY == playerY))
                slime.y = newY;

            DrawCharacter(slime.x, slime.y, slime.sprite);
        }

        void CheckSlimeCollisions()
        {
            if (activeGameMode != GameMode.Adventure)
                return;

            foreach (var slime in slimes)
            {
                bool touching =
                    (Math.Abs(slime.x - playerX) == 1 && slime.y == playerY) ||
                    (Math.Abs(slime.y - playerY) == 1 && slime.x == playerX) ||
                    (slime.x == playerX && slime.y == playerY);

                if (touching && slime.currentHitFrame == 0)
                {
                    playerHealth -= 2;
                    slime.currentHitFrame = slime.hitCooldownFrames;
                    Console.SetCursorPosition(0, map.Height + 1);
                    Console.WriteLine($"Player hit by slime! Health: {playerHealth}   ");
                }

                if (slime.currentHitFrame > 0)
                    slime.currentHitFrame--;
            }

            if (playerHealth <= 0 && !playerDied)
            {
                playerDied = true;
                SwitchToStoryMode();
            }
        }

        void DrawCharacter(int x, int y, ColoredText character)
        {
            ColoredText mapTile = map.Get(x, y);
            character.bgColor = mapTile.bgColor;
            map.Poke(x, y, character);
        }

        void ResetCell(int x, int y)
        {
            ColoredText mapTile = map.Get(x, y);
            map.Poke(x, y, mapTile);
        }

        bool IsWalkable(int x, int y)
        {
            return map.Get(x, y).text == "░";
        }

        public void DrawMap()
        {
            string mapText = Maps.map1;
            string[] lines = mapText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            for (int y = 0; y < lines.Length; y++)
            {
                string line = lines[y];
                for (int x = 0; x < line.Length; x++)
                {
                    char c = line[x];
                    ConsoleColor fg = ConsoleColor.White;
                    ConsoleColor bg = ConsoleColor.Black;

                    switch (c)
                    {
                        case '█': fg = ConsoleColor.DarkGray; break;
                        case '▓': fg = ConsoleColor.Gray; break;
                        case '▄': fg = ConsoleColor.DarkYellow; break;
                        case '░': fg = ConsoleColor.DarkGray; break;
                        case '▀': fg = ConsoleColor.Yellow; break;
                        case ' ': fg = ConsoleColor.Black; break;
                    }

                    map.Poke(x, y, new ColoredText(c.ToString(), fg, bg));
                    map.Set(new ColoredText(c.ToString(), fg, bg), x, y);
                }
            }
            Console.ResetColor();
        }

        void SwitchToStoryMode()
        {
            activeGameMode = GameMode.Story;
            storyScreenDrawn = false;
        }

        void SwitchToAdventureMode()
        {
            activeGameMode = GameMode.Adventure;
            storyScreenDrawn = false;
            playerDied = false;

            Console.Clear();
            DrawMap();
            DrawCharacter(playerX, playerY, player);
            foreach (var slime in slimes)
                DrawCharacter(slime.x, slime.y, slime.sprite);
        }

        void DisplayStoryScreen()
        {
            if (!storyScreenDrawn)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("──────────── STORY MODE ────────────");
                Console.WriteLine();

                if (playerDied)
                {
                    Console.WriteLine("Your vision fades as the last blow lands...");
                    Console.WriteLine("The dungeon claims another soul.");
                    Console.WriteLine();
                    Console.WriteLine("Press [Enter] to reflect on your journey.");
                }
                else
                {
                    Console.WriteLine("Welcome to the dungeon. Press [Enter] to begin your adventure.");
                }

                Console.ResetColor();
                storyScreenDrawn = true;
            }

            if (Input.IsKeyPressed(ConsoleKey.Enter))
            {
                if (playerDied || slimes.Count == 0)
                {
                    Console.Clear();
                    Console.WriteLine("To be continued...");
                }
                else
                {
                    SwitchToAdventureMode();
                }
            }
        }
    }
}
