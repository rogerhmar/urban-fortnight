  
      // Install the .NET library via NuGet: dotnet add package Azure.AI.OpenAI --prerelease

      using System.Diagnostics;
      using Azure;  
      using Azure.AI.OpenAI;
      using OpenAI.Chat;
        
      using static System.Environment;
      string GetCurrentFolder()
      {
          var stackTrace = new StackTrace(true);
          var frame = stackTrace.GetFrame(0); // 0 is the current method
          return  Path.Combine(frame!.GetFileName()!, "..");
      }
      void printIn(string text)
      {
          var before = Console.ForegroundColor;
          Console.ForegroundColor = ConsoleColor.Red;
          Console.WriteLine(text);
          Console.ForegroundColor = before;
      }

      async Task RunAsync()
      {
          // Retrieve the OpenAI endpoint from environment variables
          var endpoint = GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ??
                         "https://faggruppeaihub9416157283.openai.azure.com/";

          if (string.IsNullOrEmpty(endpoint))
          {
              Console.WriteLine("Please set the AZURE_OPENAI_ENDPOINT environment variable.");
              return;
          }

          var key = GetEnvironmentVariable("AZURE_OPENAI_KEY");
          if (string.IsNullOrEmpty(key))
          {
              Console.WriteLine("Please set the AZURE_OPENAI_KEY environment variable.");
              return;
          }

          AzureKeyCredential credential = new AzureKeyCredential(key);

          // Initialize the AzureOpenAIClient
          AzureOpenAIClient azureClient = new(new Uri(endpoint), credential);

          // Initialize the ChatClient with the specified deployment name
          ChatClient chatClient = azureClient.GetChatClient("gpt-4o-mini-roger");

          var files = Directory.EnumerateFiles(GetCurrentFolder()).Where(p => p.EndsWith(".md")).ToList();
          files.Sort( (a, b) => string.Compare(a, b, StringComparison.Ordinal));
          var rand = new Random();
          var num = rand.Next(0, files.Count);
          Console.WriteLine(num + " " + files[num]);
          string t1 = File.ReadAllText(files[num]);

          // Create chat completion options

          var options = new ChatCompletionOptions
          {
              Temperature = (float)0.7,
              MaxOutputTokenCount = 800,

              TopP = (float)0.95,
              FrequencyPenalty = (float)0,
              PresencePenalty = (float)0
          };

          Console.WriteLine($"Can you guess who I am? ({num})");
          while (true)
          {
              var input = Console.ReadLine();
              var messages = new List<ChatMessage>
              {
                  new SystemChatMessage(t1),
                  new SystemChatMessage(input),
              };
              try
              {
                  // Create the chat completion request
                  ChatCompletion completion = await chatClient.CompleteChatAsync(messages, options);

                  // Print the response
                  //  Console.WriteLine(JsonSerializer.Serialize(completion, new JsonSerializerOptions() { WriteIndented = true }));
                  printIn(completion != null
                      ? string.Join("\n", completion.Content.Select(s => s.Text))
                      : "No response received.");
              }
              catch (Exception ex)
              {
                  Console.WriteLine($"An error occurred: {ex.Message}");
              }
          }

      }

      await RunAsync();