using System;
using System.Text;
using System.Runtime.CompilerServices;
using Epic.Training.Project.Inventory.Text.Exceptions;

namespace Epic.Training.Project.Inventory.Text.UserInput
{
    internal static class Input
    {
        #region ITEM PROPERTY VALIDATION - [GetName|GetQuantity|GetWholesale|GetWeight]FromUser

        /// <summary>
        /// Retrieves and validates user input for item names. 
        /// </summary>
        /// <param name="inv">Currently loaded Inventory</param>
        /// <param name="callerName">Name of calling Menu/method</param>
        /// <exception cref="Epic.Training.Project.Inventory.Text.Exceptions">Thrown when user presses [ESC] key</exception>
        /// <returns type="String">Name of Item</returns>
        internal static string GetNameFromUser(Inventory inv, [CallerMemberName] string callerName = "")
        {
            string proposedName;

            while (true)
            {
                try
                {
                    proposedName = betterInput(String.Format("\b<<{0}>> ", Resource1.NAME_PROMPT));
                }
                catch (EscapeKeyPressedException ex)
                {
                    throw ex; //Reminder to let the caller of this method handle it. 
                }


                if (string.IsNullOrWhiteSpace(proposedName))
                {
                    Console.WriteLine("\n[! {0} !]\n", Resource1.NAME_EMPTY);
                    continue;
                }
                else if (proposedName.Length > 26)
                {
                    Console.WriteLine("\n[! '{0}' {1} !]\n", proposedName, Resource1.NAME_TOO_LONG);
                    continue;
                }
                else if (inv.Contains(proposedName))
                {
                    Console.WriteLine("\n[! Product with name '{0}' already exists in the inventory !]\n", proposedName);
                    continue;
                }

                return proposedName;
            }
        }

        internal static int GetQuantityFromUser(Item itemStage)
        {
            bool result;
            int proposedQuantity;

            while (true)
            {
                string proposedAsString = null;
                try
                {
                    proposedAsString = betterInput(String.Format("\b<<{0} {1}s>> ", Resource1.QUANT_PROMPT, itemStage.Name));
                }
                catch (EscapeKeyPressedException ex)
                {
                    throw ex;
                }

                result = Int32.TryParse(proposedAsString, out proposedQuantity);

                if (result)
                {
                    if (proposedQuantity < 0)
                    {
                        Console.WriteLine("\n[! '{0}' {1} !]\n", proposedQuantity, Resource1.ERROR_NEGATIVE);
                        continue;
                    }

                    return proposedQuantity;
                }
                else
                {
                    Console.WriteLine("\n[! '{0}' {1} !]\n", proposedAsString, Resource1.PARSE_ERROR_INT); //TryParse is already going to fail an entry larger than type's MaxValue
                }

            }
        }

        internal static decimal GetWholesaleFromUser(Item itemStage)
        {
            bool result;
            decimal proposedWholesale;

            while (true)
            {
                string proposedAsString = null;

                try
                {
                    proposedAsString = betterInput(String.Format("\b<<{0} {1}>> ", Resource1.WHOLESALE_PROMPT, itemStage.Name));
                }
                catch (EscapeKeyPressedException ex)
                {
                    throw ex;
                }

                result = Decimal.TryParse(proposedAsString, out proposedWholesale);

                if (result)
                {
                    if (proposedWholesale <= 0)
                    {
                        Console.WriteLine("\n[! '{0}' {1} !]\n", proposedWholesale, Resource1.ERROR_ZERO_NEG);
                        continue;
                    }

                    return proposedWholesale;
                }
                else
                {
                    Console.WriteLine("\n[! {0} {1} !]\n", proposedAsString, Resource1.PARSE_ERROR_DEC); //TryParse is already going to fail an entry larger than type's MaxValue
                }

            }
        }

        internal static double GetWeightFromUser(Item itemStage)
        {
            bool result;
            double proposedWeight;

            while (true)
            {
                string proposedAsString = null;
                try
                {
                    proposedAsString = betterInput(String.Format("\b<<{0} {1}>> ", Resource1.WEIGHT_PROMPT, itemStage.Name));
                }
                catch (EscapeKeyPressedException ex)
                {
                    throw ex;
                }

                result = Double.TryParse(proposedAsString, out proposedWeight);

                if (result)
                {
                    if (proposedWeight <= 0)
                    {
                        Console.WriteLine("\n[! '{0}' {1} !]\n", proposedWeight, Resource1.ERROR_ZERO_NEG);
                        continue;
                    }

                    return proposedWeight;
                }
                else
                {
                    Console.WriteLine("\n[! '{0}' {1} !]\n", proposedAsString, Resource1.PARSE_ERROR_DEC); //TryParse is already going to fail an entry larger than type's MaxValue
                }
            }
        }

        #endregion

        #region BETTER INPUT - ESCAPE PROMPTS AT ANY TIME

        /// <summary>
        /// Allows escape from any user prompt, at any time. 
        /// </summary>
        /// <param name="prompt">[String] Prompt displayed to user for input.</param>
        /// <exception cref="Epic.Training.Project.Inventory.Text.Exceptions">Thrown when user presses [ESC] key</exception>
        /// <returns>[String] upon hitting ENTER</returns>
        internal static string betterInput(string prompt)
        {
            StringBuilder input = new StringBuilder();
            ConsoleKeyInfo cki = new ConsoleKeyInfo();

            string inputAsStr = null; //Returned as input.ToString()
            bool inLoop = true; //Set to false when user hits [ENTER]

            Console.Write(prompt);
            while (inLoop)
            {
                cki = Console.ReadKey();

                switch (cki.Key)
                {
                    case ConsoleKey.Escape:
                        throw new EscapeKeyPressedException(); //Let caller handle it
                    case ConsoleKey.Backspace:
                        if (input.Length > 0)
                        {
                            input.Remove(input.Length - 1, 1);
                            int currentLineCursor = Console.CursorTop;
                            Console.SetCursorPosition(input.Length + prompt.Length - 1, Console.CursorTop);
                            Console.Write(new string(' ', Console.WindowWidth)); //Clears/Overwrites character removed from input StringBuilder
                            Console.SetCursorPosition(input.Length + prompt.Length - 1, currentLineCursor); //Brings cursor back to same position 
                        }
                        else
                        {
                            ClearCurrentConsoleLine();
                            Console.Write(prompt);
                        }

                        break;
                    case ConsoleKey.Enter:
                        inputAsStr = input.ToString();
                        inLoop = false;

                        break;
                    default:
                        if (Char.IsLetterOrDigit(cki.KeyChar) || Char.IsPunctuation(cki.KeyChar) || Char.IsSymbol(cki.KeyChar) || Char.IsSeparator(cki.KeyChar) || cki.KeyChar == ' ')
                        {
                            input.Append(cki.KeyChar);
                        }

                        break;
                }
            }

            Console.WriteLine();
            return inputAsStr;
        }

        private static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }

        #endregion
    }
}
