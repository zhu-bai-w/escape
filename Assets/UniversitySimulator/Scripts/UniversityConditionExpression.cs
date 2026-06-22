using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class UniversityConditionExpression : MonoBehaviour
{
    [TextArea]
    public string conditionExpression;

    public bool emptyExpressionPasses = true;
    public float dangerLowThreshold = 30f;
    public float dangerHighThreshold = 70f;
    public bool debugLogInvalidExpression;

    [ReadOnlyInspector] public string lastError;

    static readonly Dictionary<string, valueDefinitions.values> ValueMap = new Dictionary<string, valueDefinitions.values>
    {
        { "bodyMind", valueDefinitions.values.bodyMind },
        { "academics", valueDefinitions.values.academics },
        { "relationships", valueDefinitions.values.relationships },
        { "economy", valueDefinitions.values.economy }
    };

    public bool IsMet()
    {
        lastError = "";
        if (string.IsNullOrWhiteSpace(conditionExpression))
        {
            return emptyExpressionPasses;
        }

        Parser parser = new Parser(conditionExpression, this);
        bool result = parser.TryEvaluate(out bool isMet, out string error);
        lastError = error;

        if (!result && debugLogInvalidExpression)
        {
            Debug.LogWarning("Invalid conditionExpression on " + gameObject.name + ": " + error + " Expression: " + conditionExpression);
        }

        return result && isMet;
    }

    ConditionValue ResolveIdentifier(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            return ConditionValue.Number(0f);
        }

        string normalized = identifier.Trim();
        switch (normalized)
        {
            case "round":
            case "day":
            case "days":
            case "currentRunDays":
                return ConditionValue.Number(UniversityTrueEndingProgress.CurrentRunDays);
            case "lifetimeDays":
            case "totalDays":
                return ConditionValue.Number(UniversityTrueEndingProgress.LifetimeDays);
            case "gameOverCount":
            case "failures":
            case "failureCount":
                return ConditionValue.Number(UniversityTrueEndingProgress.GameOverCount);
        }

        valueDefinitions.values valueType;
        if (ValueMap.TryGetValue(normalized, out valueType) && valueManager.instance != null)
        {
            ValueScript valueScript = valueManager.instance.getFirstFittingValue(valueType);
            if (valueScript != null)
            {
                return ConditionValue.Number(valueScript.value);
            }
        }

        if (UniversityTrueEndingProgress.GetPermanentFlag(normalized))
        {
            return ConditionValue.Bool(true);
        }

        if (GameDictionary.ContainsKey(normalized))
        {
            string storedValue = GameDictionary.GetValue(normalized);
            float numericValue;
            if (float.TryParse(storedValue, NumberStyles.Float, CultureInfo.InvariantCulture, out numericValue))
            {
                return ConditionValue.Number(numericValue);
            }

            bool boolValue;
            if (TryParseBool(storedValue, out boolValue))
            {
                return ConditionValue.Bool(boolValue);
            }

            return ConditionValue.Text(storedValue);
        }

        return ConditionValue.Number(0f);
    }

    ConditionValue EvaluateFunction(string functionName, List<string> arguments)
    {
        if (string.Equals(functionName, "danger", StringComparison.OrdinalIgnoreCase))
        {
            if (arguments.Count < 1)
            {
                return ConditionValue.Error("danger() requires a value name.");
            }

            ConditionValue value = ResolveIdentifier(arguments[0]);
            if (!value.TryGetNumber(out float numericValue))
            {
                return ConditionValue.Bool(false);
            }

            float low = dangerLowThreshold;
            float high = dangerHighThreshold;
            if (arguments.Count >= 2)
            {
                float.TryParse(arguments[1], NumberStyles.Float, CultureInfo.InvariantCulture, out low);
            }

            if (arguments.Count >= 3)
            {
                float.TryParse(arguments[2], NumberStyles.Float, CultureInfo.InvariantCulture, out high);
            }

            return ConditionValue.Bool(numericValue <= low || numericValue >= high);
        }

        if (string.Equals(functionName, "flag", StringComparison.OrdinalIgnoreCase))
        {
            if (arguments.Count < 1)
            {
                return ConditionValue.Error("flag() requires a flag id.");
            }

            return ConditionValue.Bool(UniversityTrueEndingProgress.GetPermanentFlag(arguments[0]));
        }

        if (string.Equals(functionName, "hidden", StringComparison.OrdinalIgnoreCase))
        {
            if (arguments.Count < 1)
            {
                return ConditionValue.Error("hidden() requires a variable id.");
            }

            return ResolveIdentifier(arguments[0]);
        }

        return ConditionValue.Error("Unknown function: " + functionName);
    }

    static bool TryParseBool(string value, out bool result)
    {
        string normalized = (value ?? "").Trim().ToLowerInvariant();
        if (normalized == "true" || normalized == "1" || normalized == "yes")
        {
            result = true;
            return true;
        }

        if (normalized == "false" || normalized == "0" || normalized == "no")
        {
            result = false;
            return true;
        }

        result = false;
        return false;
    }

    enum TokenType
    {
        Identifier,
        Number,
        Operator,
        LeftParen,
        RightParen,
        Comma,
        End
    }

    struct Token
    {
        public TokenType Type;
        public string Text;
    }

    struct ConditionValue
    {
        public enum ValueKind
        {
            Number,
            Bool,
            Text,
            Error
        }

        public ValueKind Kind;
        public float NumberValue;
        public bool BoolValue;
        public string TextValue;

        public static ConditionValue Number(float value)
        {
            return new ConditionValue { Kind = ValueKind.Number, NumberValue = value };
        }

        public static ConditionValue Bool(bool value)
        {
            return new ConditionValue { Kind = ValueKind.Bool, BoolValue = value };
        }

        public static ConditionValue Text(string value)
        {
            return new ConditionValue { Kind = ValueKind.Text, TextValue = value ?? "" };
        }

        public static ConditionValue Error(string value)
        {
            return new ConditionValue { Kind = ValueKind.Error, TextValue = value ?? "Unknown condition error." };
        }

        public bool IsError
        {
            get { return Kind == ValueKind.Error; }
        }

        public bool IsTruthy()
        {
            switch (Kind)
            {
                case ValueKind.Number:
                    return !Mathf.Approximately(NumberValue, 0f);
                case ValueKind.Bool:
                    return BoolValue;
                case ValueKind.Text:
                    return !string.IsNullOrEmpty(TextValue);
                default:
                    return false;
            }
        }

        public bool TryGetNumber(out float value)
        {
            if (Kind == ValueKind.Number)
            {
                value = NumberValue;
                return true;
            }

            if (Kind == ValueKind.Bool)
            {
                value = BoolValue ? 1f : 0f;
                return true;
            }

            return float.TryParse(TextValue, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        public string AsText()
        {
            switch (Kind)
            {
                case ValueKind.Number:
                    return NumberValue.ToString(CultureInfo.InvariantCulture);
                case ValueKind.Bool:
                    return BoolValue ? "true" : "false";
                default:
                    return TextValue ?? "";
            }
        }
    }

    class Parser
    {
        readonly List<Token> tokens;
        readonly UniversityConditionExpression owner;
        int index;
        string error;

        public Parser(string expression, UniversityConditionExpression owner)
        {
            tokens = Tokenize(expression);
            this.owner = owner;
        }

        public bool TryEvaluate(out bool isMet, out string parseError)
        {
            isMet = false;
            error = "";
            bool value = ParseOr();

            if (string.IsNullOrEmpty(error) && Peek().Type != TokenType.End)
            {
                error = "Unexpected token: " + Peek().Text;
            }

            parseError = error;
            if (!string.IsNullOrEmpty(error))
            {
                return false;
            }

            isMet = value;
            return true;
        }

        bool ParseOr()
        {
            bool value = ParseAnd();
            while (MatchKeyword("OR") || MatchOperator("||"))
            {
                bool right = ParseAnd();
                value = value || right;
            }

            return value;
        }

        bool ParseAnd()
        {
            bool value = ParsePrimaryBool();
            while (MatchKeyword("AND") || MatchOperator("&&"))
            {
                bool right = ParsePrimaryBool();
                value = value && right;
            }

            return value;
        }

        bool ParsePrimaryBool()
        {
            if (Match(TokenType.LeftParen))
            {
                bool value = ParseOr();
                Expect(TokenType.RightParen, ")");
                return value;
            }

            ConditionValue left = ParseValue();
            if (left.IsError)
            {
                error = left.TextValue;
                return false;
            }

            Token comparison = Peek();
            if (comparison.Type == TokenType.Operator && IsComparisonOperator(comparison.Text))
            {
                index++;
                ConditionValue right = ParseValue();
                if (right.IsError)
                {
                    error = right.TextValue;
                    return false;
                }

                return Compare(left, comparison.Text, right);
            }

            return left.IsTruthy();
        }

        ConditionValue ParseValue()
        {
            Token token = Peek();
            if (token.Type == TokenType.Number)
            {
                index++;
                float number;
                if (float.TryParse(token.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
                {
                    return ConditionValue.Number(number);
                }

                return ConditionValue.Error("Invalid number: " + token.Text);
            }

            if (token.Type == TokenType.Identifier)
            {
                index++;
                if (Match(TokenType.LeftParen))
                {
                    List<string> arguments = ParseFunctionArguments();
                    return owner.EvaluateFunction(token.Text, arguments);
                }

                if (string.Equals(token.Text, "true", StringComparison.OrdinalIgnoreCase))
                {
                    return ConditionValue.Bool(true);
                }

                if (string.Equals(token.Text, "false", StringComparison.OrdinalIgnoreCase))
                {
                    return ConditionValue.Bool(false);
                }

                return owner.ResolveIdentifier(token.Text);
            }

            error = "Expected value, got: " + token.Text;
            return ConditionValue.Error(error);
        }

        List<string> ParseFunctionArguments()
        {
            List<string> arguments = new List<string>();
            while (Peek().Type != TokenType.RightParen && Peek().Type != TokenType.End)
            {
                Token token = Peek();
                if (token.Type == TokenType.Identifier || token.Type == TokenType.Number)
                {
                    arguments.Add(token.Text);
                    index++;
                }
                else
                {
                    error = "Invalid function argument: " + token.Text;
                    break;
                }

                if (!Match(TokenType.Comma))
                {
                    break;
                }
            }

            Expect(TokenType.RightParen, ")");
            return arguments;
        }

        bool Compare(ConditionValue left, string op, ConditionValue right)
        {
            float leftNumber = 0f;
            float rightNumber = 0f;
            bool numericComparison = left.TryGetNumber(out leftNumber) && right.TryGetNumber(out rightNumber);

            if (numericComparison)
            {
                switch (op)
                {
                    case ">":
                        return leftNumber > rightNumber;
                    case ">=":
                        return leftNumber >= rightNumber;
                    case "<":
                        return leftNumber < rightNumber;
                    case "<=":
                        return leftNumber <= rightNumber;
                    case "==":
                    case "=":
                        return Mathf.Approximately(leftNumber, rightNumber);
                    case "!=":
                        return !Mathf.Approximately(leftNumber, rightNumber);
                }
            }

            int stringCompare = string.Compare(left.AsText(), right.AsText(), StringComparison.OrdinalIgnoreCase);
            switch (op)
            {
                case "==":
                case "=":
                    return stringCompare == 0;
                case "!=":
                    return stringCompare != 0;
                default:
                    return false;
            }
        }

        Token Peek()
        {
            return index < tokens.Count ? tokens[index] : new Token { Type = TokenType.End, Text = "" };
        }

        bool Match(TokenType type)
        {
            if (Peek().Type != type)
            {
                return false;
            }

            index++;
            return true;
        }

        bool MatchKeyword(string keyword)
        {
            Token token = Peek();
            if (token.Type != TokenType.Identifier || !string.Equals(token.Text, keyword, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            index++;
            return true;
        }

        bool MatchOperator(string op)
        {
            Token token = Peek();
            if (token.Type != TokenType.Operator || token.Text != op)
            {
                return false;
            }

            index++;
            return true;
        }

        void Expect(TokenType type, string label)
        {
            if (!Match(type) && string.IsNullOrEmpty(error))
            {
                error = "Expected " + label + ".";
            }
        }

        static bool IsComparisonOperator(string op)
        {
            return op == ">" || op == ">=" || op == "<" || op == "<=" || op == "==" || op == "=" || op == "!=";
        }

        static List<Token> Tokenize(string expression)
        {
            List<Token> result = new List<Token>();
            int i = 0;
            while (i < expression.Length)
            {
                char ch = expression[i];
                if (char.IsWhiteSpace(ch))
                {
                    i++;
                    continue;
                }

                if (char.IsLetter(ch) || ch == '_')
                {
                    int start = i;
                    i++;
                    while (i < expression.Length && (char.IsLetterOrDigit(expression[i]) || expression[i] == '_'))
                    {
                        i++;
                    }

                    result.Add(new Token { Type = TokenType.Identifier, Text = expression.Substring(start, i - start) });
                    continue;
                }

                if (char.IsDigit(ch) || ch == '.' || (ch == '-' && i + 1 < expression.Length && char.IsDigit(expression[i + 1])))
                {
                    int start = i;
                    i++;
                    while (i < expression.Length && (char.IsDigit(expression[i]) || expression[i] == '.'))
                    {
                        i++;
                    }

                    result.Add(new Token { Type = TokenType.Number, Text = expression.Substring(start, i - start) });
                    continue;
                }

                if (ch == '(')
                {
                    result.Add(new Token { Type = TokenType.LeftParen, Text = "(" });
                    i++;
                    continue;
                }

                if (ch == ')')
                {
                    result.Add(new Token { Type = TokenType.RightParen, Text = ")" });
                    i++;
                    continue;
                }

                if (ch == ',')
                {
                    result.Add(new Token { Type = TokenType.Comma, Text = "," });
                    i++;
                    continue;
                }

                string two = i + 1 < expression.Length ? expression.Substring(i, 2) : "";
                if (two == ">=" || two == "<=" || two == "==" || two == "!=" || two == "&&" || two == "||")
                {
                    result.Add(new Token { Type = TokenType.Operator, Text = two });
                    i += 2;
                    continue;
                }

                if (ch == '>' || ch == '<' || ch == '=')
                {
                    result.Add(new Token { Type = TokenType.Operator, Text = ch.ToString() });
                    i++;
                    continue;
                }

                result.Add(new Token { Type = TokenType.Identifier, Text = ch.ToString() });
                i++;
            }

            result.Add(new Token { Type = TokenType.End, Text = "" });
            return result;
        }
    }
}
