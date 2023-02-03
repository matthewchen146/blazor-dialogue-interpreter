
#nullable enable
namespace Parser
{
    public static class Specification
    {
        public delegate int CharValidator(ref string text, int index);
        public static readonly Dictionary<char, bool> whitespace = new() {{' ', true}, {'\t', true}, {'\n', true}};
        public static readonly Dictionary<char, bool> separator = new() {{' ', true}, {'\t', true}};
        public static readonly Dictionary<char, bool> idCharacters = new();

        static Specification()
        {
            string idChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

            foreach (char c in idChars)
            {
                if (!idCharacters.ContainsKey(c))
                {
                    idCharacters.Add(c, true);
                }
            }
        }

        // dictionary mapping symbol (type) to expression (group)
        // if the parser can't find a symbol in the dictionary, it will assume that it should check for exact match (keyword)
        public static Dictionary<string, Group> nonterminals = new() {
            {
                /* <program> ::= {<line>}* */
                "program", new Group(true, true, "line")
            },
            // {
            //     /* <line-wrapper> ::= <line>{<comment>} */
            //     "line-wrapper", new OrderedGroup(new Group(true, "separator-newline"), "line", new Group(true, "comment"))
            // },
            {
                /* <line> ::= 
                    | <line-content>{comment}
                    | <separator-newline><line-content>{comment}
                    | <separator-newline>{comment}
                    | {comment}
                */
                "line", new Group(
                    "separator-newline",
                    
                    new OrderedGroup(
                        "line-content",
                        // new Group("line-content", new OrderedGroup("separator-newline", "line-content"), "separator-newline"),
                        new Group(true, "separator"),
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
                "command", new Group()//"conversation-command", "enter-command", "speak-command", "label-command", "jump-command", "option-command")
            },
            {
                "separator-newline", new OrderedGroup(new Group(true, "separator"), "newline")
            },
            // {
            //     "expression", new Group(
            //         new OrderedGroup("left-parenthesis", new Group(true, "separator"), "expression-content", new Group(true, "separator"), "right-parenthesis"), 
            //         "expression-content"
            //     )
            // },
            // {
            //     "expression-content", new OrderedGroup(new Group("expression", "float"), new Group(true, "separator"), "operator", new Group(true, "separator"), new Group("expression", "float"))
            // }
            // {
            //     /* <conversation-command> ::= 'conversation' <id> */
            //     "conversation-command", new OrderedGroup("conversation", "separator", "id")
            // },
            // {
            //     /* <conversation-command> ::= 'conversation' <id> */
            //     "enter-command", new OrderedGroup("enter", "separator", "id", new OrderedGroup(true, "separator", "string"))
            // },
            // {
            //     /* <speak-command> ::= 'speak' <string-or-id> 
                   
            //        <string-or-id> ::= <string> | <id>
            //     */
            //     "speak-command", new OrderedGroup("speak", "separator", new Group("string", "id"))
            // },
            // {
            //     /* <conversation-command> ::= 'conversation' <id> */
            //     "label-command", new OrderedGroup("label", "separator", "id")
            // },
            // {
            //     /* <conversation-command> ::= 'conversation' <id> */
            //     "jump-command", new OrderedGroup("jump", "separator", "id")
            // },
            // {
            //     /* <conversation-command> ::= 'conversation' <id> */
            //     "option-command", new OrderedGroup("option", "separator", "id", "separator", "string")
            // },
            
        };

        public static Dictionary<string, TokenValidator> terminals = new()
        {
            {
                "newline", CreateMatchExact("\n", true)
            },
            {
                "separator", (ref string text, int startIndex, out bool newlined) => {
                    newlined = false;
                    
                    string target = "";

                    while (startIndex < text.Length && separator.ContainsKey(text[startIndex]))
                    {
                        target += text[startIndex];
                        startIndex += 1;
                    }

                    if (target.Length == 0)
                    {
                        return null;
                    }

                    
                    return target;
                }
            },
            {
                "command-prefix", CreateMatchExact("@")
            },
            // {
            //     "left-parenthesis", CreateMatchExact("(")
            // },
            // {
            //     "right-parenthesis", CreateMatchExact(")")
            // },
            {
                "comment", (ref string text, int startIndex, out bool newlined) => {
                    newlined = false;
                    
                    string target = "";

                    if (text[startIndex] != '/')
                    {
                        return null;
                    }
                    if (text[startIndex + 1] != '/')
                    {
                        return null;
                    }


                    while (startIndex < text.Length && text[startIndex] != '\n')
                    {
                        target += text[startIndex];
                        startIndex += 1;
                    }

                    
                    return target;
                }
            },
            {
                "string", (ref string text, int startIndex, out bool newlined) => {
                    newlined = false;
                    
                    string target = "";

                    // check for " at the start
                    if (text[startIndex] != '"')
                    {
                        return null;
                    }

                    target += '"';

                    startIndex += 1;

                    while (startIndex < text.Length && text[startIndex] != '"' && text[startIndex] != '\n')
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
                    // endIndex = startIndex + 1;
                    return target;
                }
            },
            {
                "text", (ref string text, int startIndex, out bool newlined) => {
                    newlined = false;
                    
                    string target = "";

                    // text lines cant start with @
                    if (text[startIndex] == '@')
                    {
                        return null;
                    }

                    // text lines can only start on the beginning of the program, or after a new line
                    if (startIndex > 0)
                    {
                        if (text[startIndex - 1] != '\n')
                        {
                            return null;
                        }
                    }

                    // break text at newline or comment
                    while (startIndex < text.Length && text[startIndex] != '\n' && !(text[startIndex] == '/' && text[startIndex + 1] == '/')) //predict error here
                    {
                        target += text[startIndex];
                        startIndex += 1;
                    }

                    if (target.Length == 0)
                    {
                        return null;
                    }

                    
                    return target;
                }
            },
            // {
            //     "conversation", CreateMatchExact("conversation")
            // },
            // {
            //     "enter", CreateMatchExact("enter")
            // },
            // {
            //     "speak", CreateMatchExact("speak")
            // },
            // {
            //     "label", CreateMatchExact("label")
            // },
            // {
            //     "jump", CreateMatchExact("jump")
            // },
            // {
            //     "option", CreateMatchExact("option")
            // },
            
            {
                "float", (ref string text, int startIndex, out bool newlined) => {
                    newlined = false;

                    string target = "";
                    while (startIndex < text.Length && !whitespace.ContainsKey(text[startIndex]) && !(text[startIndex] == '/' && text[startIndex + 1] == '/'))
                    {
                        target += text[startIndex];
                        startIndex++;
                    }

                    if (float.TryParse(target, System.Globalization.NumberStyles.Number, null, out float result))
                    {
                        return target;
                    }
                    
                    return null;
                }
            },
            {
                "operator", (ref string text, int startIndex, out bool newlined) => {
                    newlined = false;
                    
                    if ("+-*/".Contains(text[startIndex]))
                    {
                        return text[startIndex].ToString();
                    }

                    return null;
                }
            },
            {
                "id", (ref string text, int startIndex, out bool newlined) => {
                    newlined = false;
                    string target = "";
                    
                    if ("0123456789".Contains(text[startIndex]))
                    {
                        return null;
                    }

                    while (
                        startIndex < text.Length && !whitespace.ContainsKey(text[startIndex]) && !(text[startIndex] == '/' && text[startIndex + 1] == '/'))
                    {
                        if (!idCharacters.ContainsKey(text[startIndex]))
                        {
                            return null;
                        }

                        target += text[startIndex];
                        startIndex += 1;
                    }

                    if (target.Length == 0)
                    {
                        return null;
                    }
                    
                    return target;
                }
            },
            {
                "word", (ref string text, int startIndex, out bool newlined) => {
                    newlined = false;
                    string target = "";

                    while (startIndex < text.Length && !whitespace.ContainsKey(text[startIndex]) && !(text[startIndex] == '/' && text[startIndex + 1] == '/'))
                    {
                        target += text[startIndex];
                        startIndex += 1;
                    }

                    if (target.Length == 0)
                    {
                        return null;
                    }

                    
                    return target;
                }
            },
            
            
            
        };

        public static TokenValidator CreateMatchExact(string toMatch, bool containsNewline = false)
        {
            return (ref string text, int startIndex, out bool newlined) => {
                newlined = containsNewline;
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
                
                return toMatch;
            };
        }

        static TokenValidator CreateMatchCharacters(string validChars, string endChars)
        {
            Dictionary<char, bool> validCharacters = new();
            Dictionary<char, bool> endCharacters = new();

            bool containsNewline = false;

            foreach (char c in validChars)
            {
                if (!validCharacters.ContainsKey(c))
                {
                    validCharacters.Add(c, true);
                }
                if (c == '\n')
                {
                    containsNewline = true;
                }
            }

            foreach (char c in endChars)
            {
                if (!endCharacters.ContainsKey(c))
                {
                    endCharacters.Add(c, true);
                }
            }

            return (ref string text, int startIndex, out bool newlined) => {
                newlined = containsNewline;

                string target = "";

                while (startIndex < text.Length && !endCharacters.ContainsKey(text[startIndex]))
                {
                    // Console.WriteLine($"Matching {matchIndex} {text[startIndex]} - {toMatch[matchIndex]}");
                    if (!validCharacters.ContainsKey(text[startIndex]))
                    {
                        return null;
                    }
                    target += text[startIndex];
                    startIndex += 1;
                }
                
                return target;
            };
        }
    } 
}