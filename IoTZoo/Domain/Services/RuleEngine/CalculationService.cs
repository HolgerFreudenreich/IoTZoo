// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   P L A Y G R O U N D
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 - 2026 Holger Freudenreich under the MIT license
// --------------------------------------------------------------------------------------------------------------------

using Dapper;
using DataAccess.Services;
using Domain.Interfaces.RuleEngine;
using Domain.Pocos;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Domain.Services.RuleEngine;

public class CalculationService : DataServiceBase, ICalculationService
{
    public CalculationService(ILogger<CalculationService> logger, IOptions<AppSettings> options) : base(options, logger)
    {
        SetConnectionString(string.Empty);
        Db = GetNewOpenedConnection();
    }

    public object? Calculate(string expression)
    {
        if (string.IsNullOrEmpty(expression))
        {
            return null;
        }
        if (expression.StartsWith("{"))
        {
            return expression; // return the original expression if it is a JSON object
        }
        if (expression.StartsWith("["))
        {
            return expression; // return the original expression if it is a JSON object
        }

        try
        {
            expression = expression.Replace("&&", "and")
                                   .Replace("||", "or")
                                   .Replace(FunctionNames.Calc, " ", StringComparison.OrdinalIgnoreCase);
            if (!IsCalculable(expression))
            {
                return expression; // plain text, no need to ask SQLite
            }
            // Let SQLite do the calculation because it is fast!
            return Db.ExecuteScalar($"select {expression};");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
        }
        return expression; // return the original expression if an exception occurs
    }

    private static readonly HashSet<string> AllowedWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "and", "or", "not", "in", "is", "isnull", "notnull", "null", "like", "glob", "regexp", "match", "escape",
        "between", "case", "when", "then", "else", "end", "true", "false", "cast", "as", "collate", "nocase", "binary",
        "rtrim", "integer", "real", "text", "numeric", "blob", "current_timestamp", "current_date", "current_time"
    };

    private const string ForbiddenCharacters = ";[]{}`$@:?#\\";

    /// <summary>
    /// Checks whether the expression can be a SQLite scalar expression: only numbers, operators, parentheses,
    /// quoted text, known keywords and function calls are allowed. Statements, comments and free text are not.
    /// </summary>
    public static bool IsCalculable(string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return false;
        }

        ReadOnlySpan<char> text = expression.AsSpan().Trim().TrimEnd(';').Trim();
        if (text.IsEmpty)
        {
            return false;
        }

        bool hasOperand = false;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '\'' || c == '"')
            {
                int end = text[(i + 1)..].IndexOf(c);
                if (end < 0)
                {
                    return false; // unterminated text literal
                }
                // A doubled quote is an escaped quote and is handled as two adjacent literals.
                i += end + 1;
                hasOperand = true;
            }
            else if (c == '-' && i + 1 < text.Length && text[i + 1] == '-')
            {
                return false; // SQL comment
            }
            else if (c == '/' && i + 1 < text.Length && text[i + 1] == '*')
            {
                return false; // SQL comment
            }
            else if (ForbiddenCharacters.Contains(c))
            {
                return false;
            }
            else if (char.IsDigit(c))
            {
                while (i + 1 < text.Length && (char.IsLetterOrDigit(text[i + 1]) || text[i + 1] == '.' || text[i + 1] == '_'))
                {
                    i++;
                }
                hasOperand = true;
            }
            else if (char.IsLetter(c) || c == '_')
            {
                int start = i;
                while (i + 1 < text.Length && (char.IsLetterOrDigit(text[i + 1]) || text[i + 1] == '_'))
                {
                    i++;
                }
                string word = text.Slice(start, i - start + 1).ToString();
                int next = i + 1;
                while (next < text.Length && char.IsWhiteSpace(text[next]))
                {
                    next++;
                }
                bool isFunctionCall = next < text.Length && text[next] == '(';
                if (isFunctionCall && !AllowedWords.Contains(word))
                {
                    if (word.Equals("load_extension", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                    hasOperand = true;
                }
                else if (AllowedWords.Contains(word))
                {
                    hasOperand |= word.Equals("null", StringComparison.OrdinalIgnoreCase)
                                  || word.Equals("true", StringComparison.OrdinalIgnoreCase)
                                  || word.Equals("false", StringComparison.OrdinalIgnoreCase)
                                  || word.StartsWith("current_", StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    return false; // free text or a statement keyword like select/drop
                }
            }
        }
        return hasOperand;
    }
}
