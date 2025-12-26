using OpenAI.Chat;
using System.ClientModel;
using TomlChat;

var fileInfo = new FileInfo(args[0]);

if (!fileInfo.Exists)
{
    TomlChatFile.New().Save(fileInfo.FullName);
    Console.WriteLine($"The TomlChat file has been created: {fileInfo.FullName}");
    return;
}

Console.WriteLine($"Loading TomlChat File...");

var file = TomlChatFile.Load(fileInfo.FullName);
var configuration = file.GetConfiguration();

string apiKeyFilePath = configuration.ApiKeyFilePath;
if (fileInfo.DirectoryName is not null)
    apiKeyFilePath = Path.GetFullPath(configuration.ApiKeyFilePath, fileInfo.DirectoryName);
var apiKey = File.ReadAllText(apiKeyFilePath);


Console.WriteLine($"Generating Response...");

var client = new ChatClient(configuration.Model,
    new ApiKeyCredential(apiKey),
    new OpenAI.OpenAIClientOptions()
    {
        Endpoint = configuration.Endpoint
    });
var result = client.CompleteChat(file.GetMessages(), new ChatCompletionOptions()
{
    Temperature = (float)configuration.Temperature
});


file.AddMessage(result.Value);
file.Save(fileInfo.FullName);

Console.WriteLine($"The TomlChat file has been updated: {fileInfo.FullName}");