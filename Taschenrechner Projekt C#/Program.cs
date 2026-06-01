// Program.cs
// Ein einfacher Konsolen-Taschenrechner mit Ausdrucksauswertung
// Unterstützte Operatoren: +, -, *, /, ^ und Klammern ()
// Dezimaltrenner: Punkt (z.B. 3.14)

using System;
using System.Collections.Generic;
using System.Globalization;

class Program
{
    static void Main()
    {
        Console.WriteLine("Willkommen zum C# Taschenrechner.");
        Console.WriteLine("Gib einen Ausdruck ein (z.B. 2 + 3 * (4 - 1) ^ 2) oder 'exit' zum Beenden.");
        while (true)
        {
            Console.Write("> ");
            string input = Console.ReadLine();
            if (input == null) break;
            input = input.Trim();
            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("quit", StringComparison.OrdinalIgnoreCase))
                break;
            if (input == "") continue;

            try
            {
                double result = EvaluateExpression(input);
                Console.WriteLine("= " + result.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler: " + ex.Message);
            }
        }

        Console.WriteLine("Auf Wiedersehen!");
    }

    // Hauptfunktion: wandelt Infix -> RPN (Shunting-yard) -> wertet RPN aus
    static double EvaluateExpression(string expr)
    {
        var tokens = Tokenize(expr);
        var rpn = ShuntingYard(tokens);
        return EvaluateRpn(rpn);
    }

    // Tokenizer: trennt Zahlen, Operatoren und Klammern
    static List<string> Tokenize(string s)
    {
        var tokens = new List<string>();
        int i = 0;
        while (i < s.Length)
        {
            char c = s[i];
            if (char.IsWhiteSpace(c)) { i++; continue; }

            if (char.IsDigit(c) || c == '.')
            {
                int start = i;
                bool dotSeen = (c == '.');
                i++;
                while (i < s.Length && (char.IsDigit(s[i]) || (!dotSeen && s[i] == '.')))
                {
                    if (s[i] == '.') dotSeen = true;
                    i++;
                }
                tokens.Add(s.Substring(start, i - start));
                continue;
            }

            // Operators and parentheses
            if ("+-*/^()".IndexOf(c) >= 0)
            {
                // Handle unary minus:
                if (c == '-')
                {
                    // Unary if at start or after another operator or after '('
                    string prev = tokens.Count > 0 ? tokens[tokens.Count - 1] : null;
                    if (prev == null || IsOperator(prev) || prev == "(")
                    {
                        // Represent unary minus as "u-" token
                        tokens.Add("u-");
                        i++;
                        continue;
                    }
                }
                tokens.Add(c.ToString());
                i++;
                continue;
            }

            throw new Exception($"Ungültiges Zeichen: '{c}'");
        }
        return tokens;
    }

    static bool IsOperator(string token)
    {
        return token == "+" || token == "-" || token == "*" || token == "/" || token == "^" || token == "u-";
    }

    // Shunting-yard: Infix -> RPN
    static List<string> ShuntingYard(List<string> tokens)
    {
        var output = new List<string>();
        var ops = new Stack<string>();

        foreach (var token in tokens)
        {
            double tmp;
            if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out tmp))
            {
                output.Add(token);
            }
            else if (IsOperator(token))
            {
                while (ops.Count > 0 && IsOperator(ops.Peek()))
                {
                    string o1 = token;
                    string o2 = ops.Peek();

                    int p1 = Precedence(o1);
                    int p2 = Precedence(o2);
                    bool leftAssoc = IsLeftAssociative(o1);

                    if ((leftAssoc && p1 <= p2) || (!leftAssoc && p1 < p2))
                    {
                        output.Add(ops.Pop());
                    }
                    else break;
                }
                ops.Push(token);
            }
            else if (token == "(")
            {
                ops.Push(token);
            }
            else if (token == ")")
            {
                bool foundLeft = false;
                while (ops.Count > 0)
                {
                    var top = ops.Pop();
                    if (top == "(") { foundLeft = true; break; }
                    output.Add(top);
                }
                if (!foundLeft) throw new Exception("Klammern nicht ausgeglichen: fehlende '('");
            }
            else
            {
                throw new Exception("Unbekannter Token: " + token);
            }
        }

        while (ops.Count > 0)
        {
            var top = ops.Pop();
            if (top == "(" || top == ")") throw new Exception("Klammern nicht ausgeglichen");
            output.Add(top);
        }

        return output;
    }

    static int Precedence(string op)
    {
        switch (op)
        {
            case "u-": return 5; // unary minus: höchster Vorrang
            case "^": return 4;
            case "*": case "/": return 3;
            case "+": case "-": return 2;
            default: return 0;
        }
    }

    static bool IsLeftAssociative(string op)
    {
        // ^ ist rechtsassoziativ, u- (unary minus) behandeln wir als rechtsassoziativ hier
        if (op == "^") return false;
        if (op == "u-") return false;
        return true;
    }

    // RPN-Auswertung
    static double EvaluateRpn(List<string> rpn)
    {
        var stack = new Stack<double>();
        foreach (var token in rpn)
        {
            double value;
            if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                stack.Push(value);
            }
            else if (token == "u-")
            {
                if (stack.Count < 1) throw new Exception("Fehler bei unärem Minus: Stapel leer");
                double a = stack.Pop();
                stack.Push(-a);
            }
            else if (IsOperator(token))
            {
                if (stack.Count < 2) throw new Exception("Zu wenige Operanden für Operator " + token);
                double b = stack.Pop(); // rechter Operand
                double a = stack.Pop(); // linker Operand
                double res = 0;
                switch (token)
                {
                    case "+": res = a + b; break;
                    case "-": res = a - b; break;
                    case "*": res = a * b; break;
                    case "/":
                        if (b == 0) throw new Exception("Division durch Null");
                        res = a / b; break;
                    case "^": res = Math.Pow(a, b); break;
                    default: throw new Exception("Unbekannter Operator: " + token);
                }
                stack.Push(res);
            }
            else
            {
                throw new Exception("Unbekannter RPN-Token: " + token);
            }
        }

        if (stack.Count != 1) throw new Exception("Ungültiger Ausdruck (mehr als ein Wert im Stapel)");
        return stack.Pop();
    }
}
