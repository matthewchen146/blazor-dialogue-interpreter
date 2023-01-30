using System;
using System.Collections;
using System.Collections.Generic;

#nullable enable
public class DialogueInterpreterOld
{
    public class DialogueCommand 
    {
        public List<string> args = new();
        public string token = "";
        public int index = 0;
        public Conversation conversation;
        public int rawLineIndex = 0;
        public DialogueCommand(int _index, Conversation _conversation, string _token, IEnumerable<string> _args)
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
        public List<DialogueCommand> commands = new();
        public Conversation(int _index)
        {
            index = _index;
        }
    }
    public class DialogueData
    {
        public List<DialogueCommand> commands = new();
        public DialogueInterpreterOld dialogueInterpreter;
        public Dictionary<string, Conversation> conversations = new();
        public Conversation? conversation;
        public List<int> rawLineIndices;
        public DialogueData(DialogueInterpreterOld _dialogueInterpreter, List<int> _rawLineIndices)
        {
            dialogueInterpreter = _dialogueInterpreter;
            rawLineIndices = _rawLineIndices;
        }
    }
    delegate int CommandPreprocessor(DialogueData dialogueData, DialogueCommand command, out string error);
    delegate int CommandValidator(DialogueData dialogueData, DialogueCommand command, out string error);

    delegate int CommandResolver(DialogueData dialogueData, DialogueCommand command, out string error);

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

        public int Preprocess(DialogueData dialogueData, DialogueCommand command, out string error)
        {
            error = "";
            return commandPreprocessor != null ? commandPreprocessor.Invoke(dialogueData, command, out error) : 1;
        }

        public int Validate(DialogueData dialogueData, DialogueCommand command, out string error)
        {
            error = "";
            if (command.args.Count < minimumArgCount)
            {
                error = $"{minimumArgCount} arguments are required";
                return 0;
            }
            return commandValidator != null ? commandValidator.Invoke(dialogueData, command, out error) : 1;
        }

        public int Resolve(DialogueData dialogueData, DialogueCommand command, out string error)
        {   
            error = "";
            return commandResolver != null ? commandResolver.Invoke(dialogueData, command, out error) : 1;
        }
    }

    private static readonly Dictionary<string, CommandContainer> possibleCommands = new() {
        {"conversation", new CommandContainer(
            (DialogueData dialogueData, DialogueCommand command, out string error) => {
                error = "";
                if (command.args.Count == 0)
                {
                    error = $"Conversation must have a name argument. Example: @conversation Greeting";
                    return 0;
                }
                string conversationName = command.args[0];
                if (dialogueData.conversations.ContainsKey(conversationName))
                {
                    error = $"Conversation \"{conversationName}\" was already defined on line {dialogueData.rawLineIndices[dialogueData.conversations[conversationName].index] + 1}";
                    return 0;
                }
                Conversation conversation = new(command.index);
                dialogueData.conversations.Add(conversationName, conversation);
                dialogueData.conversation = conversation;
                return 1;
            },
            null,
            null,
            1
        )},
        {"label", new CommandContainer(
            (DialogueData dialogueData, DialogueCommand command, out string error) => {
                error = "";
                if (command.args.Count == 0)
                {
                    error = $"Label requires a name";
                    return 0;
                }

                string labelName = command.args[0];
                if (command.conversation.labels.ContainsKey(labelName))
                {
                    error = $"Label \"{labelName}\" was already defined at {dialogueData.rawLineIndices[command.conversation.labels[labelName]] + 1} (no duplicate labels allowed)";
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
            (DialogueData dialogueData, DialogueCommand command, out string error) => {
                error = "";
                if (command.args.Count == 0)
                {
                    error = $"@enter requires atleast 1 name to enter";
                    return 0;
                }
                Dictionary<string, int> characters = new();
                foreach (string name in command.args)
                {
                    if (characters.ContainsKey(name))
                    {
                        error = $"@enter can't enter the same name multiple times";
                        return 0;
                    }
                    characters.Add(name, 1);
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
            (DialogueData dialogueData, DialogueCommand command, out string error) => {
                error = "";
                if (command.args.Count == 0)
                {
                    error = $"@speak requires atleast 1 name to speak";
                    return 0;
                }
                foreach (string name in command.args)
                {
                    if (!command.conversation.characters.ContainsKey(name))
                    {
                        error = $"Character \"{name}\" at @speak was never entered or is entered or defined. Use @enter [name]";
                        return 0;
                    }
                }
                return 1;
            },
            null, 
            (DialogueData dialogueData, DialogueCommand command, out string error) => {
                error = "";
                dialogueData.dialogueInterpreter.events.Trigger("speakerChanged", command.args[0]);
                return 1;
            },
            1
        )},
        {"text", new CommandContainer(
            null,
            null,
            (DialogueData dialogueData, DialogueCommand command, out string error) => {
                error = "";
                dialogueData.dialogueInterpreter.events.Trigger("textChanged", command.args[0]);
                dialogueData.dialogueInterpreter.nextReady = false;
                return 1;
            },
            1
        )},
        {"option", new CommandContainer(
            null,
            (DialogueData dialogueData, DialogueCommand command, out string error) => {
                error = "";
                string labelName = command.args[0];
                if (labelName != "_" && !command.conversation.labels.ContainsKey(labelName))
                {
                    error = $"Option label \"{labelName}\" does not exist in this conversation";
                    return 0;
                }
                return 1;
            }, 
            (DialogueData dialogueData, DialogueCommand command, out string error) => {
                error = "";
                if (command.args.Count > 1)
                {
                    List<string> words = new();
                    for (int i = 1; i < command.args.Count; i++)
                    {
                        words.Add(command.args[i]);
                    }
                    string text = string.Join(" ", words);
                    string labelName = command.args[0];
                    int index = dialogueData.dialogueInterpreter.currentOptions.Count;
                    dialogueData.dialogueInterpreter.currentOptions.Add(labelName);
                    dialogueData.dialogueInterpreter.events.Trigger("optionAdded", index, text, labelName);
                }
                if (command.index < dialogueData.commands.Count - 1)
                {
                    DialogueCommand nextCommand = dialogueData.commands[command.index + 1];
                    if (nextCommand.token != "option")
                    {
                        dialogueData.dialogueInterpreter.nextReady = false;
                    }
                }
                return 1;
            },
            1
        )},
        {"jump", new CommandContainer(
            null,
            (DialogueData dialogueData, DialogueCommand command, out string error) => {
                error = "";
                string labelName = command.args[0];
                if (!command.conversation.labels.ContainsKey(labelName))
                {
                    error = $"Jump label \"{labelName}\" does not exist in this conversation";
                    return 0;
                }
                return 1;
            },
            (DialogueData dialogueData, DialogueCommand command, out string error) => {
                error = "";
                string labelName = command.args[0];
                int labelIndex = command.conversation.labels[labelName];
                dialogueData.dialogueInterpreter.currentIndex = labelIndex;
                return 1;
            },
            1
        )},
        {"event", new CommandContainer(
            null,
            null,
            (DialogueData dialogueData, DialogueCommand command, out string error) => {
                error = "";
                string eventName = command.args[0];
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
                DialogueCommand command = currentDialogueData.commands[tempIndex];

                if (command.token != "option")
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

    private string HasValidToken(string line, out string error)
    {
        string token = "";
        if (line[0] == '@')
        {
            string[] words = line.Split(" ", System.StringSplitOptions.RemoveEmptyEntries);

            token = words[0].Substring(1);

            if (!possibleCommands.ContainsKey(token))
            {
                error = $"@\"{token}\" does not exist";
                return "";
            }
        }
        error = "";
        return token;
    }

    private List<string> FindArgs(string line)
    {
        List<string> args = new();
        string[] words = line.Split(" ", System.StringSplitOptions.RemoveEmptyEntries);
        // check for arguments next to the token
        if (words.Length > 1)
        {
            for (int argIndex = 1; argIndex < words.Length; argIndex++)
            {
                string arg = words[argIndex];
                args.Add(arg);
            }
        }
        return args;
    }

    public int Load(string rawText, out string error, out int errorLine, out DialogueData outDialogueData)
    {
        error = "";
        errorLine = 0;

        // extract relevant lines (no empty lines or comments)

        string[] newLineSeparators = {"\r\n", "\r", "\n"};
        string[] rawLines = rawText.Split(newLineSeparators, System.StringSplitOptions.None);
        List<string> lines = new();
        List<int> rawLineIndices = new();
        for (int i = 0; i < rawLines.Length; i++)
        {
            string line = rawLines[i];
            line = line.Trim();
            // remove comments from each line
            if (line.IndexOf("//") == 0)
            {
                continue;
            }
            if (line.Length == 0)
            {
                continue;
            }
            string[] commentSplit = line.Split("//", System.StringSplitOptions.RemoveEmptyEntries);
            line = commentSplit[0];
            // remove empty spaces
            string[] words = line.Split(" ", System.StringSplitOptions.RemoveEmptyEntries);
            line = string.Join(' ', words);

            lines.Add(line);
            rawLineIndices.Add(i);
        }

        DialogueData preprocessData = new(this, rawLineIndices);
        outDialogueData = preprocessData;

        // preprocess the commands to find all conversations and labels and characters in each conversation
        for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
        {
            errorLine = rawLineIndices[lineIndex] + 1;
            string line = lines[lineIndex];
            string token = HasValidToken(line, out string tokenError);

            // check if valid token
            if (tokenError.Length > 0)
            {
                error = $"Token Error (Line {errorLine}) - {tokenError}";
                return 0;
            }

            // check for the first conversation command
            if (preprocessData.conversation == null)
            {
                if (token != "conversation")
                {
                    error = $"Preprocess Error (Line {errorLine}) - All dialogue must be contained within a @conversation command (the first command should be @conversation)";
                    return 0;
                }

                preprocessData.conversation = new Conversation(lineIndex);
            }

            List<string>? args;

            // check if the line is a text line
            if (token.Length == 0)
            {
                token = "text";
                args = new()
                {
                    line
                };
            }
            else
            {
                args = FindArgs(line);
            }

            DialogueCommand command = new(lineIndex, preprocessData.conversation, token, args);

            if (possibleCommands[token].Preprocess(preprocessData, command, out string preprocessError) == 0)
            {
                error = $"Preprocess Error (Line {errorLine}) - {preprocessError}";
                return 0;
            }

            preprocessData.commands.Add(command);
        }


        // validate all commands
        foreach (DialogueCommand command in preprocessData.commands)
        {
            if (possibleCommands[command.token].Validate(preprocessData, command, out string validateError) == 0)
            {
                errorLine = rawLineIndices[command.index] + 1;
                error = $"Validate Error (Line {errorLine}) - {validateError}";
                return 0;
            }
        }

        currentDialogueData = preprocessData;
        return 1;

    }

    public int Load(string rawText, out string error, out int errorLine)
    {
        return Load(rawText, out error, out errorLine, out _);
    }

    public int Load(string rawText, out string error)
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
            DialogueCommand command = dialogueData.commands[currentIndex];
            int result = possibleCommands[command.token].Resolve(dialogueData, command, out string error);
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
