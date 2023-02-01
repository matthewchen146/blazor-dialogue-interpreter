
#nullable enable 

namespace Parser 
{
    public class ErrorInfo
    {
        public string type = "";
        public string message = "Error";
        public Position position = new();
        public int LineNumber
        {
            set
            {}
            get
            {
                return position.LineNumber;
            }
        }
        public int columnIndex = 0;
        public int ColumnNumber
        {
            set
            {}
            get
            {
                return position.ColumnNumber;
            }
        }
        public Token? token = null;
        public ErrorInfo(int lineIndex, int columnIndex, string _message)
        {
            position.lineIndex = lineIndex;
            position.columnIndex = columnIndex;
            message = _message;
        }

        public ErrorInfo(int lineIndex, string _message)
        {
            position.lineIndex = lineIndex;
            columnIndex = 0;
            message = _message;
        }

        public ErrorInfo(Token _token, string _message)
        {
            position.Set(_token.position);
            token = _token;
            message = _message;
        }

        public ErrorInfo(string _message)
        {
            message = _message;
        }

        public ErrorInfo(string _type, int lineIndex, int columnIndex, string _message)
        {
            type = _type;
            position.lineIndex = lineIndex;
            position.columnIndex = columnIndex;
            message = _message;
        }

        public ErrorInfo(string _type, int lineIndex, string _message)
        {
            type = _type;
            position.lineIndex = lineIndex;
            columnIndex = 0;
            message = _message;
        }

        public ErrorInfo(string _type, Token _token, string _message)
        {
            type = _type;
            position.Set(_token.position);
            token = _token;
            message = _message;
        }

        public ErrorInfo(string _type, string _message)
        {
            type = _type;
            message = _message;
        }

        public ErrorInfo()
        {

        }

        public override string ToString()
        {
            return (type.Length > 0 ? $"{type} Error " : "") + $"(Ln {LineNumber} Col {ColumnNumber}) - " + message;
        }
    }

    public class Position
    {
        public int index = 0;
        public int lineIndex = 0;
        public int columnIndex = 0;
        public int LineNumber {
            set
            {}
            get
            {
                return lineIndex + 1;
            }
        }
        public int ColumnNumber
        {
            set
            {}
            get
            {
                return columnIndex + 1;
            }
        }

        public Position(int _index = 0, int _lineIndex = 0, int _columnIndex = 0)
        {
            index = _index;
            lineIndex = _lineIndex;
            columnIndex = _columnIndex;
        }

        public Position(Position position)
        {
            Set(position);
        }

        public Position Clone()
        {
            Position clone = new();
            clone.index = index;
            clone.lineIndex = lineIndex;
            clone.columnIndex = columnIndex;
            return clone;
        }

        public void Set(Position position)
        {
            index = position.index;
            lineIndex = position.lineIndex;
            columnIndex = position.columnIndex;
        }

        public static Dictionary<int, List<int>> newlineIndices = new();
        public static void CalculateLineAndColumn(ref string text, Position position)
        {
            int stringHash = text.GetHashCode();

            List<int> indexList;

            if (!newlineIndices.ContainsKey(stringHash))
            {
                indexList = new();
                newlineIndices.Add(stringHash, indexList);
                // process text to find all indices where new line occurs
                for (int index = 0; index < text.Length; index++)
                {
                    if (text[index] == '\n')
                    {
                        indexList.Add(index);
                    }
                }
            }
            else
            {
                indexList = newlineIndices[stringHash];
            }

            if (indexList.Count == 0)
            {
                position.lineIndex = 0;
                position.columnIndex = position.index;
                return;
            }

            if (position.index < indexList[0])
            {
                position.lineIndex = 0;
                position.columnIndex = position.index;
                return;
            }

            for (int i = indexList.Count - 1; i >= 0; i--)
            {
                int testIndex = indexList[i];
                int difference = position.index - testIndex;
                if (difference > 0)
                {
                    position.lineIndex = i + 1;
                    position.columnIndex = difference;
                    break;
                }
                else if (difference == 0)
                {
                    position.lineIndex = i;
                    position.columnIndex = difference;
                    break;
                }
            }
        }
    }

    public class Token 
    {
        public string type = "";
        public string value = "";
        public Position position = new();
        public int LineNumber {
            set
            {}
            get
            {
                return position.LineNumber;
            }
        }
        public int ColumnNumber
        {
            set
            {}
            get
            {
                return position.ColumnNumber;
            }
        }
        public Token(string _type, string _value, int index, int lineIndex, int columnIndex)
        {
            type = _type;
            value = _value;
            position.index = index;
            position.lineIndex = lineIndex;
            position.columnIndex = columnIndex;
        }

        public Token(string _type, string _value, Position _position)
        {
            type = _type;
            value = _value;
            position = _position;
        }
    }

    public delegate string? TokenValidator(ref string text, int startIndex, out bool newlined);

    


    // in BNF terms: an expression
    public class Group 
    {
        // public List<Term> terms = new();
        public bool repeatable = false;
        public bool optional = false;
        public string value = "";
        public List<Group> children = new();

        public Group(bool _optional, bool _repeatable, params object[] args)
        {
            optional = _optional;
            repeatable = _repeatable;
            foreach (object arg in args)
            {
                if (arg is string s)
                {
                    children.Add(new(s));
                }
                else if (arg is Group group)
                {
                    children.Add(group);
                }
            }
        }
        public Group(bool _optional, params object[] args)
        {
            optional = _optional;
            foreach (object arg in args)
            {
                if (arg is string s)
                {
                    children.Add(new(s));
                }
                else if (arg is Group group)
                {
                    children.Add(group);
                }
            }
        }

        public Group(string _value)
        {
            value = _value;
        }

        public Group(params object[] args)
        {
            foreach (object arg in args)
            {
                if (arg is string s)
                {
                    children.Add(new(s));
                }
                else if (arg is Group group)
                {
                    children.Add(group);
                }
            }
        }


        public override string ToString()
        {
            string s = this is OrderedGroup ? "ORD[" : "GRP[ ";
            foreach (Group child in children)
            {
                // if (term.Group != null)
                // {
                //     if (term.Value.Length == 0)
                //     {
                //         s += $"{term.Group}, ";
                //     }
                    
                // }
                // else
                // {
                //     s += $"<{term.Value}>, ";
                // }
            }
            return s + "]";
        }
    }

    public class OrderedGroup : Group
    {
        public OrderedGroup(params object[] _terms) : base(_terms)
        {

        }
    }

    public class Parser 
    {
        public bool debug = true;

        class TrieNode
        {
            readonly Dictionary<char, TrieNode> children = new();

            public void Add(string word)
            {
                int index = 0;
                TrieNode curr = this;
                while (index < word.Length)
                {
                    if (!curr.children.ContainsKey(word[index]))
                    {
                        TrieNode child = new();
                        curr.children.Add(word[index], child);
                    }
                    curr = curr.children[word[index]];

                    index += 1;
                }
            }

            public bool Find(string word)
            {
                int index = 0;
                TrieNode curr = this;
                while (index < word.Length)
                {
                    if (curr.children.TryGetValue(word[index], out TrieNode? child))
                    {
                        if (index == word.Length - 1)
                        {
                            return true;
                        }

                        curr = child;
                        index += 1;
                    }
                    else
                    {
                        break;
                    }
                }
                return false;
            }
        }

        public List<Token> Parse(ref string text, out List<ErrorInfo> errors, Group group, Position? position = null)//int index = 0)
        {
            position ??= new();
            int index = position.index;

            if (Specification.nonterminals.TryGetValue(group.value, out Group? specGroup))
            {
                specGroup.value = group.value;
                group = specGroup;
            }

            // Console.WriteLine($"prediction - {group.value}");

            List<Token> parsedTokens = new();
            errors = new();

                    
            // if group is repeatable, it will parse UNTIL there is a failure
            // if group is not repeatable, it will only call this block once
            for (int iter = 0; (iter < 1 && !group.repeatable) || group.repeatable; iter++)
            {

                if (group.children.Count == 0)
                {
                    bool success = false;

                    // check terminal
                    if (Specification.terminals.TryGetValue(group.value, out TokenValidator? validator))
                    {
                        string? tokenValue = validator.Invoke(ref text, index, out bool newlined);
                        if (tokenValue != null)
                        {
                            // found valid and matching token
                            Token token = new(group.value, tokenValue, position);
                            parsedTokens.Add(token);
                            success = true;

                            if (newlined)
                            {
                                position.lineIndex += 1;   
                            }
                        }
                        else
                        {
                            success = false;
                            errors.Add(new("Parse", $"Unexpected Token. Expected [{group.value}]"));
                            // break;
                        }
                    }

                    // if terminal doesnt match, look for one that does
                    // foreach (string type in terminals.Keys)
                    // {
                    //     validator = terminals[type];
                    //     string? tokenValue = validator.Invoke(ref text, index, out int endIndex);
                    //     if (tokenValue != null)
                    //     {
                    //         // found valid token
                    //         Token token = new(group.value, tokenValue, index, 0, 0);
                    //         parsedTokens.Add(token);
                    //     }
                    // }

                    string successText = success ? $"SCSS [{parsedTokens[0].value}]" : "FAIL";
                    Console.WriteLine($"checking {group.value} - {successText}");
                    // return parsedTokens;
                    if (success)
                    {
                        continue;
                    }
                    // break repeatable
                    break;
                }

                
                int nextIndex = parsedTokens.Count > 0 ? parsedTokens[^1].position.index + parsedTokens[^1].value.Length : index;
                if (parsedTokens.Count > 0)
                {
                    Console.WriteLine($"next index: [{nextIndex}] last token: [{parsedTokens[^1].value}]");
                }
                

                if (nextIndex >= text.Length)
                {
                    break;
                    // return parsedTokens;
                }

                // check group
                if (group is OrderedGroup)
                {
                    List<Token> groupTokens = new();

                    bool success = true;

                    // check all success
                    foreach (Group child in group.children)
                    {
                        
                        int groupNextIndex = nextIndex;
                        if (groupTokens.Count > 0)
                        {
                            groupNextIndex = groupTokens[^1].position.index + groupTokens[^1].value.Length;
                        }

                        List<Token> resultingTokens = Parse(ref text, out List<ErrorInfo> parsedErrors, child, new(groupNextIndex));//groupNextIndex);

                        if (resultingTokens.Count == 0)
                        {
                            if (!child.optional)
                            {
                                errors.AddRange(parsedErrors);
                                success = false;
                                // break ordered group
                                break;
                            }
                        }

                        groupTokens.AddRange(resultingTokens);
                    }

                    string successText = success ? "SCSS" : "FAIL";
                    Console.WriteLine($"checking all success - {successText} - {group.value}");

                    parsedTokens.AddRange(groupTokens);

                    if (!success)
                    {
                        // break repeatable
                        break;
                    }
                    
                }
                else
                {
                    bool success = false;

                    List<ErrorInfo> groupErrors = new();

                    // check one success
                    foreach (Group child in group.children)
                    {
                        List<Token> resultingTokens = Parse(ref text, out List<ErrorInfo> parsedErrors, child, new(nextIndex));//nextIndex);

                        if (resultingTokens.Count > 0)
                        {
                            success = true;
                            parsedTokens.AddRange(resultingTokens);
                            break;
                        }
                        else
                        {
                            groupErrors.AddRange(parsedErrors);
                        }
                    }

                    string successText = success ? "SCSS" : "FAIL";
                    Console.WriteLine($"checking one success - {successText} - {group.value}");

                    if (!success)
                    {
                        errors.AddRange(groupErrors);
                        break;
                    }
                }
            }

            return parsedTokens;
        }

        public List<Token> Parse(ref string text, out List<ErrorInfo> errors, string type, Position? position = null)//int index = 0)
        {
            position ??= new();

            int index = position.index;
            errors = new();
            if (Specification.nonterminals.TryGetValue(type, out Group? group))
            {
                
                group.value = type;
                List<Token> groupTokens = Parse(ref text, out List<ErrorInfo> parsedErrors, group, position);//index);
                errors.AddRange(parsedErrors);

                foreach (Token token in groupTokens)
                {
                    Position.CalculateLineAndColumn(ref text, token.position);
                }

                return groupTokens;
            }

            List<Token> parsedTokens = new();

            if (index == text.Length)
            {
                return parsedTokens;
            }
            
            // check terminal
            if (Specification.terminals.TryGetValue(type, out TokenValidator? validator))
            {
                string? tokenValue = validator.Invoke(ref text, index, out bool newlined);
                if (tokenValue != null)
                {
                    // found valid token
                    Token token = new(type, tokenValue, position);
                    parsedTokens.Add(token);
                }
                else
                {
                    errors.Add(new("Parse (String Terminal)", "Unexpected Token"));
                }
            }

            return parsedTokens;
        }
    }

}