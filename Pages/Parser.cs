
#nullable enable
public class Parser 
{

    public class Tokenizer 
    {

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

        public class Term
        {
            private Group? group;
            private readonly string groupNameOrTerminal = "";
            public Group? Group 
            {
                set
                {} 
                get
                {
                    if (group == null)
                    {
                        if (specification.TryGetValue(groupNameOrTerminal, out Group? _group))
                        {
                            group = _group;
                        }
                    }
                    return group;
                }
            }
            public string Value 
            {
                set
                {}
                get
                {
                    return groupNameOrTerminal;
                }
            }
            public bool IsTerminal
            {
                set
                {}
                get
                {
                    return Group == null;
                }
            }
            public bool repeatable = false;
            public bool optional = false;
            public Term(string _groupNameOrTerminal, bool _repeatable = false, bool _optional = false)
            {
                groupNameOrTerminal = _groupNameOrTerminal;
                repeatable = _repeatable;
                optional = _optional;
            }

            public Term(Group _group, bool _repeatable = false, bool _optional = false)
            {
                group = _group;
                repeatable = _repeatable;
                optional = _optional;
            }

            public Term()
            {

            }
        }

        // in BNF terms: an expression
        public class Group 
        {
            public List<Term> terms = new();

            public Group(params object[] _terms)
            {
                foreach (object term in _terms)
                {
                    if (term is string)
                    {
                        terms.Add(new((string)term));
                    }
                    else if (term is Term)
                    {
                        terms.Add((Term)term);
                    } else if (term is Group)
                    {
                        terms.Add(new((Group)term));
                    }
                }
            }

            public override string ToString()
            {
                string s = this is OrderedGroup ? "ORD[" : "GRP[ ";
                foreach (Term term in terms)
                {
                    if (term.Group != null)
                    {
                        if (term.Value.Length == 0)
                        {
                            s += $"{term.Group}, ";
                        }
                        
                    }
                    else
                    {
                        s += $"<{term.Value}>, ";
                    }
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

        // dictionary mapping symbol (type) to expression (group)
        // if the parser can't find a symbol in the dictionary, it will assume that it should check for exact match (keyword)
        public static Dictionary<string, Group> specification = new() {
            {
                /* <program> ::= {<line>}* */
                "program", new Group(new Term("line", true, true))
            },
            // {
            //     /* <line-wrapper> ::= <newline> <line> | <line> */
            //     "line-wrapper", new Group(new OrderedGroup("newline", "line"), "line", "newline")
            // },
            {
                /* <line> ::= 
                    | <comment> 
                    | <command-line> {<comment>} 
                    | <text> {<comment>}
                */
                "line", new Group("comment", new OrderedGroup(new Group("command-line", "text"), new Term("comment", false, true)))
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
                "conversation-command", new OrderedGroup("conversation", "id")
            },
            {
                /* <conversation-command> ::= 'conversation' <id> */
                "enter-command", new OrderedGroup("enter", new Term("id", true))
            },
            {
                /* <conversation-command> ::= 'conversation' <id> */
                "speak-command", new OrderedGroup("speak", "id")
            },
            {
                /* <conversation-command> ::= 'conversation' <id> */
                "label-command", new OrderedGroup("label", "id")
            },
            {
                /* <conversation-command> ::= 'conversation' <id> */
                "jump-command", new OrderedGroup("jump", "id")
            },
            {
                /* <conversation-command> ::= 'conversation' <id> */
                "option-command", new OrderedGroup("option", "id", "string")
            },
        };

        static TokenValidator CreateMatchExact(string toMatch)
        {
            return (ref string text, int startIndex, out int endIndex) => {
                endIndex = startIndex;
                int matchIndex = 0;
                while (startIndex < text.Length && matchIndex < toMatch.Length)
                {
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

                    endIndex = startIndex;
                    return target;
                }
            },
            {
                "conversation", CreateMatchExact("conversation ")
            },
            {
                "enter", CreateMatchExact("enter ")
            },
            {
                "speak", CreateMatchExact("speak ")
            },
            {
                "label", CreateMatchExact("label ")
            },
            {
                "jump", CreateMatchExact("jump ")
            },
            {
                "option", CreateMatchExact("option ")
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
        public Tokenizer(string _text)
        {
            text = _text;
        }

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

    }

    Tokenizer tokenizer = new("");
    
    Tokenizer.Token? predictedToken = null;

    List<Tokenizer.Token> parsedTokens = new();

    public Tokenizer.Token? Consume(Tokenizer.Group group, int callstack = 0)
    {

        // Console.WriteLine($"{callstack} Consuming {group}");

        if (group is Tokenizer.OrderedGroup)
        {
            // OrderedGroup
            foreach (Tokenizer.Term term in group.terms)
            {
                Tokenizer.Token? consumedToken;
                // for each term, check for match, otherwise return null
                // must be matched in order, that is why it has to return null on fail
                // matches can be deep, so make sure state is saved here somehow?
                if (term.Group != null)
                {
                    consumedToken = Consume(term.Group, callstack + 1);
                    if (term.repeatable)
                    {
                        while (consumedToken != null)
                        {
                            // Console.WriteLine($"Consuming Repeatable {term.Group}");
                            consumedToken = Consume(term.Group, callstack + 1);
                        }
                    }
                }
                else
                {
                    consumedToken = Consume(term.Value, callstack + 1);
                    if (term.repeatable)
                    {
                        while (consumedToken != null)
                        {
                            // Console.WriteLine($"Consuming Repeatable <{term.Value}>");
                            consumedToken = Consume(term.Value, callstack + 1);
                        }
                    }
                }

                if (term.optional)
                {
                    continue;
                }

                if (!term.repeatable && consumedToken == null)
                {
                    return null;
                }
            }

            return predictedToken;
        }
        else
        {
            // OrGroup aka Group
            Tokenizer.Token? consumedToken = null;

            foreach (Tokenizer.Term term in group.terms)
            {
                // for each term, check for match, otherwise continue
                // matches can be deep, so make sure state is saved here somehow?
                if (term.Group != null)
                {
                    consumedToken = Consume(term.Group, callstack + 1);
                    if (term.repeatable)
                    {
                        while (consumedToken != null)
                        {
                            // Console.WriteLine($"Consuming Repeatable <{term.Group}>");
                            consumedToken = Consume(term.Group, callstack + 1);
                        }
                    }
                }
                else
                {
                    consumedToken = Consume(term.Value, callstack + 1);
                    if (term.repeatable)
                    {
                        while (consumedToken != null)
                        {
                            // Console.WriteLine($"Consuming Repeatable <{term.Value}>");
                            consumedToken = Consume(term.Value, callstack + 1);
                        }
                    }
                }

                if (consumedToken != null)
                {
                    break;
                }
            }

            return consumedToken;
        }
    }

    public Tokenizer.Token? Consume(string groupNameOrTerminal, int callstack = 0)
    {

        if (predictedToken == null)
        {
            Console.WriteLine("Error no predicted token");
            return null;
        }

        // Console.WriteLine($"{callstack} Consuming <{groupNameOrTerminal}> - predicted: <{predictedToken.type}>");

        if (tokenizer.terminals.ContainsKey(groupNameOrTerminal))
        {
            // string? value = tokenizer.terminals[groupNameOrTerminal].Invoke(ref tokenizer.text, tokenizer.index, out int endIndex);
            if (groupNameOrTerminal == predictedToken.type)
            {
                // Console.WriteLine($"found token match [{predictedToken.type}] [{predictedToken.value}]");
                Tokenizer.Token token = predictedToken;

                predictedToken = tokenizer.GetNextToken();//out Tokenizer.Token? whitespaceToken);

                // if (whitespaceToken != null)
                // {
                //     parsedTokens.Add(whitespaceToken);
                // }

                parsedTokens.Add(token);

                return token;
            }
            // return tokenizer.GetNextToken(); // or something that means true??
            return null;
        }

        if (Tokenizer.specification.TryGetValue(groupNameOrTerminal, out Tokenizer.Group? group))
        {
            return Consume(group, callstack + 1);
        }

        return null;
    }

    public List<Tokenizer.Token> Parse(string text, string type = "program")
    {
        Console.WriteLine("parsing");
        tokenizer = new(text);

        parsedTokens = new();

        predictedToken = tokenizer.GetNextToken();//out Tokenizer.Token? whitespaceToken);

        // if (whitespaceToken != null)
        // {
        //     parsedTokens.Add(whitespaceToken);
        // }

        if (predictedToken == null)
        {
            return parsedTokens;
        }

        // test for getting all tokens

        // int iter = 0;
        // while (predictedToken != null && iter < 100)
        // {
        //     Console.WriteLine($"token: TYPE: [{predictedToken.type}], VALUE: [{predictedToken.value}]");
        //     predictedToken = tokenizer.GetNextToken();
        //     iter++;
        // }

        // int iter = 0;
        Tokenizer.Token? consumedToken = Consume(type);
        if (consumedToken != null)
        {
            Console.WriteLine($"Successfully consumed [{type}]");
        }

        return parsedTokens;



        // while (consumedToken != null && iter < 200)
        // {
        //     Console.WriteLine($"consumedToken: TYPE: [{consumedToken.type}], VALUE: [{consumedToken.value}]");
        //     consumedToken = Consume("program");
        //     iter++;
        // }
        


        // return program : command | text | comment
        // Tokenizer.Token? returnedToken = Eat(predictedToken.type);
        // if (returnedToken != null)
        // {
        //     Console.WriteLine($"returned token - [{returnedToken.type}] [{returnedToken.value}]");
        // }

        // while (tokenizer.GetNextToken(out Tokenizer.Token? token) != null)
        // {
        //     if (token != null)
        //     {
        //         Console.WriteLine($"TOKEN - TYPE: [{token.type}], VALUE: [{token.value}]");
        //     }
        //     else
        //     {
        //         Console.WriteLine($"TOKEN - NULL");
        //     }
        // }
        
    }

}