namespace MohawkTerminalGame
{
    public class TerminalGame
    {
        public enum GameMode { Story, Adventure }

        public GameMode activeGameMode = GameMode.Story;

        // Place your variables here
        TerminalGridWithColor map;

        ColoredText background = new(@"░", ConsoleColor.White, ConsoleColor.Black);
        ColoredText player = new(@"𐀪", ConsoleColor.White, ConsoleColor.Black);
        bool inputChanged;
        int oldPlayerX;
        int oldPlayerY;
        int playerX = 5;
        int playerY = 0;

        /// Run once before Execute begins
        public void Setup()
        {
            // Run program at timed intervals.
            Program.TerminalExecuteMode = TerminalExecuteMode.ExecuteTime;
            Program.TerminalInputMode = TerminalInputMode.EnableInputDisableReadLine;
            Program.TargetFPS = 60;
            // Prepare some terminal settings
            Terminal.SetTitle("Dungeon Crawler Sample");
            Terminal.CursorVisible = false; // hide cursor

            // Set map to some values
            map = new(39, 18, background);



            // Clear window and draw map
            map.ClearWrite();
            // Draw player. x2 because my tileset is 2 columns wide.
            DrawCharacter(playerX, playerY, player);
        }

        // Execute() runs based on Program.TerminalExecuteMode (assign to it in Setup).
        //  ExecuteOnce: runs only once. Once Execute() is done, program closes.
        //  ExecuteLoop: runs in infinite loop. Next iteration starts at the top of Execute().
        //  ExecuteTime: runs at timed intervals (eg. "FPS"). Code tries to run at Program.TargetFPS.
        //               Code must finish within the alloted time frame for this to work well.
        public void Execute()
        {
            if (activeGameMode == GameMode.Story)
            {
                //Tell the story
            }
            else if (activeGameMode == GameMode.Adventure)
            {
                //Play adventure
            }

            // Move player
            CheckMovePlayer();

            // Naive approach, works but it's much but slower
            //map.Overwrite(0,0);
            //map.Poke(playerX * 2, playerY, player);

            // Only move player if needed
            if (inputChanged)
            {
                ResetCell(oldPlayerX, oldPlayerY);
                DrawCharacter(playerX, playerY, player);
                inputChanged = false;
            }


        }

        void CheckMovePlayer()
        {
            //
            inputChanged = false;
            oldPlayerX = playerX;
            oldPlayerY = playerY;

            if (Input.IsKeyPressed(ConsoleKey.RightArrow))
                playerX++;
            if (Input.IsKeyPressed(ConsoleKey.LeftArrow))
                playerX--;
            if (Input.IsKeyPressed(ConsoleKey.DownArrow))
                playerY++;
            if (Input.IsKeyPressed(ConsoleKey.UpArrow))
                playerY--;

            playerX = Math.Clamp(playerX, 0, map.Width - 1);
            playerY = Math.Clamp(playerY, 0, map.Height - 1);

            if (oldPlayerX != playerX || oldPlayerY != playerY)
                inputChanged = true;
        }

        void DrawCharacter(int x, int y, ColoredText character)
        {
            ColoredText mapTile = map.Get(x, y);
            // Copy BG color. This assumes emoji.
            player.bgColor = mapTile.bgColor;
            // Character (eg. player) and grid are 2-width characters
            map.Poke(x * 2, y, player);
        }

        void ResetCell(int x, int y)
        {
            ColoredText mapTile = map.Get(x, y);
            // Player and grid are 2-width characters
            map.Poke(x * 2, oldPlayerY, mapTile);
        }

        void DrawMap(string mapText, int mapX, int mapY)
        {
            for (int x = 0; x < mapX; x++)

            {

                for (int y = 0; y < mapY; y++)

                {

                }
            }
        }
    }
}
