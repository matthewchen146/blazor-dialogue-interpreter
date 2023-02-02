using System;
using System.Collections;
using System.Collections.Generic;
using Parser;

#nullable enable
public class DialogueInterpreter
{

    public class Command 
    {
        public List<Token> args = new();
        public Token token;
        public Conversation? conversation;
        public int index = 0;
        // public Conversation conversation;
        public Command(int _index, Token _token, Conversation? _conversation, IEnumerable<Token> _args)
        {
            index = _index;
            token = _token;
            conversation = _conversation;
            args.AddRange(_args);
        }

    }

    public class Character 
    {
        public string name = "";
        public Character(string _name)
        {
            name = _name;
        }
    }

    public class Conversation 
    {
        public Dictionary<string, Character> characters = new();
        public Dictionary<string, int> labels = new();
        public int index = 0;
        // public List<Command> commands = new();
        public Conversation(int _index)
        {
            index = _index;
        }
        public Conversation()
        {

        }
    }
    public class DialogueData
    {
        public List<Command> commands = new();
        public DialogueInterpreter dialogueInterpreter;
        public Dictionary<string, Conversation> conversations = new();
        public Conversation? conversation = null;
        public List<Token> tokens = new();

        public DialogueData(DialogueInterpreter _dialogueInterpreter)
        {
            dialogueInterpreter = _dialogueInterpreter;
        }
    }
    delegate int CommandPreprocessor(DialogueData dialogueData, Command command, out string error);
    delegate int CommandValidator(DialogueData dialogueData, Command command, out string error);

    delegate int CommandResolver(DialogueData dialogueData, Command command, out string error);

    class CommandContainer {
        public CommandPreprocessor? commandPreprocessor;
        public CommandValidator? commandValidator;
        public CommandResolver? commandResolver;
        public int minimumArgCount = 0;
        public CommandContainer(CommandPreprocessor? _commandPreprocessor, CommandValidator? _commandValidator, CommandResolver? _commandResolver, int _minimumArgCount = 0)
        {
            commandPreprocessor = _commandPreprocessor;
            commandValidator = _commandValidator;
            commandResolver = _commandResolver;
            minimumArgCount = _minimumArgCount;
        }
        public CommandContainer()
        {
            
        }

        public int Preprocess(DialogueData dialogueData, Command command, out string error)
        {
            error = "";
            // if (command.args.Count < minimumArgCount)
            // {
            //     error = $"{minimumArgCount} arguments are required";
            //     return 0;
            // }
            return commandPreprocessor != null ? commandPreprocessor.Invoke(dialogueData, command, out error) : 1;
        }

        public int Validate(DialogueData dialogueData, Command command, out string error)
        {
            error = "";
            if (command.args.Count < minimumArgCount)
            {
                error = $"{minimumArgCount} arguments are required";
                return 0;
            }
            return commandValidator != null ? commandValidator.Invoke(dialogueData, command, out error) : 1;
        }

        public int Resolve(DialogueData dialogueData, Command command, out string error)
        {   
            error = "";
            return commandResolver != null ? commandResolver.Invoke(dialogueData, command, out error) : 1;
        }
    }

    private static readonly Dictionary<string, CommandContainer> possibleCommands = new() {
        {"conversation", new CommandContainer(
            (DialogueData dialogueData, Command command, out string error) => {
                error = "";
                if (command.args.Count == 0)
                {
                    error = $"Conversation must have a name argument. Example: @conversation Greeting";
                    return 0;
                }
                string conversationName = command.args[0].value;
                if (dialogueData.conversations.ContainsKey(conversationName))
                {
                    error = $"Conversation [{conversationName}] was already defined";
                    return 0;
                }
                Conversation conversation = new(command.index);
                dialogueData.conversations.Add(conversationName, conversation);
                dialogueData.conversation = conversation;
                command.conversation = conversation;
                return 1;
            },
            null,
            null,
            1
        )},
        {"label", new CommandContainer(
            (DialogueData dialogueData, Command command, out string error) => {
                error = "";
                if (command.args.Count == 0)
                {
                    error = $"Label requires a name";
                    return 0;
                }

                if (command.conversation == null)
                {
                    error = "No conversation attached to this command";
                    return 0;
                }

                string labelName = command.args[0].value;
                if (command.conversation.labels.ContainsKey(labelName))
                {
                    // error = $"Label \"{labelName}\" was already defined at {dialogueData.rawLineIndices[command.conversation.labels[labelName]] + 1} (no duplicate labels allowed)";
                    return 0;
                }
                command.conversation.labels.Add(labelName, command.index);
                return 1;
            },
            null,
            null,
            1
        )},
        {"enter", new CommandContainer(
            (DialogueData dialogueData, Command command, out string error) => {
                error = "";
                if (command.args.Count == 0)
                {
                    error = $"@enter requires atleast 1 name to enter";
                    return 0;
                }
                Dictionary<string, int> characters = new();
                foreach (Token token in command.args)
                {
                    string name = token.value;
                    if (characters.ContainsKey(name))
                    {
                        error = $"Can't enter the same ID multiple times";
                        return 0;
                    }
                    characters.Add(name, 1);
                }

                if (command.conversation == null)
                {
                    error = "No conversation attached to this command";
                    return 0;
                }
                // add characters to the conversation
                foreach (string name in characters.Keys)
                {
                    if (!command.conversation.characters.ContainsKey(name))
                    {
                        command.conversation.characters.Add(name, new(name));
                    }
                }
                return 1;
            },
            null,
            null,
            1
        )},
        {"speak", new CommandContainer(
            (DialogueData dialogueData, Command command, out string error) => {
                error = "";
                // if (command.args.Count == 0)
                // {
                //     error = $"@speak requires atleast 1 name to speak";
                //     return 0;
                // }
                // foreach (string name in command.args)
                // {
                //     if (!command.conversation.characters.ContainsKey(name))
                //     {
                //         error = $"Character \"{name}\" at @speak was never entered or is entered or defined. Use @enter [name]";
                //         return 0;
                //     }
                // }
                return 1;
            },
            null, 
            (DialogueData dialogueData, Command command, out string error) => {
                error = "";
                dialogueData.dialogueInterpreter.events.Trigger("speakerChanged", command.args[0].value);
                return 1;
            },
            1
        )},
        {"text", new CommandContainer(
            null,
            null,
            (DialogueData dialogueData, Command command, out string error) => {
                error = "";
                dialogueData.dialogueInterpreter.events.Trigger("textChanged", command.args[0].value);
                dialogueData.dialogueInterpreter.nextReady = false;
                return 1;
            },
            1
        )},
        {"option", new CommandContainer(
            null,
            (DialogueData dialogueData, Command command, out string error) => {
                error = "";
                string labelName = command.args[0].value;

                if (command.conversation == null)
                {
                    error = "No conversation attached to this command";
                    return 0;
                }

                if (labelName != "_" && !command.conversation.labels.ContainsKey(labelName))
                {
                    error = $"Option label \"{labelName}\" does not exist in this conversation";
                    return 0;
                }
                return 1;
            }, 
            (DialogueData dialogueData, Command command, out string error) => {
                error = "";

                int index = dialogueData.dialogueInterpreter.currentOptions.Count;
                string text = command.args[1].value;
                string labelName = command.args[0].value;

                dialogueData.dialogueInterpreter.currentOptions.Add(labelName);

                dialogueData.dialogueInterpreter.events.Trigger("optionAdded", index, text, labelName);
                
                // Console.WriteLine("checking for last option");
                if (command.index < dialogueData.commands.Count - 1)
                {
                    // Console.WriteLine("option is <= the second to last command");
                    Command nextCommand = dialogueData.commands[command.index + 1];
                    if (nextCommand.token.type != "option")
                    {
                        // Console.WriteLine("next ready setting to false");
                        dialogueData.dialogueInterpreter.nextReady = false;
                    }
                }
                return 1;
            },
            2
        )},
        {"jump", new CommandContainer(
            null,
            (DialogueData dialogueData, Command command, out string error) => {
                error = "";
                if (command.conversation == null)
                {
                    error = "No conversation attached to this command";
                    return 0;
                }

                string labelName = command.args[0].value;
                if (!command.conversation.labels.ContainsKey(labelName))
                {
                    error = $"Jump label \"{labelName}\" does not exist in this conversation";
                    return 0;
                }
                return 1;
            },
            (DialogueData dialogueData, Command command, out string error) => {
                error = "";
                if (command.conversation == null)
                {
                    error = "No conversation attached to this command";
                    return 0;
                }

                string labelName = command.args[0].value;
                int labelIndex = command.conversation.labels[labelName];
                dialogueData.dialogueInterpreter.currentIndex = labelIndex;
                return 1;
            },
            1
        )},
        {"event", new CommandContainer(
            null,
            null,
            (DialogueData dialogueData, Command command, out string error) => {
                error = "";
                Token eventName = command.args[0];
                dialogueData.dialogueInterpreter.events.Trigger("dialogueEvent", eventName);
                return 1;
            },
            1
        )}
    };

    
    private IEnumerator<int>? currentIterator;
    public string currentIteratorError = "";
    private int currentIndex = 0;
    List<string> currentOptions = new();
    public DialogueData? currentDialogueData;
    public EventEmitter events {get; private set;} = new();

    private bool nextReady = false;

    public int Next()
    {
        if (currentIterator == null)
        {
            return 0;
        }
        nextReady = true;
        currentIterator.MoveNext();
        return 1;
    }

    public int ChooseOption(int index, out string error)
    {
        error = "";
        if (currentDialogueData == null)
        {
            error = $"ChooseOption Dialogue Interpreter has no current dialogue";
            return 0;
        }

        if (currentDialogueData.conversation == null)
        {
            error = $"ChooseOption There is no current conversation";
            return 0;
        }

        if (index < 0 || index >= currentOptions.Count)
        {
            error = $"ChooseOption {index} index out of range (should be within 0 to {currentOptions.Count - 1})";
            return 0;
        }

        string labelName = currentOptions[index];
        if (labelName != "_")
        {
            int labelIndex = currentDialogueData.conversation.labels[labelName];
            currentIndex = labelIndex;
        }
        else
        {
            // go past last option
            int tempIndex = currentIndex + 1;
            while (tempIndex < currentDialogueData.commands.Count)
            {
                Command command = currentDialogueData.commands[tempIndex];

                if (command.token.type != "option")
                {
                    currentIndex = tempIndex - 1;
                    break;
                }

                tempIndex += 1;
            }
        }

        currentOptions.Clear();
        events.Trigger("optionsCleared");
        
        return Next();
    }

    public int Load(string rawText, out ErrorInfo? error, out DialogueData outDialogueData)
    {
        int errorCode = 1;
        error = null;
        DialogueData preprocessData = new(this);

        outDialogueData = preprocessData;


        Parser.Parser parser = new()
        {
            debug = false
        };
        int parseResult = parser.Parse(ref rawText, out List<Token> tokens, out List<ErrorInfo> parsedErrors, "program");
        Console.WriteLine(parseResult == 0 ? "PARSE SUCCESS" : "PARSE FAIL");

        outDialogueData.tokens = tokens;

        // if (parsedErrors.Count > 0)
        // {
        //     error = new("Parse", tokens[tokens.Count - 1].position.lineIndex, parsedErrors[parsedErrors.Count - 1].message);
        //     return 0;
        // }
        

        Console.WriteLine("TOKENS FOUND:");

        foreach (Token token in tokens)
        {
            Console.WriteLine($"(Ln {token.LineNumber}, Col {token.ColumnNumber}) TYPE: [{token.type}] VALUE: [{token.value}]");
        }

        Console.WriteLine("ERRORS FOUND:");

        foreach (ErrorInfo errorInfo in parsedErrors)
        {
            Console.WriteLine($"ERROR: {errorInfo}");
        }

        // get/create all commands
        for (int index = 0; index < tokens.Count; index++)
        {
            Token token = tokens[index];

            if (token.type == "newline" || token.type == "separator")
            {
                continue;
            }

            // Console.WriteLine($"checking for command, text: [{token.type}] [{token.value}]");

            // check if command
            if (token.type == "command-prefix")
            {
             
                // Console.WriteLine("found command prefix, looking for command name");

                index += 1;

                if (index == tokens.Count)
                {
                    error = new("Parse", token, $"Unexpected end of script");
                    return 0;
                }

                Token commandToken = tokens[index];

                // Console.WriteLine($"found command name {commandToken.type}");

                // get args
                List<Token> args = new();
                for (index += 1; 
                    index < tokens.Count 
                    && tokens[index].type != "comment" 
                    && tokens[index].type != "command-prefix" 
                    && tokens[index].type != "text"
                    && tokens[index].type != "newline"; 
                    index++
                )
                {
                    if (tokens[index].type == "separator")
                    {
                        continue;
                    }
                    args.Add(tokens[index]);
                }

                index -= 1;

                int commandIndex = preprocessData.commands.Count; // refers to the command index in the command list, not token list
                Command command = new(commandIndex, commandToken, preprocessData.conversation, args);


                // add to preprocess data
                preprocessData.commands.Add(command);

            }
            else if (token.type == "text")
            {
                Command command = new(index, token, preprocessData.conversation, new Token[] {token});

                // add to preprocess data
                preprocessData.commands.Add(command);
            }
            else if (token.type != "comment")
            {
                error = new("Parse", token, $"Unexpected token type [{token.type}] ({token.value})");
                errorCode = 0;
                // return 0;
            }
        }

        foreach (Command command in preprocessData.commands)
        {
            Console.WriteLine("COMMAND: " + command.token.type);
            foreach (Token arg in command.args)
            {
                Console.WriteLine($"ARG: [{arg.type}] ({arg.value})");
            }
        }

        // // preprocess commands
        for (int index = 0; index < preprocessData.commands.Count; index++)
        {
            Command command = preprocessData.commands[index];
            Token token = command.token;

            // Console.WriteLine($"preprocess --- lookg at command {token.type}");

            if (!possibleCommands.ContainsKey(token.type))
            {
                error = new("Preprocess", token, $"Command [{token.type}] does not exist");
                return 0;
            }


            command.conversation = preprocessData.conversation;

            int result = possibleCommands[token.type].Preprocess(preprocessData, command, out string errorMessage);
            if (result == 0)
            {
                error = new("Preprocess", token, $"@{token.type} Error - {errorMessage}");
                return 0;
            }
        }

        // validate commands
        for (int index = 0; index < preprocessData.commands.Count; index++)
        {
            Command command = preprocessData.commands[index];
            Token token = command.token;

            int result = possibleCommands[token.type].Validate(preprocessData, command, out string errorMessage);
            if (result == 0)
            {
                error = new("Validate", token, $"@{token.type} Error - {errorMessage}");
                return 0;
            }

        }

        if (errorCode == 1)
        {
            currentDialogueData = preprocessData;
        }

        return errorCode;
    }

    public int Load(string rawText, out ErrorInfo? error)
    {
        return Load(rawText, out error, out _);
    }
    public int Load(string rawText)
    {
        return Load(rawText, out _);
    }

    private IEnumerator<int> StartDialogueEnumerator()
    {
        if (currentDialogueData == null)
        {
            yield break;
        }
        DialogueData dialogueData = currentDialogueData;
        nextReady = true;
        while (currentIndex < dialogueData.commands.Count)
        {
            Command command = dialogueData.commands[currentIndex];
            int result = possibleCommands[command.token.type].Resolve(dialogueData, command, out string error);
            // Console.WriteLine($"command: {command.token}, index: {currentIndex}, result: {result}");
            if (result == 0)
            {
                Console.WriteLine(error);
                currentIteratorError = error;
                Console.WriteLine($"breaking with error: {error}");
                yield break;
            }

            if (!nextReady) 
            {
                yield return result;
            }

            currentIndex += 1;
        }

    }

    public int StartConversation(string conversationName, out string error)
    {
        error = "";
        if (currentDialogueData == null)
        {
            error = $"StartConversation - Dialogue Interpreter has no current dialogue";
            return 0;
        }

        if (!currentDialogueData.conversations.ContainsKey(conversationName))
        {
            error = $"StartConversation - No conversation found by the name {conversationName}";
            return 0;
        }

        // clear current options
        currentOptions.Clear();
        events.Trigger("optionsCleared");

        // reset command index
        currentIndex = 0;

        // create enumerable
        currentIterator = StartDialogueEnumerator();   

        return Next();
    }

    public int StartConversation(string conversationName)
    {
        return StartConversation(conversationName, out string _);
    }

}
