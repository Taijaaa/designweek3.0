using System;
using System.ComponentModel;

namespace MohawkTerminalGame
{
    public class TerminalGame
    {
        // Place your variables here

        
        /// Run once before Execute begins
        public void Setup()
        {
            Program.TerminalExecuteMode = TerminalExecuteMode.ExecuteLoop;
            Program.TerminalInputMode = TerminalInputMode.KeyboardReadAndReadLine;

            Terminal.SetTitle("the heart of deception ");
            Terminal.RoboTypeIntervalMilliseconds = 70; // 70 milliseconds
            Terminal.UseRoboType = true; // use slow character typing
            Terminal.WriteWithWordBreaks = true; // donbreak around wors, don't cut them off
            Terminal.WordBreakCharacter = ' '; // break on spaces
        }

        // Execute() runs based on Program.TerminalExecuteMode (assign to it in Setup).
        //  ExecuteOnce: runs only once. Once Execute() is done, program closes.
        //  ExecuteLoop: runs in infinite loop. Next iteration starts at the top of Execute().
        //  ExecuteTime: runs at timed intervals (eg. "FPS"). Code tries to run at Program.TargetFPS.
        //               Code must finish within the alloted time frame for this to work well.
        public void Execute()
        {
            Terminal.RoboTypeIntervalMilliseconds = 50;
            Terminal.Beep();
            Terminal.WriteLine("This is your final test to prove yourself worthy to Omarious, the lead councilman of Angrulia. Many have tried before, but those who ventured out never returned." +
                " Your task is to track down Helsadona" +
                "the banished witch who dwells deep within the cave at the edge of the local forest and bring back her heart." +
                "You walk alongside a forest elf guiding you toward your destination. He speaks of the town’s resentment toward Helsadona," +
                "how many believe she is the cause of the havoc and chaos that plagues Angrulia. She was once a councilwoman of Angrulia, " +
                "until a falling out with Omarious led to her banishment." +
                "To this day no one knows what truly happened between them.As you reach the crumbling cave entrance, the elf turns to you." +
                " “She is the most powerful being this town has ever seen. You will need something to defend yourself.” With a flick of his wrist, " +
                "a golden glow appears before you. Within it floats three weapons: a scepter, a sword, and a spear. “Which one calls to you?”");
            

            Terminal.Beep();
            Terminal.RoboTypeIntervalMilliseconds = 10;
            // player option to choose a weapon  
            Terminal.WriteLine("which weapon do you choose [septor], [sword], or a [spear]");
            
            string answer = Terminal.ReadAndClearLine();
            if (answer.ToLower().Equals("y"))
            {
                Terminal.RoboTypeIntervalMilliseconds = 50;
                Terminal.WriteLine("Mankind thanks you, Elite McGeet.");
            }
            else if (answer.ToLower().Equals("n"))
            {
                Terminal.RoboTypeIntervalMilliseconds = 100;
                Terminal.WriteLine("Mission failed before it even began.");
            }
            else
            {
                Terminal.RoboTypeIntervalMilliseconds = 150;
                Terminal.WriteLine("MISCHIEVIOUS ARE WE?");
            }
            Terminal.WriteLine();
            Terminal.WriteLine("Press CTRL+C to force quit any program.");
            Terminal.WriteLine();
        }

    }
}
