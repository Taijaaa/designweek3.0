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
                moveCooldown = 90;
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
            public int pushCooldownFrames = 0; // frames until next push allowed

            public Scorpion(int x, int y) : base(x, y, "𓆌")
            {
                moveCooldown = 40;
                health = 60;
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

                if (pushCooldownFrames > 0) pushCooldownFrames--;
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
                fireCooldown = 95;
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

            Terminal.SetTitle("The Heart of Deception");
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

                // === Scorpion special push-back ===
                if (enemy is Scorpion scorpion && touching)
                {
                    // Reduce cooldown every frame
                    if (scorpion.pushCooldownFrames > 0)
                        scorpion.pushCooldownFrames--;

                    // Only trigger push if cooldown is ready
                    if (scorpion.pushCooldownFrames == 0)
                    {
                        playerHealth -= 10;

                        // Push player 2 tiles away from scorpion
                        int pushX = playerX + Math.Sign(playerX - scorpion.x) * 2;
                        int pushY = playerY + Math.Sign(playerY - scorpion.y) * 2;

                        pushX = Math.Clamp(pushX, 0, map.Width - 1);
                        pushY = Math.Clamp(pushY, 0, map.Height - 1);

                        if (IsWalkable(pushX, pushY))
                        {
                            ResetCell(playerX, playerY);
                            playerX = pushX;
                            playerY = pushY;
                            DrawCharacter(playerX, playerY, player);
                        }

                        scorpion.pushCooldownFrames = 300; // 2s cooldown
                        scorpion.currentHitFrame = scorpion.hitCooldownFrames;

                        Console.SetCursorPosition(0, map.Height + 1);
                        Console.WriteLine($"Player hit and pushed by {scorpion.sprite.text}! Health: {playerHealth}   ");
                    }
                }
                else if (touching && enemy.currentHitFrame == 0)
                {
                    // === Default melee collision for all other enemies ===
                    playerHealth -= 2;
                    enemy.currentHitFrame = enemy.hitCooldownFrames;
                    Console.SetCursorPosition(0, map.Height + 1);
                    Console.WriteLine($"Player hit by {enemy.sprite.text}! Health: {playerHealth}   ");
                }

                // === Dragon fire collision (unchanged) ===
                if (enemy is Dragon dragon)
                {
                    foreach (var bullet in dragon.bullets)
                    {
                        if (bullet.x == playerX && bullet.y == playerY)
                        {
                            playerHealth -= 7;
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
                    enemies.Add(new Slime(20, 6));
                    enemies.Add(new Slime(30, 4));
                    enemies.Add(new Slime(7, 7));
                    enemies.Add(new Slime(18, 1));
                    break;
                case 2:
                    enemies.Add(new Spider(5, 4));
                    enemies.Add(new BabySpider(12, 9));
                    enemies.Add(new BabySpider(19, 6));
                    break;
                case 3:
                    enemies.Add(new Scorpion(20, 9));
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
                case 3: playerX = 10; playerY = 13; break;
                case 4: playerX = 10; playerY = 5; break;
            }

            DrawCharacter(playerX, playerY, player);
            InitializeEnemiesForCurrentLevel();
        }


        //================== Story Screen ==================//
        void DisplayStoryScreen()
        {
           //onsole.Clear();
            if (!storyScreenDrawn)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.White;
                //onsole.WriteLine("──────────── STORY MODE ────────────\n");
               
                if (!firstStoryPlayed)
                {
                    Console.WriteLine(
@"
▀█▀ █░█ █▀▀   █░█ █▀▀ ▄▀█ █▀█ ▀█▀   █▀█ █▀▀   █▀▄ █▀▀ █▀▀ █▀▀ █▀█ ▀█▀ █ █▀█ █▄░█
░█░ █▀█ ██▄   █▀█ ██▄ █▀█ █▀▄ ░█░   █▄█ █▀░   █▄▀ ██▄ █▄▄ ██▄ █▀▀ ░█░ █ █▄█ █░▀█

How to play:

-> Use the arrow keys to move your character.
-> Make choices in the story using the indicated key.
-> Press SPACEBAR to attack adjacent enemies.
-----------------------------------------------------------------------------------------------------

This is your final test to prove yourself worthy to Omarious, the lead councilman of Angrulia. 

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

                                               ██
                                              ████
                     ▓▓                      █████
                ▒░░▒▓█████                  ████▓▓
               ▒░▒▓▓███▓▓▓                 ▓████▓ 
               ▒░░▓███▓░▒▒                ▓████▓  
                ▒░░▒▓▒░░▒▒               ▓████▓   
                ░░░░░▒▒▒                 ▓███▓▓   
                ░░░░▒▒                  ▓███▓▓    
               ░░░░░                   ▓███▓▓     
               ░░▒▒                    ▓██▓▓      
              ░░░▒▒                   ▓██▓▓       
             ░░░▒                    ▓███▓        
            ▒░▒▒                     ███▓         
           ▒░░▒▒                    ▓██▓▓         
          ░░░▒▒                    ▓██▓▓          
         ▒░░▒▒                    ▓██▓▓           
         ░░▒▒                    ███▓▓            
        ░░▒▒                   ███▓▓▓▓            
       ░░▒▒                ▓   ▓▒░▒█▓             
      ▒░▒▒                 ░▒▓▒░▒▒░█              
     ▒░░▒▒                  ▒▒▒▒▒▒░               
    ▒░░▒▒                     ░░░▒▒▒░░░           
   ▒░░▒▒▒                    ░░░                  
  ▒░░▒▒▒▒                   ░░░                   
 ▒░░▒▒▒▒▒                  ▒░░░                   
▒░▒▒  ░░▒                 ▒▒▒▒                    
░░▒▒                    ▓▒▒▒░                     
  ▒                      ░░░▒                     


[S] Scepter  [W] Sword"
                    ); 
                   
                    // Weapon choice handled per frame
                    storyScreenDrawn = true;
                    
                }
                else if (playerDied)
                {
                   
                    Console.WriteLine( $@"You kept fighting until you couldn't anymore. 

Your wounds are too deep, your strength is gone, and every breath is a challenge. 

You know it's over. Just like the others who tried, you failed.  

Your bones will lie here, another warning to anyone who comes after. 


Press [Enter] to retry this map.");
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
                                    Console.WriteLine($@"You enter the left tunnel and begin your descent. As you move deeper, your hands brush along the rough stone 
walls while your feet sink softly into a bed of moss. The darkness makes it nearly impossible to see. 
You raise your {chosenWeapon} and it emits a faint magical glow. The passage opens into a wide chamber.
The light dances across the rocks,revealing a faded mural. You step closer. Helsadona is depicted,
surrounded by wildlife. Foxes and deer bow willingly at her feet, an owl perched upon her shoulder nestles
into her neck. You reach out to press your hand against the mural. A wave of warmth flows through you,
calm yet strange, stealing your breath for a moment. Goosebumps cover your skin as you stumble back,
catching your heel on a small pile of healing mushrooms.For a moment you hesitate and think,
“Did Helsadona leave these behind?” You pick up a mushroom and eat it, rejuvenating your body.
You come across another opening in the stone and continue your journey inside. 


You’ve been walking these tunnels for a while now and you’ve noticed the increasingly alarming amount of spider
webs stream from wall to wall. It is so dense with webs at this point your arms are no match for them.
You draw your {chosenWeapon} and begin to. Every slice sends vibrations into the darkness, you continue
cutting while cursing under your breath, after cutting a particularly large web you hear a high pitched screech;
one so loud you fall to your knees in pain. While on the ground you're horrified by the sight of a colossal spider
descending from the ceiling. Baby spiders are scattered around about behind it. In a quick motion you grab your
{chosenWeapon} and ready yourself for a fight.
");

                                    // clear any leftover keypress before the pause
                                    while (Console.KeyAvailable) Console.ReadKey(true);

                                    Console.WriteLine("\nPress Enter twice to continue...");
                                      Console.ReadKey(true);
                                  if (  Input.IsKeyPressed(ConsoleKey.Enter))
                                        currentMapIndex = 2;
                                    SwitchToAdventureMode();
                                    return;
                                }
                                else if (Input.IsKeyPressed(ConsoleKey.R))
                                {
                                    Console.Clear();
                                    Console.WriteLine($@"You enter the right tunnel and begin your trek into the shadows. The sound of dripping water echoes like
a ticking clock. Your footsteps crunch over brittle bones half-buried in the dirt. Your surroundings
darken with each step, you raise your {chosenWeapon} and it emits a faint magical glow. As your eyes
adjust to the light, carvings etched into the walls come into view. You wipe away the dirt and dust,
and the images come to life. They tell stories both twisted and cruel. Helsadona, cloaked in all black,
stands over fallen creatures, their heads decapitated at her feet. In another carving, she drains the life
from the forest itself, roots curling away from her touch. A third one captures your attention, a serpent
coiled across the stone. Your finger follows its blood-stained scales from tail to head till you reach where
you expect to see the serpent's face, but it is no serpent at all. It is Helsadona, jaw unhinged as she
devours an innocent child. A cold shiver races down your spine. You tear your gaze away from the gruesome
depictions and continue your steps through the tunnel. You come across another opening in the stone
and continue your journey inside. 


You’ve been walking these tunnels for a while now and you’ve noticed the increasingly alarming amount of spider
webs streaming from wall to wall. It is so dense with webs at this point your arms are no match for them anymore. 
You draw your {chosenWeapon} and begin to hack your way through. Every slice sends vibrations into the darkness, 
you continue cutting while cursing under your breath until cutting a particularly thick web you hear a high-pitched
screech; one so loud you almost fall to your knees in pain. While on the ground covering your ears, you're horrified 
by the sight of a colossal spider descending from the ceiling. Baby spiders are scattered about behind them. 
In a quick motion ready your {chosenWeapon} and begin to fight.
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

                        case 2:
                            {
                                Console.WriteLine(
                            $@"You swiftly pulverize the colossal spider, your {chosenWeapon} glows purple. 
It is infused with spider venom! You are now one step closer to gaining the power needed to slay the dragon.
You brush the grime off your clothes and scan the cave ahead.  At the backside of the chamber, two more 
tunnel openings are there, each leading deeper into the unknown. Which path will you take?


Type [L] for left or [R] for right:");

                                if (Input.IsKeyPressed(ConsoleKey.L))
                                {
                                    Console.Clear();
                                    Console.WriteLine($@"You choose to explore the left tunnel. You walk for what seems like an eternity before the tunnel finally
widens into another open chamber. Inside you find a stone pillar with a bronzed plaque on top of it.
A torn banner hangs from the wall to the right of it. As you approach, it becomes clearer it is the crest of Angrulia.
You make your way over to the pillar and read the inscription on the plaque. “Helsadona, the wicked, the vile,
the damned. Her wrath has plagued Angrulia for centuries, causing vast destruction to the once beautiful town.
Fires erupted throughout the land, houses burned to ash, residents perished, and legacies were lost. Omarious 
became our light in the time of darkness. He is the only one we can trust.” You back away from the pillar,
letting the newfound knowledge set in. You swing your head around looking for the next tunnel.On your way
you pass the tattered crest banner and a sense of determination waves over you. You are doing this for the
better of the town, for Omarious, right? You make your way through the next tunnel.


The tunnel is short, you already see an opening in the near distance. You pick up the pace to get there.
The moment you step through the opening a wave of movement catches your eye, tiny baby scorpions scatter
in every direction. You stumble back, picking your feet up frantically to avoid crushing them, but they are
everywhere, coating the walls and ground. Panic takes over and you break into a sprint, desperate to get away.
The tunnel narrows, squeezing you between jagged rocks. Scorpions begin to leap from the walls, clinging to
your clothes and crawling on your skin. You push forward, heart pounding, until the tunnel finally loosens its
grip and the path widens once more. You see a faint light ahead. You move swiftly toward it, and as it grows
brighter, its source comes into focus. A gigantic red-glowing scorpion. The beast sets its sight on you, 
tail lashing the air in anticipation

");
                                    // clear any leftover keypress before the pause
                                    while (Console.KeyAvailable) Console.ReadKey(true);

                                    Console.WriteLine("\nPress Enter twice to continue...");
                                    Console.ReadKey(true);
                                    if (Input.IsKeyPressed(ConsoleKey.Enter))
                                        currentMapIndex = 3;
                                    SwitchToAdventureMode();
                                    return;
                                }
                                else if (Input.IsKeyPressed(ConsoleKey.R))
                                {
                                    Console.Clear();
                                    Console.WriteLine($@"You continue onward, a spiderweb still stuck to your boot. This tunnel is barely big enough
to comfortably maneuver through it. You shrug it off and conitnue till you reach another clearing. A sapling 
pokes from a crack in the ground, dripping a golden liquid. You look above it, the ceiling is painted in faded 
colors, cracked and crumbling with age. The painting remains visible despite the decay. It depicts Helsadona,
arms raised not in aggression, but in defense of the forest. Behind her the forest and all its creatures are
busied, burned, and harmed. She is not attacking, she stands ready to protect. From the sapling, the golden
liquid drips steadily, pooling onto the ground. As you approach, a small lizard darts into view at the edge 
of your vision. You raise your weapon instinctively, but it does not flee. Instead, it seems entirely
unconcerned by your presence. One of its legs is missing and many scars trailing up its back. It settles by
the puddle under the sapling, dipping its head down to drink. As it does, the scars begin to fade, and its
missing leg regenerates. Compelled, you step forward, and the lizard startles, darting away. Without thinking,
you cup the liquid into your hands and bring it to your lips. Warmth pulses through your body, spreading through
your skin. One by one, the scars you bear begin to vanish, leaving you whole.


The tunnel is short, you already see an opening in the near distance. You pick up the pace to get there.
The moment you step through the opening a wave of movement catches your eye, tiny baby scorpions scatter
in every direction. You stumble back, picking your feet up frantically to avoid crushing them, but they are
everywhere, coating the walls and ground. Panic takes over and you break into a sprint, desperate to get away.
The tunnel narrows, squeezing you between jagged rocks. Scorpions begin to leap from the walls, clinging to
your clothes and crawling on your skin. You push forward, heart pounding, until the tunnel finally loosens its
grip and the path widens once more. You see a faint light ahead. You move swiftly toward it, and as it grows
brighter, its source comes into focus. A gigantic red-glowing scorpion. The beast sets its sight on you, 
tail lashing the air in anticipation.
");

                                    // clear any leftover keypress before the pause
                                    while (Console.KeyAvailable) Console.ReadKey(true);

                                    Console.WriteLine("\nPress Enter twice to continue...");
                                    Console.ReadKey(true);
                                    if (Input.IsKeyPressed(ConsoleKey.Enter))
                                        currentMapIndex = 3;
                                    SwitchToAdventureMode();
                                    return;
                                }

                                return; // wait for L or R
                            }


                        case 3:
                            {
                                Console.WriteLine(
                            $@"After vanquishing the giant scorpion, your {chosenWeapon} glows red.
It is infused with the scorpion’s poison! You are now one step closer to gaining the power needed to slay
the dragon. You brush the grime off your clothes and scan the cave ahead. At the backside of the chamber,
one tunnel opening is before you, leading deeper into the unknown. 

Press [Enter] to continue:");

                                if (Input.IsKeyPressed(ConsoleKey.Enter))
                                {
                                    Console.Clear();
                                    Console.WriteLine($@"As you walk, you begin to wonder where Helsadona could be hiding. It feels like you’ve
searched every inch of this cave. Up ahead, a small crawl space catches your eye. You move toward it
and crouch down, slipping inside. The rocks are jagged, scraping against your arms and tearing at your
clothes, but you keep moving forward. The crawl space tunnel eventually opens into another dark chamber.
Bats hang motionless from the ceiling, and you stay quiet, careful not to wake them. Near the far wall, 
you spot a small pile of healing mushrooms. Hunger gnaws at you, you pick one up and eat it. A tingling 
spreads through your body as your strength returns. A narrow tunnel waits to your left. You take a 
steady breath and start down the path.

This is strange, you think to yourself. The cave has been ice-cold since you entered, but now, with each step,
the air grows warmer. Then hot. Sweltering. Sweat beads along your temples, sliding down your face as you wipe
your brow. Without warning, a deep roar rumbles through the tunnel, shaking the ground beneath your feet. 
You stumble, catching yourself against the wall. “I’m close,” you whisper under your breath. The roars grow 
louder, more frequent. The tunnel trembles like an earthquake as you move closer to the source. Just as you 
round a corner, a wave of fire bursts past your face, scorching the stone  beside you. It’s the dragon. 
The fire-breathing guardian of Helsadona. You take a step back and ready yourself for the final battle.
");

                                    // clear any leftover keypress before the pause
                                    while (Console.KeyAvailable) Console.ReadKey(true);

                                    Console.WriteLine("\nPress Enter twice to continue...");
                                    Console.ReadKey(true);
                                    if (Input.IsKeyPressed(ConsoleKey.Enter))
                                        currentMapIndex = 4;
                                    SwitchToAdventureMode();
                                    return;
                                }
                                else if (Input.IsKeyPressed(ConsoleKey.Enter))
                                {
                                    Console.Clear();
                                    Console.WriteLine($@"As you walk, you begin to wonder where Helsadona could be hiding. It feels like you’ve
searched every inch of this cave. Up ahead, a small crawl space catches your eye. You move toward it
and crouch down, slipping inside. The rocks are jagged, scraping against your arms and tearing at your
clothes, but you keep moving forward. The crawl space tunnel eventually opens into another dark chamber.
Bats hang motionless from the ceiling, and you stay quiet, careful not to wake them. Near the far wall, 
you spot a small pile of healing mushrooms. Hunger gnaws at you, you pick one up and eat it. A tingling 
spreads through your body as your strength returns. A narrow tunnel waits to your left. You take a 
steady breath. You must be close now, you think, and start down the path.

This is strange, you think to yourself. The cave has been ice-cold since you entered, but now, with each step,
the air grows warmer. Then hot. Sweltering. Sweat beads along your temples, sliding down your face as you wipe
your brow. Without warning, a deep roar rumbles through the tunnel, shaking the ground beneath your feet. 
You stumble, catching yourself against the wall. “I’m close,” you whisper under your breath. The roars grow 
louder, more frequent. The tunnel trembles like an earthquake as you move closer to the source. Just as you 
round a corner, another roar splits the air, and a wave of fire bursts past your face, scorching the stone 
beside you. It’s the dragon. The fire-breathing guardian of Helsadona. You take a step back and ready 
yourself for the final battle.");

                                    // clear any leftover keypress before the pause
                                    while (Console.KeyAvailable) Console.ReadKey(true);

                                    Console.WriteLine("\nPress Enter twice to continue...");
                                    Console.ReadKey(true);
                                    if (Input.IsKeyPressed(ConsoleKey.Enter))
                                        currentMapIndex = 4;
                                    SwitchToAdventureMode();
                                    return;
                                }

                                return; // wait for L or R
                            }

                        case 4:
                            {
                                Console.WriteLine(
                            $@"You stand hovering over the dragon's lifeless body. You hear a whimper from a dark corner of the cavern. 
You approach the noise. It's Helsadona, she looks scared and hurt. She speaks: “I know Omarious has sent you
here to kill me, just like he sent others before you. I am not what he says. I have only ever loved and cared 
for this town. Omarious just wants my power. Please believe me, we can join together and fight against Omarious
and save Angrulia”. 

Type [Y] for Yes or [N] for No:
");

                                if (Input.IsKeyPressed(ConsoleKey.Y))
                                {
                                    Console.Clear();
                                    Console.WriteLine($@"Helsadona smiles as you lower your weapon, approaching you quietly. “I knew I
could help you seek reason. Please, allow me to heal your wounds.”She takes your hand gently in hers, and a 
warm sensation flows through you. All the injuries you sustained through your adventure healed within seconds, 
and you felt reinvigorated immediately.“Please, let us exit this wretched cave and take care of Omarion once 
and for all. He won’t stand a chance against us both.” You backtrack through the caverns with Helsadona following 
behind. Any creatures you encounter on your journey back to Angrulia sense your alliance with the Helsdona and 
letyou pass without trouble. Now in possession of immense power, you two swiftly make your way through the town 
to stormthe council hall, hell-bent on slaying Omarious. You both attack Omarious with all your might. He is 
no match for Helsdona’s power, she holds him in a trance while you take one last swing of your weapon, killing 
Omarious. Helsadona and you slowly emerge from the council hall. A crowd of people stand around. You tell them 
the truth about what happened and everyone begins to cheer. At first, the brutality of your traitorous act towards
Omarious worried you.For days, you wondered if you made the right decision, taking control of the town with 
Helsadona. Your moral concernssubsided once you realized the chaos plaguing the area had ceased, as there had 
not been an incident since your rebellion.The forest, once dark and eerie, was now gorgeous and brimming with 
fresh life. Creatures thought to have gone extinctin the area returned to live beneath the shade of the trees.
It was Omarious all along, his jealousy of Helsadona rotted the town, but now, thanks to you, peace has finally
been restored to Angrulia.

       █ ▓▓█ █▓▓▓▓▓▓▓▓                                                  ▓▓▓█▓▓█   ▓             
        ▓▒▓▓▓▓▒▒▒▒▓▒▒▒▒▒▓                                               ▓▓▒▒▒▓▒▒▒▓▓▓▓▒▓▓▓         
       ▓▒▒▓▒▒▒▒▒▓▒▒▒▒▓▓▒▒▒▓                                           ▓▓▒▓▓▒▓▒▒▒▒▒▒▒▒▒▓▒▓▓        
      ▓▓▓▒▒▓▓▓▓▒▓░▒▒▒▒▒▒▒▒▒▓                                          ▒▒▒▒▒▒▒▒▒▒▒▒▒▓▒▓▒▒▓▓▓       
     ▓▓▒▒░▒▓▒▒▒░░░░▒▒▒▒▒░▒▒▓▓▓▓                                   █▓▓▓▒▒░▒▒▒░▒░▒░░░▒▒▓▒░▒▒▓▓      
    █▓▓▒▒▓▓▒░▒░░▒▒░▒▒▒░▒▒░▒▒▒▒▓                                  █▓▒▒░▒░░░░▒▒░░▓▓▒▒▒▒▒▒▓▓▒▓▓      
    █▓▓▒░░░▒░▒▒▒▒▒▒▒▒░▒▒▓▓▒▒░▒▒▓                                 ▓▒▒▒▒▒▒▓▒▒▒▒▓▒▒▒▓▒░▒▒░░░░▓▓      
   ▓▒▒░▒▒░░░▒░▒▒▒░▒▒░▒░░░░▒▒▒▒░▒                   ▒▒            ▓▒▒▒░▒▒░░░▒▒▒▒░░▒▒▒▓▒░░▒▒░▒▒▓    
▓▓▒▒▒▒▒▓▓▓▒▒░▒░░░░░░▒░░░░░░▒░░▒▒                   ░░░           ▒▒░▒▒▒░░░░░░░░▒▒▒░░░░▒▒▒▓▓▒▒▒▓██ 
█▓▒▒▒▒▒▒▓▓▓▒▒▒▒▓▓▒▓▒░░░▒▒░░░░▒▒▓█      ▓▒▓█▓       ▓▓▓   ▒       ▓▓▓▒░▒░▒▒░░░░▓▓▓▓▓▓▓░▓▒▓▒▓░▓▓▓▒▓ 
▓▓▒▓▒▓▒▒▒▒▒░▒▒░░▒░▒▒▒▒▒▒░░▒▒▓▓▓▒▓▓▓▒   ▒▓▓▒      ▓▒░░░▒▓ ▓   ▒▒▒▒▓▒▓▒▒▒░░▒▓▓▒▒▒░▒▒▒▒▒░▒▒▒▒▓▓▓▒▓▓▓▓
▓▒▒░░░░░░░░░░░░░░░░░░░░░▒▓▒▒▒▒▒▒░░▒▓   ▒▓█▓      ▒░░░░░░ ▓   ▓░▒▒▒▓▒▒▒▓▒░░░░░░░░░▒░░░░▒░░░░▒▒▒▒▓▓ 
    █▒░░░░░░░░░░░░░░░░░░░▒▒░▒▒▒▒░▒   ▓▓▓▓█▓▓     ░▒░░░░▒░▒    ▓▒▒▒▒▒░▒▒▒░░░░░░░░░░░░░░░░░░░░▓▓█▓  
       ▒░░░▒░░░░░░░░░░░░░▒▒▒░░▒░▒     ▒████▓▓  ▒▒▒░░░░░ ▒▓      ▓▒▒▒▒░▓▒▒▒░░░░░░░░░░░▒░░▒▒░       
        ▓▒▒▓▓░░▓░░░░▓▒▒▒▒░▒▒▓▓       ▒██▓▒▓█▓  ▓▓ ░░░░░ ▒         ▓▒▒▒▒▒▒▒▒▒▒░░░░▓░░░▓▒░▒▒        
             ▓░░░░░░                ░▓█▓█▓▓▓█    ▒░░░░░▓▓                   ▒░░░░▓▒░▓             
               ▒▒░░▒                ▒▓▓▓▓▓▓▓▓▓   ░░░░░░▒▓                    ▒░░▒░                
               ▒▒▒░░               ▒▓▓▓▓▓▓▒▓▓▓▓  ░░░░░░░▓                    ▒░▒▒▒                
               ▓▒▒▒▒▒            ▒▓▓▓▓▓▓▒▓▒▓▓▓▓▒▒░░░░░░░                     ▒▒▒▒▓                
              ▓▒▒▒▒▒▓             ▓▓▓▓▒ ▒▒ ▒▓▓▓  ░░▒░░░▒                    █▒▒▒▒▒▓               
             ░░▒▒▒▒░▒▓     ░░░      ▒              ░░░▒       ░▒         ░░░░▒░▒▒▒▓▓  ░░      ▒   
             ░░░           ▒░░░                   ░░░░ ░     ░░░▒         ░░░        ░░░░   ░░░░  
             ░░░             ░░▒    ░░░      ░░░  ░░░▓░░░░░░  ░░░  ░       ░░░ ░░    ░░░░  ░░░    
       ░░░   ░░░             ░░░░  ░░░░      ▒░░░░░░  ░░  ░░░░░░░░░░░░      ░░░░░░▒ ░░░░░ ░░░     
       ░░░░▒ ░░░              ░░░░░░░░░░░░▒   ░░░░░  ░░    ░░░  ░░░░░░       ░░░░░░░░░░░░░░░      
        ░░   ░░░  ░░░ ░░░░      ░░░░ ░░░░░░▓  ░░░░  ░░░     ░░░  ░░░░   ▒░░░░ ░░░░░░░░░░░░░       
        ░░   ░░░░░░░░░░░░░     ░░░░░░░░░░░░ ░░░░░░░░░░     ▓ ░░░  ░░░░  ░░░░░ ░░░░░░░░░░░▒        
        ░░    ░░░░░ ░░░░░░ ░░░░░░░░░░░░░░░░░░░░░░░░░░    ░░░░░░░░ ░░░░░░░░░░░░░░░░░░░░░░░░   ░░░░ 
        ░░░░░░░░░░░░░░░░░░░░░░░░  ░░░░░░░░░░░░░░░░░░░   ░░░░░░▒░░▒░░░░░░░░░░░░░░░░░░░░░░░░░  ░░░░ 


Congratulations, you have won.

");

                                    // clear any leftover keypress before the pause
                                    while (Console.KeyAvailable) Console.ReadKey(true);

                                   // Console.WriteLine("\nType [Y] for Yes or [N] for No:"); 
                                      Console.ReadKey(true);
                                    if (Input.IsKeyPressed(ConsoleKey.Y))
                                        currentMapIndex = 0;
                                    SwitchToAdventureMode();
                                    return;
                                }
                                else if (Input.IsKeyPressed(ConsoleKey.N))
                                {
                                    Console.Clear();
                                    Console.WriteLine($@"Helsadona’s voice trembles as she steps toward you, her palms raised in 
surrender. “Please, listen to me,” she pleads, eyes watery. “Omarious deceived you, deceived them all. 
I never sought destruction, only to protect what remains of the old magic. The forest, the creatures…”
“Enough,” you interrupt, your grip tightening around the {chosenWeapon}. It hums with the combined 
power of slime, spider, and scorpion, a culmination of everything you have fought to get here. “You think
I haven’t heard the stories?” you say. “The carvings. The plaques. The dead.” Her expression breaks, tears 
stain her cheeks. “Lies! Lies carved by fearful hands! He feared me, feared what he couldn’t control. I 
can prove—” You lunge before she can finish, driving your blade deep into her chest. Her body falls to the
cold damp ground. Helsadona is finally dead. You kneel to her level, cutting through her ribcage to retrieve 
her heart, still faintly glowing with magic. Your task is complete. Without looking back, you follow the 
tunnels out, emerging into the forest’s early morning light. When you reach the outskirts of Angrulia,
the town is already gathered. Murmurs ripple through the crowd as they see what you carry. Omarious
steps forward, his eyes widening at the sight of Helsadona’s heart.“You’ve done it,” he whispers,
awe-struck. “After all these years... I am— ahem —Angrulia is free.” He grabs your hand and raises 
your arm high, and the crowd erupts in cheers. “The Hero of Angrulia!” they chant. “The slayer of 
the Helsadona!”

                                                                                                  
               ▒                                                                ▒▓▒               
          ▒    ▒▒                                                                ▒▒   ▒▒          
            ▒▒▒▒                                                                  ▒▒▒▒            
          ▒▒▓ ▒▒▒▒▒                                                            ▓▒▒▒▒▓▓▒▒          
           ▓▒▒▒▒         ▓▓ ▒                                        ▒ ▓▓         ▒▒▒▒▓           
          ▒▒▒▓ ▓▒        ▒▒▒▒          ▓                             ▒▒▒▒        ▒▓▒▓▓▒▓          
        ▒▓▓   ▒▓▒▓        ▒▒▒▒▒▓      ▒░▓          ▒▒              ▒▒▒▒▒        ▒▒█▒    ▒▒        
         ▒     ▒ ▒▒▒     ▒▒▒  ▓  ░▒▒ ▒▒▒▓▓▓        ░░░                ▒▒▒     ▒▒▒ ▒     ▒         
      ▒▒  ▒▒▒ ▒▓▒ ▒▒ ▓▓▒▒▒▒▒▒▒▒ ▒▒▒▓ ▓█▓█▓▓        ▓▓▓   ▒         ▒▓▒▒▒▒▒▒▒▓ ▒▒ ▒▓▒▒▒▒   ▒▒      
        ▓   ▒▒▒▒▒▒▒░▒    ▒    ▒   ▒ ▒▒▓▓▓▓▒▓     ▓▒░░░▒▓ ▓             ▓▒    ▒▒▒▒▒▒  ▒  ▓▒        
          ▒▓   ▒▒▒▒▒▒            ▒▓▒▒░▒▒▒▒▒▓     ▒░░░░░░ ▓                   ▒▒▒▒▒▒   ▒▓          
          ░▒▒▒▒▒▒                ▒▒▒▒░░▒▒░▒▓▓    ░▒░░░░▒░▒                      ▓▒▒▒▒▒▒           
            ▒▒▒▓▒▒               ▒░░▒░▒▒▒░▓▓▓  ▒▒▒░░░░░ ▒▓                      ▒▒▓▒▒▒            
            ▒▒▒ ▒▒               ▒░░░░░▒░▒▒▒▓█ ▓▓ ░░░░░ ▒                       ▒▒ ▒▒▒            
            ▒▒▒▒▒                ▒▒░░░░▒▒░▒▒▓▓   ▒░░░░░▓▓                        ▒▒▒▒▒▒           
            ▒▒▒░▒                ▒▓▒░░░░▓░▒░▒▓   ░░░░░░▒▓                        ▒░▒▒▒            
            ▒▒▒▒░                ▒▓▒░░░░▒░▓░▒▒   ░░░░░░░▓                        ▒▒▒▒▒            
             ▒▒▒▒                ▒▓▒▒░░░▒░░▒▒▒▓ ▒░░░░░░░                         ▒▒▒▒             
             ▒▒▒▒                ▒▒▒░░░░░░▒░░▒▒  ░░▒░░░▒                         ▒▒▒▒             
             ▒░▒▒          ░░░                     ░░░▒       ░▒         ░░░░    ▒▒▒▒▒░░      ▒   
            ▒░░▒▒          ▒░░░                   ░░░░ ░     ░░░▒         ░░░    ▒▒▒▒▒░░░   ░░░░  
           ▒▒░░░▒            ░░▒    ░░░      ░░░  ░░░▓░░░░░░  ░░░  ░       ░░░ ░░▒▒░▒░░░░  ░░░    
       ░░░   ░░░             ░░░░  ░░░░      ▒░░░░░░  ░░  ░░░░░░░░░░░░      ░░░░░░▒ ░░░░░ ░░░     
       ░░░░▒ ░░░              ░░░░░░░░░░░░▒   ░░░░░  ░░    ░░░  ░░░░░░       ░░░░░░░░░░░░░░░      
        ░░   ░░░  ░░░ ░░░░      ░░░░ ░░░░░░▓  ░░░░  ░░░     ░░░  ░░░░   ▒░░░░ ░░░░░░░░░░░░░       
        ░░   ░░░░░░░░░░░░░     ░░░░░░░░░░░░ ░░░░░░░░░░     ▓ ░░░  ░░░░  ░░░░░ ░░░░░░░░░░░▒        
        ░░    ░░░░░ ░░░░░░ ░░░░░░░░░░░░░░░░░░░░░░░░░░    ░░░░░░░░ ░░░░░░░░░░░░░░░░░░░░░░░░   ░░░░ 
        ░░░░░░░░░░░░░░░░░░░░░░░░  ░░░░░░░░░░░░░░░░░░░   ░░░░░░▒░░▒░░░░░░░░░░░░░░░░░░░░░░░░░  ░░░░ 


Congratulations, you have won.

");

                                    // clear any leftover keypress before the pause
                                    while (Console.KeyAvailable) Console.ReadKey(true);

                                   // Console.WriteLine("\nType [Y] for Yes or [N] for No:");
                                    Console.ReadKey(true);
                                    if (Input.IsKeyPressed(ConsoleKey.N))
                                        currentMapIndex = 0;
                                    SwitchToAdventureMode();
                                    return;
                                }

                                return; // wait for L or R
                            }


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
