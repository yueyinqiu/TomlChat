using OpenAI.Chat;
using System;
using System.IO;
using System.Text;
using Tommy;

namespace TomlChat;

internal sealed class TomlChatFile
{
    internal readonly TomlTable toml;
    private TomlChatFile(TomlTable toml)
    {
        this.toml = toml;

        _ = this.GetConfiguration();
        _ = this.GetMessages().ToArray();
    }

    private static TomlArray GetArrayOrThrow(TomlNode node, string key)
    {
        if (!node.HasKey(key))
        {
            throw new FormatException(
                $"The given toml is not in TomlChat format: '{key}' does not exist.");
        }
        var result = node[key];
        if (!result.IsArray)
        {
            throw new FormatException(
                $"The given toml is not in TomlChat format: '{key}' is not a toml array.");
        }
        return result.AsArray;
    }

    private static TomlTable GetTableOrThrow(TomlNode node, string key)
    {
        if (!node.HasKey(key))
        {
            throw new FormatException(
                $"The given toml is not in TomlChat format: '{key}' does not exist.");
        }
        var result = node[key];
        if (!result.IsTable)
        {
            throw new FormatException(
                $"The given toml is not in TomlChat format: '{key}' is not a toml table.");
        }
        return result.AsTable;
    }

    private static TomlString GetStringOrThrow(TomlNode node, string key)
    {
        if (!node.HasKey(key))
        {
            throw new FormatException(
                $"The given toml is not in TomlChat format: '{key}' does not exist.");
        }
        var result = node[key];
        if (!result.IsString)
        {
            throw new FormatException(
                $"The given toml is not in TomlChat format: '{key}' is not a string.");
        }
        return result.AsString;
    }

    private static double GetFloatValueOrThrow(TomlNode node, string key)
    {
        if (!node.HasKey(key))
        {
            throw new FormatException(
                $"The given toml is not in TomlChat format: '{key}' does not exist.");
        }
        var result = node[key];

        if (result.IsFloat)
            return result.AsFloat.Value;

        if (result.IsInteger)
            return result.AsInteger.Value;
        
        throw new FormatException(
            $"The given toml is not in TomlChat format: '{key}' is not a toml float.");
    }


    public Configuration GetConfiguration()
    {
        var configuration = GetTableOrThrow(toml, "Configuration");
        var endpointString = GetStringOrThrow(configuration, "Endpoint");

        Uri uri;
        try
        {
            uri = new Uri(endpointString);
        }
        catch
        {
            throw new FormatException(
                "The given toml is not in TomlChat format: 'Endpoint' is not a correct uri.");
        }

        return new Configuration(
            GetStringOrThrow(configuration, "ApiKeyFilePath").Value,
            GetStringOrThrow(configuration, "Model").Value,
            uri,
            GetFloatValueOrThrow(configuration, "Temperature"));
    }

    public IEnumerable<ChatMessage> GetMessages()
    {
        var history = GetArrayOrThrow(toml, "History");
        foreach (var item in history.RawArray)
        {
            var content = GetStringOrThrow(item, "Content").Value;
            var roleString = GetStringOrThrow(item, "Role");

            yield return roleString.Value.ToLowerInvariant() switch
            {
                "system" => new SystemChatMessage(content),
                "user" => new UserChatMessage(content),
                "assistant" => new AssistantChatMessage(content),
                _ => throw new FormatException(
                    "The given toml is not in TomlChat format: 'Role' is a unsupported value.")
            };
        }
    }

    public void AddMessage(ChatCompletion message)
    {
        var history = GetArrayOrThrow(toml, "History");
        history.IsTableArray = true;
        history.Add(new TomlTable()
        {
            ["Role"] = "Assistant",
            ["Content"] = new TomlString()
            {
                IsMultiline = true,
                Value = message.Content.Single().Text
            }
        });
    }

    public void Save(TextWriter writer)
    {
        this.toml.WriteToWithoutOrderNormalizing(writer);
        writer.Flush();
    }

    public void Save(string path)
    {
        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        this.Save(writer);
    }

    public static TomlChatFile Load(TextReader reader)
    {
        var toml = TOML.Parse(reader);
        return new TomlChatFile(toml);
    }
    public static TomlChatFile Load(string path)
    {
        using var reader = new StreamReader(path, Encoding.UTF8);
        return Load(reader);
    }
    public static TomlChatFile New()
    {
        var tomlString =
            """"
            [Configuration]
            Model = "gpt-5"
            Endpoint = "https://api.openai.com/v1"

            # The file will be read for api key. Can be relative to your TomlChat file.
            ApiKeyFilePath = "my_secret_key.txt"
            
            Temperature = 0


            [[History]]
            Role = "System"
            Content = "Edit your prompts here. The role can be System, User, or Assistant."


            [[History]]
            Role = "User"
            Content = """Make full use of TOML!
            Multi-line strings are great for a long prompt."""
            """";
        using var reader = new StringReader(tomlString);
        return Load(reader);
    }
}