using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TomlChat;
internal sealed record Configuration(string ApiKeyFilePath, string Model, Uri Endpoint, double Temperature);
