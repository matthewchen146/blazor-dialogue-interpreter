
#nullable enable 

namespace Parser 
{
    public class ErrorInfo
    {
        public string type = "";
        public string message = "Error";
        public int lineIndex = 0;
        public int LineNumber
        {
            set
            {}
            get
            {
                return lineIndex + 1;
            }
        }
        public int columnIndex = 0;
        public int ColumnNumber
        {
            set
            {}
            get
            {
                return columnIndex + 1;
            }
        }
        public Token? token = null;
        public ErrorInfo(int _lineIndex, int _columnIndex, string _message)
        {
            lineIndex = _lineIndex;
            columnIndex = _columnIndex;
            message = _message;
        }

        public ErrorInfo(int _lineIndex, string _message)
        {
            lineIndex = _lineIndex;
            columnIndex = 0;
            message = _message;
        }

        public ErrorInfo(Token _token, string _message)
        {
            lineIndex = _token.lineIndex;
            columnIndex = _token.columnIndex;
            token = _token;
            message = _message;
        }

        public ErrorInfo(string _message)
        {
            message = _message;
        }

        public ErrorInfo(string _type, int _lineIndex, int _columnIndex, string _message)
        {
            type = _type;
            lineIndex = _lineIndex;
            columnIndex = _columnIndex;
            message = _message;
        }

        public ErrorInfo(string _type, int _lineIndex, string _message)
        {
            type = _type;
            lineIndex = _lineIndex;
            columnIndex = 0;
            message = _message;
        }

        public ErrorInfo(string _type, Token _token, string _message)
        {
            type = _type;
            lineIndex = _token.lineIndex;
            columnIndex = _token.columnIndex;
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
    }

    public class Token 
    {
        public string type = "";
        public string value = "";
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
        public Token(string _type, string _value, int _index, int _lineIndex, int _columnIndex)
        {
            type = _type;
            value = _value;
            index = _index;
            lineIndex = _lineIndex;
            columnIndex = _columnIndex;
        }
    }

    public delegate string? TokenValidator(ref string text, int startIndex, out int endIndex);

    // public class Term
    // {
    //     private Group? group;
    //     private readonly string groupNameOrTerminal = "";
    //     public Group? Group 
    //     {
    //         set
    //         {} 
    //         get
    //         {
    //             if (group == null)
    //             {
    //                 if (Parser.specification.TryGetValue(groupNameOrTerminal, out Group? _group))
    //                 {
    //                     group = _group;
    //                 }
    //             }
    //             return group;
    //         }
    //     }
    //     public string Value 
    //     {
    //         set
    //         {}
    //         get
    //         {
    //             return groupNameOrTerminal;
    //         }
    //     }
    //     public bool IsTerminal
    //     {
    //         set
    //         {}
    //         get
    //         {
    //             return Group == null;
    //         }
    //     }
    //     public bool repeatable = false;
    //     public bool optional = false;
    //     public Term(string _groupNameOrTerminal, bool _repeatable = false, bool _optional = false)
    //     {
    //         groupNameOrTerminal = _groupNameOrTerminal;
    //         repeatable = _repeatable;
    //         optional = _optional;
    //     }

    //     public Term(Group _group, bool _repeatable = false, bool _optional = false)
    //     {
    //         group = _group;
    //         repeatable = _repeatable;
    //         optional = _optional;
    //     }

    //     public Term()
    //     {

    //     }
    // }

    public static class Specification
    {
        // dictionary mapping symbol (type) to expression (group)
        // if the parser can't find a symbol in the dictionary, it will assume that it should check for exact match (keyword)
        public static Dictionary<string, Group> nonterminals = new() {
            {
                /* <program> ::= {<line>}* */
                "program", new Group(true, true, "line")
            },
            // {
            //     /* <line-wrapper> ::= <newline> <line> | <line> */
            //     "line-wrapper", new Group(new OrderedGroup("newline", "line"), "line", "newline")
            // },
            {
                /* <line> ::= 
                    | <line-content> {comment}
                    | <separator-newline> <line-content> {comment}
                    | <separator-newline> {comment}
                    | {comment}
                */
                "line", new Group(
                    new OrderedGroup(
                        new Group("line-content", new OrderedGroup("separator-newline", "line-content"), "separator-newline"), 
                        new Group(true, "comment")
                    ),
                    new Group(true, "comment")
                )
            },
            {
                "line-content", new Group("command-line", "text")
            },
            {
                /* <command-line> ::= '@' <command> */
                "command-line", new OrderedGroup("command-prefix", "command")
            },
            {
                /* <command> ::= 
                    | <conversation-command> 
                    | <enter-command>
                    | <speak-command>
                    | ... 
                */
                "command", new Group("conversation-command", "enter-command", "speak-command", "label-command", "jump-command", "option-command")
            },
            {
                /* <conversation-command> ::= 'conversation' <id> */
                "conversation-command", new OrderedGroup("conversation", "separator", "id")
            },
            {
                /* <conversation-command> ::= 'conversation' <id> */
                "enter-command", new OrderedGroup("enter", "separator", "id", new OrderedGroup(true, "separator", "string"))
            },
            {
                /* <speak-command> ::= 'speak' <string-or-id> 
                   
                   <string-or-id> ::= <string> | <id>
                */
                "speak-command", new OrderedGroup("speak", "separator", new Group("string", "id"))
            },
            {
                /* <conversation-command> ::= 'conversation' <id> */
                "label-command", new OrderedGroup("label", "separator", "id")
            },
            {
                /* <conversation-command> ::= 'conversation' <id> */
                "jump-command", new OrderedGroup("jump", "separator", "id")
            },
            {
                /* <conversation-command> ::= 'conversation' <id> */
                "option-command", new OrderedGroup("option", "separator", "id", "separator", "string")
            },
        };
    } 


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

        

        

        static TokenValidator CreateMatchExact(string toMatch)
        {
            return (ref string text, int startIndex, out int endIndex) => {
                endIndex = startIndex;
                int matchIndex = 0;

                while (startIndex < text.Length && matchIndex < toMatch.Length)
                {
                    // Console.WriteLine($"Matching {matchIndex} {text[startIndex]} - {toMatch[matchIndex]}");
                    if (text[startIndex] != toMatch[matchIndex])
                    {
                        return null;
                    }
                    matchIndex += 1;
                    startIndex += 1;
                }
                endIndex = startIndex;
                return toMatch;
            };
        }

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

        // static TokenValidator CreateMatchExactAny(IEnumerable<string> toMatchAny)
        // {
        //     TrieNode root = new();
        //     foreach (string s in toMatchAny)
        //     {
        //         root.Add(s);
        //     }

        //     return (ref string text, int startIndex, out int endIndex) => {
        //         endIndex = startIndex;
        //         int matchIndex = 0;
        //         while (startIndex < text.Length && matchIndex < toMatch.Length)
        //         {
        //             if (text[startIndex] != toMatch[matchIndex])
        //             {
        //                 return null;
        //             }
        //             matchIndex += 1;
        //             startIndex += 1;
        //         }
        //         endIndex = startIndex;
        //         return toMatch;
        //     };
        // }

        public Dictionary<string, TokenValidator> terminals = new()
        {
            {
                "newline", CreateMatchExact("\n")
            },
            {
                "separator-newline", (ref string text, int startIndex, out int endIndex) => {
                    
                    endIndex = startIndex;
                    string target = "";

                    if (text[startIndex] == '\n')
                    {
                        endIndex = startIndex + 1;
                        return "\n";
                    }

                    while (text[startIndex] != '\n' && startIndex < text.Length)
                    {
                        if (!separator.ContainsKey(text[startIndex]))
                        {
                            // if anything but space and tab is found
                            return null;
                        }

                        target += text[startIndex];
                        startIndex += 1;
                    }

                    endIndex = startIndex;
                    return target;
                }
            },
            {
                "separator", (ref string text, int startIndex, out int endIndex) => {
                    
                    endIndex = startIndex;
                    string target = "";

                    while (separator.ContainsKey(text[startIndex]) && startIndex < text.Length)
                    {
                        target += text[startIndex];
                        startIndex += 1;
                    }

                    endIndex = startIndex;
                    return target;
                }
            },
            {
                "command-prefix", CreateMatchExact("@")
            },
            {
                "comment", (ref string text, int startIndex, out int endIndex) => {
                    
                    endIndex = startIndex;
                    string target = "";

                    if (text[startIndex] != '/')
                    {
                        return null;
                    }
                    if (text[startIndex + 1] != '/')
                    {
                        return null;
                    }


                    while (text[startIndex] != '\n' && startIndex < text.Length)
                    {
                        target += text[startIndex];
                        startIndex += 1;
                    }

                    endIndex = startIndex;
                    return target;
                }
            },
            {
                "string", (ref string text, int startIndex, out int endIndex) => {
                    
                    endIndex = startIndex;
                    string target = "";

                    // check for " at the start
                    if (text[startIndex] != '"')
                    {
                        return null;
                    }

                    target += '"';

                    startIndex += 1;

                    while (text[startIndex] != '"' && text[startIndex] != '\n' && startIndex < text.Length)
                    {
                        target += text[startIndex];
                        startIndex += 1;
                    }

                    // no ending " found
                    if (startIndex == text.Length || text[startIndex] == '\n')
                    {
                        return null;
                    }

                    target += '"';

                    // skip last " for end index
                    endIndex = startIndex + 1;
                    return target;
                }
            },
            {
                "text", (ref string text, int startIndex, out int endIndex) => {
                    
                    endIndex = startIndex;
                    string target = "";

                    // text lines can only start on the beginning of the program, or after a new line
                    if (startIndex != 0)
                    {
                        if (text[startIndex - 1] != '\n')
                        {
                            return null;
                        }
                    }

                    while (text[startIndex] != '\n' && startIndex < text.Length && !(text[startIndex] == '/' && text[startIndex + 1] == '/')) //predict error here
                    {
                        target += text[startIndex];
                        startIndex += 1;
                    }

                    if (target.Length == 0)
                    {
                        return null;
                    }

                    endIndex = startIndex;
                    return target;
                }
            },
            {
                "conversation", CreateMatchExact("conversation")
            },
            {
                "enter", CreateMatchExact("enter")
            },
            {
                "speak", CreateMatchExact("speak")
            },
            {
                "label", CreateMatchExact("label")
            },
            {
                "jump", CreateMatchExact("jump")
            },
            {
                "option", CreateMatchExact("option")
            },
            {
                "id", (ref string text, int startIndex, out int endIndex) => {
                    
                    string target = "";

                    while (!whitespace.ContainsKey(text[startIndex]) && startIndex < text.Length && !(text[startIndex] == '/' && text[startIndex + 1] == '/'))
                    {
                        target += text[startIndex];
                        startIndex += 1;
                    }

                    endIndex = startIndex;
                    return target;
                }
            },
            {
                "word", (ref string text, int startIndex, out int endIndex) => {
                    
                    string target = "";

                    while (!whitespace.ContainsKey(text[startIndex]) && startIndex < text.Length && !(text[startIndex] == '/' && text[startIndex + 1] == '/'))
                    {
                        target += text[startIndex];
                        startIndex += 1;
                    }

                    endIndex = startIndex;
                    return target;
                }
            },
            
            
            
        };

        public string text = "";
        public int index = 0;
        private int newlineCount = 0;
        private int lineColumnCount = 0;
        public static readonly Dictionary<char, bool> whitespace = new() {{' ', true}, {'\t', true}, {'\n', true}};
        public static readonly Dictionary<char, bool> separator = new() {{' ', true}, {'\t', true}};

        void AdvanceIndex()
        {
            // check if passed newline
            if (text[index] == '\n')
            {
                newlineCount += 1;
                lineColumnCount = 0;
            }
            else
            {
                lineColumnCount += 1;
            }

            index += 1;
        }

        public Token? GetNextToken()//out Token? whitespaceToken)
        {
            // whitespaceToken = null;
            // string whitespaceValue = "";
            // int whitespaceStartIndex = index;
            // int whitespaceLineIndex = newlineCount;
            // int whitespaceColumnIndex = lineColumnCount;
            // skip white space
            while (!ReachedEnd() && whitespace.ContainsKey(text[index]))
            {
                // whitespaceValue += text[index];
                AdvanceIndex();
            }

            // if (whitespaceValue.Length > 0)
            // {
            //     whitespaceToken = new("whitespace", whitespaceValue, whitespaceStartIndex, whitespaceLineIndex, whitespaceColumnIndex);
            // }

            if (!HasMoreTokens())
            {
                Console.WriteLine("GetNextToken - no more tokens!!");
                return null;
            }

            // find terminal
            foreach (string key in terminals.Keys)
            {
                TokenValidator validator = terminals[key];
                string? value = validator.Invoke(ref text, index, out int endIndex);
                
                if (value == null)
                {
                    continue;
                }

                int startIndex = index;
                int startColumnCount = lineColumnCount;

                // index = endIndex;
                while (index < endIndex)
                {
                    AdvanceIndex();  
                }
                

                return new(key, value, startIndex, newlineCount, startColumnCount);
            }
            
            // if no matching terminal, just be null
            // Console.WriteLine("GetNextToken - no token found :[");
            return null;
        }

        bool ReachedEnd()
        {
            return index >= text.Length;
        }

        bool HasMoreTokens()
        {
            return !ReachedEnd();
        }
    
        Token? leadingToken = null;

        List<Token> parsedTokens = new();

        // public Token? Consume(Group group, int callstack = 0)
        // {

        //     // Console.WriteLine($"{callstack} Consuming {group}");

        //     if (group is OrderedGroup)
        //     {
        //         // OrderedGroup
        //         foreach (Term term in group.terms)
        //         {
        //             Token? consumedToken;
        //             // for each term, check for match, otherwise return null
        //             // must be matched in order, that is why it has to return null on fail
        //             // matches can be deep, so make sure state is saved here somehow?
        //             if (term.Group != null)
        //             {
        //                 consumedToken = Consume(term.Group, callstack + 1);
        //                 if (term.repeatable)
        //                 {
        //                     while (consumedToken != null)
        //                     {
        //                         // Console.WriteLine($"Consuming Repeatable {term.Group}");
        //                         consumedToken = Consume(term.Group, callstack + 1);
        //                     }
        //                 }
        //             }
        //             else
        //             {
        //                 consumedToken = Consume(term.Value, callstack + 1);
        //                 if (term.repeatable)
        //                 {
        //                     while (consumedToken != null)
        //                     {
        //                         // Console.WriteLine($"Consuming Repeatable <{term.Value}>");
        //                         consumedToken = Consume(term.Value, callstack + 1);
        //                     }
        //                 }
        //             }

        //             if (term.optional)
        //             {
        //                 continue;
        //             }

        //             if (!term.repeatable && consumedToken == null)
        //             {
        //                 return null;
        //             }
        //         }

        //         return leadingToken;
        //     }
        //     else
        //     {
        //         // OrGroup aka Group
        //         Token? consumedToken = null;

        //         foreach (Term term in group.terms)
        //         {
        //             // for each term, check for match, otherwise continue
        //             // matches can be deep, so make sure state is saved here somehow?
        //             if (term.Group != null)
        //             {
        //                 consumedToken = Consume(term.Group, callstack + 1);
        //                 if (term.repeatable)
        //                 {
        //                     while (consumedToken != null)
        //                     {
        //                         // Console.WriteLine($"Consuming Repeatable <{term.Group}>");
        //                         consumedToken = Consume(term.Group, callstack + 1);
        //                     }
        //                 }
        //             }
        //             else
        //             {
        //                 consumedToken = Consume(term.Value, callstack + 1);
        //                 if (term.repeatable)
        //                 {
        //                     while (consumedToken != null)
        //                     {
        //                         // Console.WriteLine($"Consuming Repeatable <{term.Value}>");
        //                         consumedToken = Consume(term.Value, callstack + 1);
        //                     }
        //                 }
        //             }

        //             if (consumedToken != null)
        //             {
        //                 break;
        //             }
        //         }

        //         return consumedToken;
        //     }
        // }

        // public Token? Consume(string groupNameOrTerminal, int callstack = 0)
        // {

        //     if (leadingToken == null)
        //     {
        //         Console.WriteLine("Error no predicted token");
        //         return null;
        //     }

        //     // Console.WriteLine($"{callstack} Consuming <{groupNameOrTerminal}> - predicted: <{leadingToken.type}>");

        //     if (Terminals.ContainsKey(groupNameOrTerminal))
        //     {
        //         // string? value = Terminals[groupNameOrTerminal].Invoke(ref tokenizer.text, tokenizer.index, out int endIndex);
        //         if (groupNameOrTerminal == leadingToken.type)
        //         {
        //             // Console.WriteLine($"found token match [{leadingToken.type}] [{leadingToken.value}]");
        //             Token token = leadingToken;

        //             leadingToken = tokenizer.GetNextToken();//out Token? whitespaceToken);

        //             // if (whitespaceToken != null)
        //             // {
        //             //     parsedTokens.Add(whitespaceToken);
        //             // }

        //             parsedTokens.Add(token);

        //             return token;
        //         }
        //         // return tokenizer.GetNextToken(); // or something that means true??
        //         return null;
        //     }

        //     if (Tokenizer.specification.TryGetValue(groupNameOrTerminal, out Group? group))
        //     {
        //         return Consume(group, callstack + 1);
        //     }

        //     return null;
        // }

        public List<Token> Parse(ref string text, List<ErrorInfo> errors, Group group, int index = 0)
        {
            if (Specification.nonterminals.TryGetValue(group.value, out Group? specGroup))
            {
                specGroup.value = group.value;
                group = specGroup;
            }

            // Console.WriteLine($"prediction - {group.value}");

            List<Token> parsedTokens = new();

            if (group.children.Count == 0)
            {
                // check terminal
                if (terminals.TryGetValue(group.value, out TokenValidator? validator))
                {
                    string? tokenValue = validator.Invoke(ref text, index, out int endIndex);
                    if (tokenValue != null)
                    {
                        // found valid token
                        Token token = new(group.value, tokenValue, index, 0, 0);
                        parsedTokens.Add(token);
                    }
                }
                string success = parsedTokens.Count > 0 ? $"SCSS [{parsedTokens[0].value}]" : "FAIL";
                Console.WriteLine($"checking {group.value} - {success}");
                return parsedTokens;
            }
           

            for (int iter = 0; ((iter < 1 && !group.repeatable) || group.repeatable) && iter < 200; iter++)
            {
                
                int nextIndex = parsedTokens.Count > 0 ? parsedTokens[^1].index + parsedTokens[^1].value.Length : index;
                if (parsedTokens.Count > 0)
                {
                    Console.WriteLine($"next index: [{nextIndex}] last token: [{parsedTokens[^1].value}]");
                }
                

                if (nextIndex >= text.Length)
                {
                    return parsedTokens;
                }

                // check group
                if (group is OrderedGroup)
                {
                    List<Token> groupTokens = new();

                    // check all success
                    foreach (Group child in group.children)
                    {
                        int groupNextIndex = nextIndex;
                        if (groupTokens.Count > 0)
                        {
                            groupNextIndex = groupTokens[^1].index + groupTokens[^1].value.Length;
                        }

                        List<Token> resultingTokens = Parse(ref text, errors, child, groupNextIndex);

                        if (resultingTokens.Count == 0 && !child.optional)
                        {
                            break;
                        }

                        groupTokens.AddRange(resultingTokens);
                    }

                    parsedTokens.AddRange(groupTokens);
                    
                }
                else
                {
                    // check one success
                    foreach (Group child in group.children)
                    {
                        List<Token> resultingTokens = Parse(ref text, errors, child, nextIndex);

                        if (resultingTokens.Count > 0)
                        {
                            parsedTokens.AddRange(resultingTokens);
                            break;
                        }
                    }
                }
            }

            return parsedTokens;
        }

        public List<Token> Parse(ref string text, List<ErrorInfo> errors, string type, int index = 0)
        {
            if (Specification.nonterminals.TryGetValue(type, out Group? group))
            {
                
                group.value = type;
                return Parse(ref text, errors, group, index);
                
            }

            List<Token> parsedTokens = new();

            if (index == text.Length)
            {
                return parsedTokens;
            }
            
            // check terminal
            if (terminals.TryGetValue(type, out TokenValidator? validator))
            {
                string? tokenValue = validator.Invoke(ref text, index, out int endIndex);
                if (tokenValue != null)
                {
                    // found valid token
                    Token token = new(type, tokenValue, index, 0, 0);
                    parsedTokens.Add(token);
                }
            }

            return parsedTokens;
        }

        // public List<Token> Parse(string text, out string error, string type = "program")
        // {
        //     error = "";

        //     Console.WriteLine("parsing");
        //     // tokenizer = new(text);

        //     parsedTokens = new();

        //     // leadingToken = tokenizer.GetNextToken();//out Token? whitespaceToken);

        //     // if (whitespaceToken != null)
        //     // {
        //     //     parsedTokens.Add(whitespaceToken);
        //     // }

        //     // if (leadingToken == null)
        //     // {
        //     //     return parsedTokens;
        //     // }

        //     // test for getting all tokens

        //     // int iter = 0;
        //     // while (leadingToken != null && iter < 100)
        //     // {
        //     //     Console.WriteLine($"token: TYPE: [{leadingToken.type}], VALUE: [{leadingToken.value}]");
        //     //     leadingToken = tokenizer.GetNextToken();
        //     //     iter++;
        //     // }

        //     // int iter = 0;
        //     // Token? consumedToken = Consume(type);
        //     // if (consumedToken != null)
        //     // {
        //     //     Console.WriteLine($"Successfully consumed [{type}]");
        //     // }
        //     // else
        //     // {
        //     //     if (leadingToken != null)
        //     //     {
        //     //         error = $"Expected token [{leadingToken.type}]";
        //     //     }
        //     //     else
        //     //     {
        //     //         error = $"Failed to consume [{type}]";
        //     //     }
                
        //     // }

        //     return parsedTokens;



        //     // while (consumedToken != null && iter < 200)
        //     // {
        //     //     Console.WriteLine($"consumedToken: TYPE: [{consumedToken.type}], VALUE: [{consumedToken.value}]");
        //     //     consumedToken = Consume("program");
        //     //     iter++;
        //     // }
            


        //     // return program : command | text | comment
        //     // Token? returnedToken = Eat(leadingToken.type);
        //     // if (returnedToken != null)
        //     // {
        //     //     Console.WriteLine($"returned token - [{returnedToken.type}] [{returnedToken.value}]");
        //     // }

        //     // while (tokenizer.GetNextToken(out Token? token) != null)
        //     // {
        //     //     if (token != null)
        //     //     {
        //     //         Console.WriteLine($"TOKEN - TYPE: [{token.type}], VALUE: [{token.value}]");
        //     //     }
        //     //     else
        //     //     {
        //     //         Console.WriteLine($"TOKEN - NULL");
        //     //     }
        //     // }
            
        // }
    }

    

}