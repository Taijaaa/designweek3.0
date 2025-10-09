using DungeonCrawlerSample;
using System;
using System.Collections.Generic;

namespace MohawkTerminalGame
{
    public class TerminalGame
    {
        public enum GameMode { Story, Adventure }

        public GameMode activeGameMode = GameMode.Adventure;

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

        /// Simple slime class
        class Slime
        {
            public int x, y;
            public ColoredText sprite;
            public int moveCooldown = 40; // frames between moves
            public int currentFrame = 0;

            // New: hit cooldown so player doesn't take damage every frame
            public int hitCooldownFrames = 30;
            public int currentHitFrame = 0;

            public int health = 30; // New: slime health

            public Slime(int x, int y)
            {
                this.x = x;
                this.y = y;
                sprite = new ColoredText("ଳ", ConsoleColor.White, ConsoleColor.Black);
            }
        }

        /// Run once before Execute begins
        public void Setup()
        {
            Program.TerminalExecuteMode = TerminalExecuteMode.ExecuteTime;
            Program.TerminalInputMode = TerminalInputMode.EnableInputDisableReadLine;
            Program.TargetFPS = 60;

            Terminal.SetTitle("Dungeon Crawler Sample");
            Terminal.CursorVisible = false;

            // Initialize map
            map = new(39, 18, background);

            // Draw map
            DrawMap();

            // Draw player
            DrawCharacter(playerX, playerY, player);

            // Add some slimes
            slimes.Add(new Slime(10, 3));
            slimes.Add(new Slime(12, 9));
          //  slimes.Add(new Slime(5, 10));
           // slimes.Add(new Slime(5, 10));

            // Draw initial slimes
            foreach (var slime in slimes)
                DrawCharacter(slime.x, slime.y, slime.sprite);
        }

        /// Called every frame
        public void Execute()
        {
            if (activeGameMode == GameMode.Adventure)
            {
                frameCounter++;

                // Move player
                CheckMovePlayer();

                if (inputChanged)
                {
                    ResetCell(oldPlayerX, oldPlayerY);
                    DrawCharacter(playerX, playerY, player);
                    inputChanged = false;
                }

                // Player attacks
                PlayerAttack();

                // Move slimes slowly
                foreach (var slime in slimes)
                {
                    slime.currentFrame++;
                    if (slime.currentFrame >= slime.moveCooldown)
                    {
                        MoveSlime(slime);
                        slime.currentFrame = 0;
                    }

                    // Update hit cooldown counter
                    if (slime.currentHitFrame > 0)
                        slime.currentHitFrame--;
                }

                // Check for collisions after all slimes have moved
                CheckSlimeCollisions();
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

        /// Player attack method (spacebar)
        void PlayerAttack()
        {
            if (Input.IsKeyPressed(ConsoleKey.Spacebar))
            {
                List<Slime> slimesToRemove = new();

                foreach (var slime in slimes)
                {
                    // Check if slime is adjacent (up, down, left, right)
                    if ((Math.Abs(slime.x - playerX) == 1 && slime.y == playerY) ||
                        (Math.Abs(slime.y - playerY) == 1 && slime.x == playerX))
                    {
                        slime.health -= 5; // Deal 5 damage
                        Console.SetCursorPosition(0, map.Height + 2);
                        Console.WriteLine($"Hit slime at ({slime.x},{slime.y})! Health: {slime.health}   ");

                        if (slime.health <= 0)
                            slimesToRemove.Add(slime);
                    }
                }

                // Remove dead slimes
                foreach (var dead in slimesToRemove)
                {
                    ResetCell(dead.x, dead.y);
                    slimes.Remove(dead);
                }
            }
        }

        /// Move a slime one step toward the player
        void MoveSlime(Slime slime)
        {
            // Restore the floor behind the slime
            ResetCell(slime.x, slime.y);

            int dx = Math.Sign(playerX - slime.x);
            int dy = Math.Sign(playerY - slime.y);

            // If both directions are possible, randomly choose one
            if (dx != 0 && dy != 0)
            {
                if (Random.Bool()) // 0 = move horizontally, 1 = move vertically
                    dy = 0;  // cancel vertical movement
                else
                    dx = 0;  // cancel horizontal movement
            }

            int newX = slime.x + dx;
            int newY = slime.y + dy;

            // Only move if walkable and not into player
            if (IsWalkable(newX, slime.y) && !(newX == playerX && slime.y == playerY))
                slime.x = newX;

            if (IsWalkable(slime.x, newY) && !(slime.x == playerX && newY == playerY))
                slime.y = newY;

            DrawCharacter(slime.x, slime.y, slime.sprite);
        }

        /// Check if any slime touches the player
        void CheckSlimeCollisions()
        {
            foreach (var slime in slimes)
            {
                if (slime.x == playerX && slime.y == playerY)
                {
                    if (slime.currentHitFrame == 0)
                    {
                        playerHealth -= 2;
                        slime.currentHitFrame = slime.hitCooldownFrames; // reset hit cooldown
                        Console.SetCursorPosition(0, map.Height + 1);
                        Console.WriteLine($"Player hit! Health: {playerHealth}  ");
                    }
                }
            }
        }

        void DrawCharacter(int x, int y, ColoredText character)
        {
            ColoredText mapTile = map.Get(x, y);
            character.bgColor = mapTile.bgColor; // copy background color
            map.Poke(x, y, character);
        }

        void ResetCell(int x, int y)
        {
            ColoredText mapTile = map.Get(x, y);
            map.Poke(x, y, mapTile);
        }

        bool IsWalkable(int x, int y)
        {
            ColoredText tile = map.Get(x, y);
            // Only floor tiles are walkable
            return tile.text == "░";
        }

        public void DrawMap()
        {
            string mapText = Maps.map2;
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
    }
}
