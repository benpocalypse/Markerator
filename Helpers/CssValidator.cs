using ExCSS;
using FluentResults;

namespace Markerator.Helpers;

/// <summary>
/// This class is leveraged  
/// </summary>
public static class CssValidator
{
    /// <summary>
    /// This function will validate that the passed in CSS file contains the correct elements to allow the supplied
    /// file to correctly theme the generated website. If the validation fails, the built-in default CSS will be used.
    /// </summary>
    /// <param name="cssFilenames">The name and location of the provided CSS file.</param>
    /// <returns>Either the custom CSS, or the default CSS upon failure.</returns>
    /// <exception cref="Exception">Using the FluentResult library, this will bubble up the exception to the caller.</exception>
    public static Result<string> ValidateAndGetCustomCssContents(string cssFilenames)
    {
        return Result.Try<string>(() =>
        {
            if (cssFilenames.Equals(string.Empty))
            {
                return Globals.DefaultCss;
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
