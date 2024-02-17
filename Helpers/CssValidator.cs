using System;
using FluentResults;
using ExCSS;
using System.IO;

namespace com.github.benpocalypse.markerator;

public static class CssValidator
{
        public static Result<string> ValidateAndGetCustomCssContents(string cssFilenames)
    {
        return Result.Try<string>(() =>
        {
            if (cssFilenames.Equals(string.Empty))
            {
                return Markerator.DefaultCss;
            }

            var parser = new StylesheetParser();
            string cssFilePath = Path.Combine(Directory.GetCurrentDirectory(), "input", cssFilenames);
            string cssContent = File.ReadAllText(cssFilePath);
            var stylesheet = parser.Parse(cssContent);

            foreach (var rule in stylesheet.StyleRules)
            {
                if (!rule.SelectorText.Contains(".navigation-title") &&
                    !rule.SelectorText.Contains(".navigation") &&
                    !rule.SelectorText.Contains(".content") &&
                    !rule.SelectorText.Contains("head") &&
                    !rule.SelectorText.Contains("body") &&
                    !rule.SelectorText.Contains("h"))
                    {
                        Console.WriteLine("Returning default Css.");
                        throw new Exception($"Failed to parse {cssFilenames}.");
                    }
            }

            Console.WriteLine("Returning custom Css.");
            return cssContent;
        });
    }
}
