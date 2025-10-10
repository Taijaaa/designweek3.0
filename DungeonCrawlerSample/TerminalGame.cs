using DungeonCrawlerSample;
using System;
using System.Collections.Generic;

namespace MohawkTerminalGame
{
    public class TerminalGame
    {
        public enum GameMode { Story, Adventure }

        public GameMode activeGameMode = GameMode.Story;

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

        // Enemies
        List<Enemy> enemies = new();
        int frameCounter = 0;

        // Flags
        bool playerDied = false;
        bool storyScreenDrawn = false;
        bool firstStoryPlayed = false;

        // Weapon
        string chosenWeapon = "";

        // Map progression
        int currentMapIndex = 0; // 0 = intro story, 1 = map1, etc.

        //================== Enemy Classes ==================//
        abstract class Enemy
        {
            public int x, y;
            public ColoredText sprite;
            public int moveCooldown = 60;
            public int currentFrame = 0;
            public int hitCooldownFrames = 30;
            public int currentHitFrame = 0;
            public int health = 30;

            public Enemy(int x, int y, string symbol)
            {
                this.x = x;
                this.y = y;
                sprite = new ColoredText(symbol, ConsoleColor.White, ConsoleColor.Black);
            }

            public abstract void Move(int playerX, int playerY, TerminalGridWithColor map);
        }

        class Slime : Enemy
        {
            public Slime(int x, int y) : base(x, y, "ଳ") { }

            public override void Move(int playerX, int playerY, TerminalGridWithColor map)
            {
                int dx = Math.Sign(playerX - x);
                int dy = Math.Sign(playerY - y);

                if (dx != 0 && dy != 0)
                    if (Random.Bool()) dy = 0; else dx = 0;

                int newX = x + dx;
                int newY = y + dy;

                if (map.Get(newX, y).text == "░" && !(newX == playerX && y == playerY))
                    x = newX;

                if (map.Get(x, newY).text == "░" && !(x == playerX && newY == playerY))
                    y = newY;
            }
        }

        class Spider : Enemy
        {
            public Spider(int x, int y) : base(x, y, "✵")
            {
                moveCooldown = 60;
                health = 50;
            }

            public override void Move(int playerX, int playerY, TerminalGridWithColor map)
            {
                int dx = Math.Sign(playerX - x);
                int dy = Math.Sign(playerY - y);

                int jumpX = x + dx * 2;
                int jumpY = y + dy * 2;

                if (map.Get(jumpX, jumpY).text == "░" && !(jumpX == playerX && jumpY == playerY))
                {
                    x = jumpX;
                    y = jumpY;
                }
            }
        }

        class BabySpider : Enemy
        {
            public BabySpider(int x, int y) : base(x, y, "⋆")
            {
                moveCooldown = 120;
                health = 30;
            }

            public override void Move(int playerX, int playerY, TerminalGridWithColor map)
            {
                int dx = Math.Sign(playerX - x);
                int dy = Math.Sign(playerY - y);

                int jumpX = x + dx * 2;
                int jumpY = y + dy * 2;

                if (map.Get(jumpX, jumpY).text == "░" && !(jumpX == playerX && jumpY == playerY))
                {
                    x = jumpX;
                    y = jumpY;
                }
            }
        }

        class Scorpion : Enemy
        {
            public Scorpion(int x, int y) : base(x, y, "𓆌")
            {
                moveCooldown = 40;
                health = 35;
            }

            public override void Move(int playerX, int playerY, TerminalGridWithColor map)
            {
                int dx = Math.Sign(playerX - x);
                int dy = Math.Sign(playerY - y);

                if (dx != 0 && dy != 0)
                    if (Random.Bool()) dy = 0; else dx = 0;

                int newX = x + dx;
                int newY = y + dy;

                if (map.Get(newX, y).text == "░" && !(newX == playerX && y == playerY))
                    x = newX;

                if (map.Get(x, newY).text == "░" && !(x == playerX && newY == playerY))
                    y = newY;
            }
        }

        class FireBullet
        {
            public int x, y;
            int dx, dy;
            public bool isAlive = true;
            public ColoredText sprite = new("*", ConsoleColor.Red, ConsoleColor.Black);

            public FireBullet(int x, int y, int dx, int dy)
            {
                this.x = x;
                this.y = y;
                this.dx = dx;
                this.dy = dy;
            }

            public void Move(TerminalGridWithColor map)
            {
                int newX = x + dx;
                int newY = y + dy;

                if (newX < 0 || newX >= map.Width || newY < 0 || newY >= map.Height || map.Get(newX, newY).text != "░")
                {
                    isAlive = false;
                    return;
                }

                x = newX;
                y = newY;
            }
        }

        class Dragon : Enemy
        {
            public Dragon(int x, int y) : base(x, y, "𖤍")
            {
                moveCooldown = 0;
                health = 100;
                fireCooldown = 80;
            }

            public int fireCooldown;
            public int currentFireFrame = 0;
            public List<FireBullet> bullets = new();

            public override void Move(int playerX, int playerY, TerminalGridWithColor map)
            {
                currentFireFrame++;
                if (currentFireFrame >= fireCooldown)
                {
                    ShootFire(playerX, playerY);
                    currentFireFrame = 0;
                }

                for (int i = bullets.Count - 1; i >= 0; i--)
                {
                    FireBullet b = bullets[i];
                    b.Move(map);
                    if (!b.isAlive)
                        bullets.RemoveAt(i);
                }
            }

            void ShootFire(int targetX, int targetY)
            {
                int dx = Math.Sign(targetX - x);
                int dy = Math.Sign(targetY - y);
                if (dx == 0 && dy == 0) dx = 1;
                bullets.Add(new FireBullet(x, y, dx, dy));
            }
        }

        //================== Setup & Execute ==================//
        public void Setup()
        {
            Program.TerminalExecuteMode = TerminalExecuteMode.ExecuteTime;
            Program.TerminalInputMode = TerminalInputMode.EnableInputDisableReadLine;
            Program.TargetFPS = 60;

            Terminal.SetTitle("Dungeon Crawler Sample");
            Terminal.CursorVisible = false;

            map = new(39, 18, background);

            DisplayStoryScreen();
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

                foreach (var enemy in enemies)
                {
                    enemy.currentFrame++;
                    if (enemy.currentFrame >= enemy.moveCooldown)
                    {
                        ResetCell(enemy.x, enemy.y);
                        enemy.Move(playerX, playerY, map);
                        DrawCharacter(enemy.x, enemy.y, enemy.sprite);
                        enemy.currentFrame = 0;
                    }

                    if (enemy is Dragon dragon)
                    {
                        foreach (var bullet in dragon.bullets)
                        {
                            DrawCharacter(bullet.x, bullet.y, bullet.sprite);
                        }
                    }

                    if (enemy.currentHitFrame > 0)
                        enemy.currentHitFrame--;
                }

                CheckEnemyCollisions();

                if (enemies.Count == 0 || playerDied)
                {
                    activeGameMode = GameMode.Story;
                    storyScreenDrawn = false;
                }
            }
            else if (activeGameMode == GameMode.Story)
            {
                DisplayStoryScreen();
            }
        }

        //================== Player & Collision ==================//
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
                List<Enemy> enemiesToRemove = new();

                foreach (var enemy in enemies)
                {
                    if ((Math.Abs(enemy.x - playerX) == 1 && enemy.y == playerY) ||
                        (Math.Abs(enemy.y - playerY) == 1 && enemy.x == playerX))
                    {
                        enemy.health -= 5;
                        Console.SetCursorPosition(0, map.Height + 2);
                        Console.WriteLine($"Hit {enemy.sprite.text} at ({enemy.x},{enemy.y})! Health: {enemy.health}   ");

                        if (enemy.health <= 0)
                            enemiesToRemove.Add(enemy);
                    }
                }

                foreach (var dead in enemiesToRemove)
                {
                    ResetCell(dead.x, dead.y);
                    enemies.Remove(dead);
                }
            }
        }

        void CheckEnemyCollisions()
        {
            foreach (var enemy in enemies)
            {
                bool touching =
                    (enemy.x == playerX && Math.Abs(enemy.y - playerY) == 1) ||
                    (enemy.y == playerY && Math.Abs(enemy.x - playerX) == 1) ||
                    (enemy.x == playerX && enemy.y == playerY);

                if (touching && enemy.currentHitFrame == 0)
                {
                    playerHealth -= 2;
                    enemy.currentHitFrame = enemy.hitCooldownFrames;
                    Console.SetCursorPosition(0, map.Height + 1);
                    Console.WriteLine($"Player hit by {enemy.sprite.text}! Health: {playerHealth}   ");
                }

                if (enemy is Dragon dragon)
                {
                    foreach (var bullet in dragon.bullets)
                    {
                        if (bullet.x == playerX && bullet.y == playerY)
                        {
                            playerHealth -= 5;
                            bullet.isAlive = false;
                            Console.SetCursorPosition(0, map.Height + 1);
                            Console.WriteLine($"Player hit by fire! Health: {playerHealth}   ");
                        }
                    }
                }

                if (enemy.currentHitFrame > 0)
                    enemy.currentHitFrame--;
            }

            if (playerHealth <= 0 && !playerDied)
            {
                playerDied = true;
                activeGameMode = GameMode.Story;
                storyScreenDrawn = false;
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

        //================== Map Drawing ==================//
        void DrawMapForCurrentLevel()
        {
            if (activeGameMode != GameMode.Adventure) return;

            string mapText = currentMapIndex switch
            {
                1 => Maps.map1,
                2 => Maps.map2,
                3 => Maps.map3,
                4 => Maps.map4,
                _ => Maps.map1,
            };

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

        void InitializeEnemiesForCurrentLevel()
        {
            enemies.Clear();

            switch (currentMapIndex)
            {
                case 1:
                    enemies.Add(new Slime(18, 7));
                    break;
                case 2:
                    enemies.Add(new BabySpider(12, 4));
                    break;
                case 3:
                    enemies.Add(new Scorpion(22, 7));
                    break;
                case 4:
                    enemies.Add(new Dragon(15, 5));
                    break;
            }

            foreach (var enemy in enemies)
            {
                DrawCharacter(enemy.x, enemy.y, enemy.sprite);
                enemy.currentHitFrame = enemy.hitCooldownFrames;
            }
        }

        //================== Adventure Mode Switching ==================//
        void SwitchToAdventureMode()
        {
            activeGameMode = GameMode.Adventure;
            storyScreenDrawn = false;
            playerDied = false;

            // Clear the entire console once before drawing map
            Console.Clear();

            DrawMapForCurrentLevel();

            playerHealth = 100;

            switch (currentMapIndex)
            {
                case 1: playerX = 1; playerY = 8; break;
                case 2: playerX = 3; playerY = 10; break;
                case 3: playerX = 10; playerY = 7; break;
                case 4: playerX = 10; playerY = 5; break;
            }

            DrawCharacter(playerX, playerY, player);
            InitializeEnemiesForCurrentLevel();
        }


        //================== Story Screen ==================//
        void DisplayStoryScreen()
        {
            if (!storyScreenDrawn)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("──────────── STORY MODE ────────────\n");

                if (!firstStoryPlayed)
                {
                    Console.WriteLine(
@"This is your final test to prove yourself worthy to Omarious, the lead councilman of Angrulia. 

Many have tried before, but those who ventured out never returned. Your task is to track down Helsadona,

the banished witch who dwells deep within the cave at the edge of the local forest and bring back her heart.

You walk alongside a forest elf guiding you toward your destination. 

He speaks of the town’s resentment toward Helsadona, how many believe she is the cause of the havoc and chaos that 

plagues Angrulia. She was once a councilwoman of Angrulia, until

a falling out with Omarious led to her banishment. To this day no one knows what truly happened between them.

As you reach the crumbling cave entrance, the elf turns to you. “She is the most powerful being 

this town has ever seen. You will need something to defend yourself.” With a flick of his wrist, 

a golden glow appears before you. Within it float three weapons:

a scepter, a sword, and a spear. “Which one calls to you?”

[S] Scepter  [W] Sword  [P] Spear"
                    );

                    // Weapon choice handled per frame
                    storyScreenDrawn = true;
                }
                else if (playerDied)
                {
                    Console.WriteLine("You have fallen in battle... Press [Enter] to retry this map.");
                }
                else
                {

                    switch (currentMapIndex)
                    {
                        case 1:
                            {
                                Console.WriteLine(
                            $@"After vanquishing the horde of slimes, your {chosenWeapon} glows green.

It is magically infused with the slimes’ Plasma-Goo! You are now one step closer to gaining the power
needed to slay the dragon. You brush the grime off your clothes and scan the cave ahead. 

On the far side of the cavern, two more tunnel openings gape,
each leading deeper into the unknown. Which path will you take?

Type [L] for left or [R] for right:");

                                if (Input.IsKeyPressed(ConsoleKey.L))
                                {
                                    Console.Clear();
                                    Console.WriteLine($@"You enter the left tunnel and begin your descent. As you move deeper, your hands brush along the rough stone walls while your feet sink softly into a bed of moss. The darkness makes it nearly impossible to see. You raise your {chosenWeapon} and it emits a faint magical glow.
The passage opens into a wide chamber. The light dances across the rocks, revealing a faded mural. You step closer. Helsadona is depicted, surrounded by wildlife. Foxes and deer bow willingly at her feet, an owl perched upon her shoulder nestles into her neck.
You reach out to press your hand against the mural. A wave of warmth flows through you, calm and strange, stealing your breath for a moment. Goosebumps cover your skin as you stumble back, catching your heel on a small pile of healing mushrooms, unrooted from the ground. For a moment you hesitate and think, “Did Helsadona leave these behind?” You pick up a mushroom and eat it, rejuvenating your body. You come across another opening in the stone and continue your journey inside.
");

                                    // clear any leftover keypress before the pause
                                    while (Console.KeyAvailable) Console.ReadKey(true);

                                    Console.WriteLine("\nPress Enter twice to continue...");
                                    //  Console.ReadKey(true);
                                  if (  Input.IsKeyPressed(ConsoleKey.Enter))
                                        currentMapIndex = 2;
                                    SwitchToAdventureMode();
                                    return;
                                }
                                else if (Input.IsKeyPressed(ConsoleKey.R))
                                {
                                    Console.Clear();
                                    Console.WriteLine($@"You enter the right tunnel and begin your trek into the shadows. The sound of dripping water echoes like a ticking clock. Your footsteps crunch over brittle bones half-buried in the dirt. Your surroundings darken with each step, you raise your {chosenWeapon} and it emits a faint magical glow. As your eyes adjust to the light, carvings etched into the walls come into view. You wipe away the dirt and dust, and the images come to life.
They tell stories both twisted and cruel. Helsadona, cloaked in all black, stands over fallen creatures, their heads decapitated at her feet. In another carving, she drains the life from the forest itself, roots curling away from her touch. A third one captures your attention, a serpent coiled across the stone. Your finger follows its blood-stained scales from tail to head till you reach where you expect to see the serpent's face, but it is no serpent at all. It is Helsadona, jaw unhinged as she devours an innocent child.
A cold shiver races down your spine. You tear your gaze away from the gruesome depictions and continue your steps through the tunnel. You come across another opening in the stone and continue your journey inside.
");

                                    // clear any leftover keypress before the pause
                                    while (Console.KeyAvailable) Console.ReadKey(true);

                                    Console.WriteLine("\nPress Enter twice to continue...");
                                      Console.ReadKey(true);
                                    if (Input.IsKeyPressed(ConsoleKey.Enter))
                                        currentMapIndex = 2;
                                    SwitchToAdventureMode();
                                    return;
                                }

                                return; // wait for L or R
                            }


                            // If neither key pressed yet, wait for next frame
                            
                            


                        case 2:
                            Console.WriteLine(
                    @"The spider and her brood are vanquished. You notice strange markings on the walls, hinting at deeper secrets.

Press [Enter] to continue.");
                            Console.ReadKey(true);
                            currentMapIndex = 3;
                            break;

                        case 3:
                            Console.WriteLine(
                    @"Scorpions defeated, the desert ruins are eerily silent.

A distant roar echoes — something big awaits. Press [Enter] to continue.");
                            Console.ReadKey(true);
                            currentMapIndex = 4;
                            break;

                        case 4:
                            Console.WriteLine(
                    @"The dragon falls! Flames die down and the dungeon feels still.

Your journey reaches its climax. Press [Enter] to finish.");
                            Console.ReadKey(true);
                            // End of game
                            break;

                        default:
                            Console.WriteLine("Victory! Press [Enter] to continue to the next adventure.");
                            Console.ReadKey(true);
                            break;
                    }

                }

                Console.ResetColor();
            }

            // Weapon selection handling
            if (!firstStoryPlayed && chosenWeapon == "")
            {
                if (Input.IsKeyPressed(ConsoleKey.S)) chosenWeapon = "Scepter";
                if (Input.IsKeyPressed(ConsoleKey.W)) chosenWeapon = "Sword";
                if (Input.IsKeyPressed(ConsoleKey.P)) chosenWeapon = "Spear";

                if (chosenWeapon != "")
                {
                    Console.WriteLine(); // Start on new line after choice
                    Console.WriteLine(
$@"The {chosenWeapon} is drawn to your hands like a magnet, and grasping its handle makes you feel powerful.

The elf continues, “To defeat the dragon that guards the witch, you must first overcome the slimes, spiders,

and scorpions lurking within. Each foe you vanquish will infuse your weapon with their power. Good luck, and

remember, nothing is ever as it seems.” With that, the elf vanishes, leaving you alone at the 

cave’s entrance pondering. Taking a deep breath, you shake off any fear and step into the darkness. 

Press [Enter] to continue.");
                }
            }

            if (Input.IsKeyPressed(ConsoleKey.Enter) && (firstStoryPlayed || chosenWeapon != ""))
            {
                if (!firstStoryPlayed)
                {
                    firstStoryPlayed = true;
                    currentMapIndex = 1;
                    SwitchToAdventureMode();
                }
                else if (playerDied)
                {
                    playerDied = false;
                    SwitchToAdventureMode();
                }
                else if (currentMapIndex < 4)
                {
                    currentMapIndex++;
                    SwitchToAdventureMode();
                }
                else
                {
                    Console.Clear();
                    Console.WriteLine("Thanks for playing!");
                    Environment.Exit(0);
                }
            }
        }
    }
}
