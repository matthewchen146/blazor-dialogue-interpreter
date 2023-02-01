
#nullable enable
namespace Parser
{
    public static class Specification
    {
        public static readonly Dictionary<char, bool> whitespace = new() {{' ', true}, {'\t', true}, {'\n', true}};
        public static readonly Dictionary<char, bool> separator = new() {{' ', true}, {'\t', true}};

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
                        new Group("separator"),
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
            {
                "separator-newline", new OrderedGroup(new Group(true, "separator"), "newline")
            }
        };

        public static Dictionary<string, TokenValidator> terminals = new()
        {
            {
                "newline", CreateMatchExact("\n", true)
            },
            // {
            //     "separator-newline", (ref string text, int startIndex, out bool newlined) => {
            //         newlined = true;
                    
            //         string target = "";

            //         if (text[startIndex] == '\n')
            //         {
                        
            //             return "\n";
            //         }

            //         while (startIndex < text.Length && text[startIndex] != '\n')
            //         {
            //             if (!separator.ContainsKey(text[startIndex]))
            //             {
            //                 // if anything but space and tab is found
            //                 return null;
            //             }

            //             target += text[startIndex];
            //             startIndex += 1;
            //         }

                    
            //         return target;
            //     }
            // },
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

                    // text lines can only start on the beginning of the program, or after a new line
                    if (startIndex != 0)
                    {
                        if (text[startIndex - 1] != '\n')
                        {
                            return null;
                        }
                    }

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
                "id", (ref string text, int startIndex, out bool newlined) => {
                    newlined = false;
                    string target = "";

                    while (startIndex < text.Length && !whitespace.ContainsKey(text[startIndex]) && !(text[startIndex] == '/' && text[startIndex + 1] == '/'))
                    {
                        target += text[startIndex];
                        startIndex += 1;
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

                    
                    return target;
                }
            },
            
            
            
        };

        static TokenValidator CreateMatchExact(string toMatch, bool containsNewline = false)
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
    } 
}