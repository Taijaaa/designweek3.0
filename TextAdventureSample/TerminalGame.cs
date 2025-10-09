using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks.Dataflow;

namespace MohawkTerminalGame
{
    public class TerminalGame
    {
        // Place your variables here
        int battlewon;
        
        /// Run once before Execute begins
        public void Setup()
        {
           // Program.TerminalExecuteMode = TerminalExecuteMode.ExecuteLoop;
            Program.TerminalInputMode = TerminalInputMode.KeyboardReadAndReadLine;

            Terminal.SetTitle("the heart of deception ");
            Terminal.RoboTypeIntervalMilliseconds = 70; // 70 milliseconds
            Terminal.UseRoboType = true; // use slow character typing
            Terminal.WriteWithWordBreaks = true; // donbreak around wors, don't cut them off
            Terminal.WordBreakCharacter = ' '; // break on spaces
        }

        
        public void Execute()
        {
            Terminal.RoboTypeIntervalMilliseconds = 30;
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
            // player option for a weapon to proceed 
            Terminal.WriteLine("which weapon do you choose [septor] or the [sword]");
            
            string answer = Terminal.ReadAndClearLine();
            if (answer.ToLower().Equals("septor"))
            {
                Terminal.RoboTypeIntervalMilliseconds = 50;
                Terminal.WriteLine("ahh a septor what a mysterious choice");
            }
            else if (answer.ToLower().Equals("sword"))
            {
                Terminal.RoboTypeIntervalMilliseconds = 100;
                Terminal.WriteLine("the sword a great weapon to slice through your enemies");

                Terminal.WriteLine("The *weapon of your choice* is drawn to your hands like a magnet, and grasping its handle makes you feel powerful." +
                    " The elf continues, “To defeat the dragon that guards the witch, you must first overcome the slimes, spiders, and scorpions lurking within." +
                    " Each foe you vanquish will infuse your weapon with their power. Good luck, and remember, nothing is ever as it seems." +
                    "”With that, the elf vanishes," +
                    " leaving you alone at the cave’s entrance. " +
                    "Taking a deep breath, you shake off any fear and step into the darkness.");
            }
            Terminal.WriteLine("The air grows heavier with each step, thick with the stench of mold and mildew." +
                " You pull your collar over your nose, pushing onward through the twisting passage. Just as you round a bend," +
                " your foot sinks into something gooey and moist. A squelch echoes through the tunnel. Then comes the wet, sludge-like," +
                " dragging sound of something moving through the darkness. You tense, bracing yourself for whatever waits around the corner.");
            
            
            //terminal control we are going to be switching from a text based game into a fight scene made in 2d interactive
            //and then switching back to texted based terminal when gameplay is complete 
            
            
            Terminal.WriteLine("**gameplay * *");

            //end of gameplay we are now switching back to text based gameplay 

            

            {


            }

            Terminal.WriteLine("");
            
            Terminal.WriteLine("  You enter the right tunnel and begin your trek into the shadows. The sound of dripping water echoes like a ticking clock. " +
                "Your footsteps crunch over brittle bones half-buried in the dirt. Your surroundings darken with each step, you raise your *weapon of choice* " +
                "and it emits a faint magical glow. As your eyes adjust to the light, carvings etched into the walls come into view." +
                " You wipe away the dirt and dust, and the images come to life.They tell stories both twisted and cruel. Helsadona, " +
                "cloaked in all black, stands over fallen creatures, their heads decapitated at her feet. In another carving," +
                " she drains the life from the forest itself, roots curling away from her touch. A third one captures your attention," +
                " a serpent coiled across the stone. Your finger follows its blood-stained scales from tail to head till you reach where you expect to see the serpent's face" +
                ", but it is no serpent at all. It is Helsadona, jaw unhinged as she devours an innocent child. A cold shiver races down your spine." +
                " You tear your gaze away from the gruesome depictions and continue your steps through the tunnel. " +
                "You come across another opening in the stone and continue your journey inside.");
            {
              

                   
        
    }
            
        }

    }
}
