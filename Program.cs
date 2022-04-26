using Gma.System.MouseKeyHook;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Spectre.Console;

namespace BulkCurrencySeller;

public static class Program
{
    [STAThread]
    private static void Main()
    {
        var startPriceCheck = Combination.FromString("Control+C");

        var assignment = new Dictionary<Combination, Action>
        {
            {startPriceCheck, PriceCheck}
        };

        Hook.GlobalEvents().OnCombination(assignment);

        Application.Run(new ApplicationContext());
    }

    private static async void PriceCheck()
    {
        // Wait for new clipboard contents
        await Task.Delay(50);

        var clipboard = Clipboard.GetText();
        var clipboardLines = clipboard.Split(Environment.NewLine);

        if (!ValidateClipboard(clipboardLines))
        {
            return;
        }

        var playerCurrencyType = clipboardLines[2].Trim();
        var playerCurrencyAmount = int.Parse(clipboardLines[4][12..^3], NumberStyles.Number);

        var rule = new Rule("[green1]Price Check Started![/]");
        rule.Alignment = Justify.Left;
        rule.RuleStyle("purple3");
        AnsiConsole.Write(rule);

        var client = new HttpClient();
        var response = await client.GetFromJsonAsync<Root>("https://poe.ninja/api/data/currencyoverview?league=Archnemesis&type=Currency");

        if (response == null)
        {
            return;
        }
        //If the user searches the value of chaos, we return the chaos value per Exalted Orb.
        if (clipboardLines[2].Contains("Chaos Orb") && !clipboardLines[2].Contains("Veiled") && !clipboardLines[2].Contains("Eldritch"))
        {
            playerCurrencyType = "Exalted Orb";
            playerCurrencyAmount = 1;
        }

        var orbData = response.lines.FirstOrDefault(line => line.currencyTypeName == playerCurrencyType);

        //The game classifies several items as currency that are not supported by the API, due to worth being insignificant.
        if (orbData == null)
        {
            AnsiConsole.MarkupLine($"\n[underline red3_1]Error:[/]\n\n[red3_1]{playerCurrencyType} is not a valid currency.[/]\n");
            return;
        }

        var userCurrencyStackValue = Math.Round(Math.Round(orbData.chaosEquivalent, 5) * (playerCurrencyAmount), 0);

        AnsiConsole.MarkupLine($"\nCurrency Type: [underline orange1]{orbData.currencyTypeName}[/]");
        AnsiConsole.MarkupLine($"\nAvailable to sell: [underline orange1]{playerCurrencyAmount}[/]");
        AnsiConsole.MarkupLine($"\nValue: [underline green1]{orbData.chaosEquivalent}[/][orange1] Chaos[/]");
        AnsiConsole.MarkupLine($"\nSale Price: [underline green1]{userCurrencyStackValue}[/] [orange1]Chaos[/]");
        Clipboard.SetText($"{userCurrencyStackValue}/{playerCurrencyAmount}");
        AnsiConsole.MarkupLine($"\n[underline green1]{userCurrencyStackValue}/{playerCurrencyAmount}[/] copied to clipboard!\n");
    }

    private static bool ValidateClipboard(string[] clipboardLines)
    {
        if (!clipboardLines[0].Contains("Item Class: "))
        {
            return false;
        }

        if (!clipboardLines[0].Contains("Stackable Currency"))
        {
            var invalidItemType = clipboardLines[2].Trim();
            AnsiConsole.MarkupLine($"\n[underline red3_1]Error:[/]\n\n[red3_1]{invalidItemType} is not a valid currency.[/]\n");
            return false;
        }
        
        if (clipboardLines[2].Contains("Charged Compass"))
        {
            AnsiConsole.MarkupLine($"\n[underline red3_1]Error:[/]\n\n[red3_1]Charged Compass is not a valid currency.[/]\n");
            return false;
        }

        return true;
    }
}