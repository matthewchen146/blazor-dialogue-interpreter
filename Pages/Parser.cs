
#nullable enable
class Parser 
{

    public class Tokenizer 
    {

        public class Token 
        {
            public string type = "";
            public object? value = null;
            public Token(string _type, object _value)
            {
                type = _type;
                value = _value;
            }
        }

        public delegate string? TokenValidator(ref string text, int startIndex, out int endIndex);

        public class Term
        {
            private object? _value;
            public Group? Value {
                set
                {
                    _value = value;
                } 
                get
                {
                    if (_value is string)
                    {
                        if (specification.ContainsKey((string)_value))
                        {
                            return specification[(string)_value];
                        }                        
                    }
                    else if (_value is Group)
                    {
                        return (Group)_value;
                    }
                    return null;
                }
            }
            public bool repeatable = false;
            public bool optional = false;
            public Term(string term, bool _repeatable = false, bool _optional = false)
            {
                _value = term;
                repeatable = _repeatable;
                optional = _optional;
            }

            public Term(Group group, bool _repeatable = false, bool _optional = false)
            {
                _value = group;
                repeatable = _repeatable;
                optional = _optional;
            }
        }

        // in BNF terms: an expression
        public class Group 
        {
            public TokenValidator? validator = null;
            public List<Term> terms = new();
            public bool isTerminal = false;

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
        }

        public class EmptyGroup : Group
        {

        }

        // public class AnyGroup : Group
        // {
        //     public AnyGroup(IEnumerable<string> _children) : base(_children)
        //     {

        //     }
        // }

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
                /* <program> ::= {<line-wrapper>}* */
                "program", new Group(new Term("line-wrapper", true, true))
            },
            {
                /* <line-wrapper> ::= 
                    | '\n' <line> 
                    | <line> 
                */
                "line-wrapper", new Group(new OrderedGroup("\n", "line"), new OrderedGroup("line"))
            },
            {
                /* <line> ::= 
                    | <comment> 
                    | <command-line> {<comment>} 
                    | <text-line> {<comment>}
                */
                "line", new Group(new OrderedGroup(new Group("command-line", "text-line"), new Term("comment", false, true)), "comment")
            },
            {
                /* <command-line> ::= '@' <command> */
                "command-line", new OrderedGroup("@", "command")
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
            }
        };

        static TokenValidator CreateMatchExact(string toMatch)
        {
            return (ref string text, int startIndex, out int endIndex) => {
                endIndex = startIndex;
                int matchIndex = 0;
                while (!whitespace.ContainsKey(text[startIndex]) && startIndex < text.Length && startIndex < toMatch.Length)
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

        public Dictionary<string, TokenValidator> terminals = new()
        {
            {
                "\n", CreateMatchExact("\n")
            },
            {
                "@", CreateMatchExact("@")
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
                "id", (ref string text, int startIndex, out int endIndex) => {
                    
                    string target = "";

                    while (!whitespace.ContainsKey(text[startIndex]) && startIndex < text.Length)
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

                    while (!whitespace.ContainsKey(text[startIndex]) && startIndex < text.Length)
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

                    while (text[startIndex] != '"' && startIndex < text.Length)
                    {
                        target += text[startIndex];
                        startIndex += 1;
                    }

                    // no ending " found
                    if (startIndex == text.Length)
                    {
                        return null;
                    }

                    // skip last " for end index
                    endIndex = startIndex + 1;
                    return target;
                }
            },
            {
                "text-line", (ref string text, int startIndex, out int endIndex) => {
                    
                    endIndex = startIndex;
                    string target = "";

                    while (text[startIndex] != '\n' && startIndex < text.Length)
                    {
                        target += text[startIndex];
                        startIndex += 1;
                    }

                    endIndex = startIndex;
                    return target;
                }
            }
            
        };

        string text = "";
        int index = 0;
        bool justNewlined = true;
        static readonly Dictionary<char, bool> whitespace = new() {{' ', true}, {'\n', true}, {'\t', true}};
        public Tokenizer(string _text)
        {
            text = _text;
        }

        public Token? GetNextToken()
        {
            // skip white space
            while (!ReachedEnd() && whitespace.ContainsKey(text[index]))
            {
                index += 1;
            }

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

                index = endIndex;

                return new(key, value);
            }
            
            // if no matching terminal, just be null
            Console.WriteLine("GetNextToken - no token found :[");
            return null;
        }

        // public Token? GetNextToken(out Token? token)
        // {
            

        //     // skip white space
        //     while (!ReachedEnd() && whitespace.Contains(text[index]))
        //     {
        //         if (text[index] == '\n')
        //         {
        //             justNewlined = true;
        //         }
        //         index += 1;
        //     }

        //     token = null;
        //     if (!HasMoreTokens())
        //     {
        //         return null;
        //     }


        //     // string target = "";
          
        //     foreach (string type in specification.Keys)
        //     {
        //         Group group = specification[type];
        //         string? subtype = ResolveGroup(group);
        //         if (subtype != null)
        //         {

        //             string? value = validator(ref text, ref index);
        //             token = new(type, value);
        //         }
        //         if (group.children.Count > 0)
        //         {
        //             // for each type in the group, get group
        //             foreach (string subtype in group.children)
        //             {
        //                 Group subgroup = specification[subtype];
        //             }
        //         }
                
        //         string? value = validator(ref text, ref index);
        //         if (value != null)
        //         {
        //             token = new(type, value);
        //             return token;
        //         }

        //     }

        //     // // command
        //     // if (text[index] == '@')
        //     // {
        //     //     // int endIndex = text.IndexOf(' ', index + 1);
        //     //     index += 1;
        //     //     while (!whitespace.Contains(text[index]) && !ReachedEnd())
        //     //     {
        //     //         target += text[index];
        //     //         // Console.WriteLine($"current target {target}, char {text[index]}");
        //     //         index += 1;
        //     //     }
        //     //     justNewlined = false;
        //     //     token = new("command", target);
        //     //     return token;
        //     // }

        //     // // comment
        //     // if (text[index] == '/')
        //     // {
        //     //     if (text[index + 1] == '/')
        //     //     {
        //     //         while (text[index] != '\n')
        //     //         {
        //     //             target += text[index];
        //     //             index += 1;
        //     //         }
        //     //         justNewlined = false;
        //     //         token = new("comment", target);
        //     //         return token;
        //     //     }
        //     // }
            
        //     // // text
        //     // if (index == 0 || justNewlined)//text[index - 1] == '\n')
        //     // {
        //     //     while (text[index] != '\n')
        //     //     {
        //     //         target += text[index];
        //     //         index += 1;
        //     //     }

        //     //     // also check if target does not only consist of white space?
        //     //     if (target.Length > 0)
        //     //     {
        //     //         justNewlined = false;
        //     //         token = new("text", target);
        //     //         return token;
        //     //     }
                
        //     // }

        //     // skip white space?
        //     string target = "" + text[index];
        //     index += 1;

        //     if (token == null) 
        //     {
        //         justNewlined = false;
        //     }

        //     token = new("unknown", target);
        //     return token;
        // }

        // public Token? GetNextToken()
        // {
        //     return GetNextToken(out Token _);
        // }

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
    // Tokenizer.Token? predictedToken;
    Tokenizer.Token? predictedToken = null;

    // public Tokenizer.Token? Eat(string tokenType)
    // {
    //     if (predictedToken == null)
    //     {
    //         Console.WriteLine($"Unexpected end, expected a {tokenType}");
    //         return null;
    //     }

    //     Tokenizer.Token token = predictedToken;

    //     if (token.type != tokenType)
    //     {
    //         Console.WriteLine($"Unexpected {predictedToken.type} ({predictedToken.value}), expected a {tokenType}");
    //         return null;
    //     }

    //     predictedToken = tokenizer.GetNextToken();

    //     return token;
    // }

    public void Parse(string text)
    {
        Console.WriteLine("parsing");
        tokenizer = new(text);

        predictedToken = tokenizer.GetNextToken();

        if (predictedToken == null)
        {
            return;
        }

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