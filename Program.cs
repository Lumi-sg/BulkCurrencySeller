using Gma.System.MouseKeyHook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Spectre.Console;

namespace BulkCurrencySeller
{
    public class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var startPriceCheck = Combination.FromString("Control+C");

            Action priceCheck = async () => //start of hotkey action
            {
                await Task.Delay(50); // this is to prevent the program grabbing the previously queried item's output before the new one is provided

                var clipboard = Clipboard.GetText(); // prevents user accidentally inputting an invalid clipboard string into the program, which causes the program to crash
                if (!clipboard.Contains("Item Class: "))
                {
                    AnsiConsole.MarkupLine($"\n[underline red3_1]Error:[/]\n\n[red3_1]Invalid clipboard, did you accidentally copy a non PoE item?[/]");
                    return;
                }
                var clipboardLines = clipboard.Split('\n');

                //shitty way of preventing errors (these are items not valid according to API)
                if (!clipboardLines[0].Contains("Item Class: Stackable Currency"))
                {
                    var invalidItemType = clipboardLines[2].Trim(); //grab invalid request
                    AnsiConsole.MarkupLine($"\n[underline red3_1]Error:[/]\n\n[red3_1]{invalidItemType} is not a valid currency.[/]");
                    Console.WriteLine("");
                }
                else if (clipboardLines[2].Contains("Chaos Orb") && !clipboardLines[2].Contains("Veiled") && !clipboardLines[2].Contains("Eldritch"))
                {
                    AnsiConsole.MarkupLine($"\n[underline red3_1]Error:[/]\n\n[green1]A Chaos Orb is worth one Chaos Orb.[/]");
                }
                else if (clipboardLines[2].Contains("Charged Compass"))
                {
                    AnsiConsole.MarkupLine($"\n[underline red3_1]Error:[/]\n\n[red3_1]Charged Compass is not a valid currency.[/]");
                }
                else //if item is valid, continue
                {
                    var playerCurrencyType = clipboardLines[2].Trim(); //grab currency type
                    string stackSizeString = clipboardLines[4].Replace(",", ""); //trim out comma because the following regex already took everything I had to give

                    var regexSring = Regex.Match(stackSizeString, @"Stack Size: (\d*)/(\d*)").Groups[1].Value; //grab stack value from clipboard
                    var playerCurrencyAmount = int.Parse(regexSring);

                    //greeting / divides searches for better readability
                    Console.WriteLine("");
                    var rule = new Rule("[green1]Price Check Started![/]");
                    rule.Alignment = Justify.Left;
                    rule.RuleStyle("purple3");
                    AnsiConsole.Write(rule);


                    var client = new HttpClient(); //instantiate HTTP Client
                    var response = await client.GetFromJsonAsync<Root>("https://poe.ninja/api/data/currencyoverview?league=Archnemesis&type=Currency");//fetch/deserialize Poe Ninja API
                    if (response != null)
                    {
                        Line orbData = response.lines.FirstOrDefault(line => line.currencyTypeName == playerCurrencyType); //check API for searched currency's fields

                        //Outputs to user
                        if (orbData != null)
                        {
                            var userCurrencyStackValue = Math.Round(Math.Round(orbData.chaosEquivalent, 5) * (playerCurrencyAmount), 0); //Calculates stack value with rounded result

                            //self explanatory user outputs for relevant information
                            AnsiConsole.MarkupLine($"\nCurrency Type: [underline orange1]{orbData.currencyTypeName}[/]");
                            AnsiConsole.MarkupLine($"\nAvailable to sell: [underline orange1]{playerCurrencyAmount}[/]");
                            AnsiConsole.MarkupLine($"\nValue: [underline green1]{orbData.chaosEquivalent}[/][orange1] Chaos[/]");
                            AnsiConsole.MarkupLine($"\nSale Price: [underline green1]{userCurrencyStackValue}[/] [orange1]Chaos[/]");
                            Clipboard.SetText($"{userCurrencyStackValue}/{playerCurrencyAmount}");
                            AnsiConsole.MarkupLine($"\n[underline green1]{userCurrencyStackValue}/{playerCurrencyAmount}[/] copied to clipboard!");
                        }
                        else
                        {   //poe ninja does not list certain currencies as currencies for some reason
                            AnsiConsole.MarkupLine($"\n[underline red3_1]Error:[/]\n\n[red3_1]{playerCurrencyType} is not a valid currency.[/]");
                        }
                    }
                }
            };// end of action which is basically one massive method

            //assigns the key combination to the action
            var assignment = new Dictionary<Combination, Action>()
            {
                {startPriceCheck, priceCheck}
            };

            //watch for hotkey press
            Hook.GlobalEvents().OnCombination(assignment);

            Application.Run(new ApplicationContext());
        }

    }

}
