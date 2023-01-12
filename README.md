# SkillDialog

Bot Framework v4 Skills with Dialogs sample.

This bot has been created using the [Bot Framework](https://dev.botframework.com); it shows how to use a skill dialog from a root bot.

## Prerequisites

- [.NET Framework SDK](https://dotnet.microsoft.com/download) version 6.0

  ```bash
  # determine dotnet version
  dotnet --version
  ```

## Key concepts in this sample

The solution uses dialogs, within both a parent bot (`PersonalAssistantBot`) and a skill bot (`TravelAgentBot`).
It demonstrates how to post activities from the parent bot to the skill bot and return the skill responses to the user.

- `PersonalAssistantBot`: this project shows how to consume a skill bot using a `SkillDialog`. It includes:
  - A [root dialog](PersonalAssistantBot/Dialogs/MainDialog.cs) that can call different actions on a skill using a `SkillDialog`:
    - To send events activities.
    - To send message activities.
    - To cancel a `SkillDialog` using `CancelAllDialogsAsync` that automatically sends an `EndOfConversation` activity to remotely let a skill know that it needs to end a conversation.
  - A sample [AdapterWithErrorHandler](PersonalAssistantBot/AdapterWithErrorHandler.cs) adapter that shows how to handle errors, terminate skills and send traces back to the emulator to help debugging the bot.
  - A sample [AllowedCallersHelper](PersonalAssistantBot/Authentication/AllowedCallersHelper.cs) class that shows how to validate that responses sent to the bot are coming from the configured skills.
  - A [Logger Middleware](PersonalAssistantBot/Middleware/LoggerMiddleware.cs) that shows how to handle and log activities coming from a skill.
  - A [SkillsConfiguration](PersonalAssistantBot/SkillsConfiguration.cs) class that can load skill definitions from the appsettings.json file.
  - A [startup](PersonalAssistantBot/Startup.cs) class that shows how to register the different root bot components for dependency injection.
  - A [BotController](PersonalAssistantBot/Controllers/BotController.cs) that handles skill responses.

- `TravelAgentBot`: this project shows a modified CoreBot that acts as a skill. It receives event and message activities from the parent bot and executes the requested tasks. This project includes:
  - An [ActivityRouterDialog](TravelAgentBot/Dialogs/ActivityRouterDialog.cs) that handles Event and Message activities coming from a parent and performs different tasks.
    - Event activities are routed to specific dialogs using the parameters provided in the `Values` property of the activity.
    - Message activities are sent to CLU if configured and trigger the desired tasks if the intent is recognized.
  - A sample [ActivityHandler](TravelAgentBot/Bots/TravelAgentBot.cs) that uses the `RunAsync` method on `ActivityRouterDialog`.
    
    Note: Starting in Bot Framework 4.8, the `RunAsync` method adds support to automatically send `EndOfConversation` with return values when the bot is running as a skill and the current dialog ends. It also handles reprompt messages to resume a skill where it left of.
  - A sample [AdapterWithErrorHandler](TravelAgentBot/AdapterWithErrorHandler.cs) adapter that shows how to handle errors, terminate the skills, send traces back to the emulator to help debugging the bot and send `EndOfConversation` messages to the parent bot with details of the error.
  - A [startup](TravelAgentBot/Startup.cs) class that shows how to register the different skill components for dependency injection.
  - A [sample skill manifest](TravelAgentBot/wwwroot/manifest/travelagentbot-manifest-1.0.json) that describes what the skill can do.

## To try this sample

- Clone the repository.

  ```bash
  git clone https://github.com/dannygar/personalAssistantBot.git
  ```

- Create a bot registration in the azure portal for the `TravelAgentBot` and update [TravelAgentBot/appsettings.json](TravelAgentBot/appsettings.json) with the AppId and password.
- Create a bot registration in the azure portal for the PersonalAssistantBot and update [PersonalAssistantBot/appsettings.json](PersonalAssistantBot/appsettings.json) with the AppId and password. 
- Update the BotFrameworkSkills section in [PersonalAssistantBot/appsettings.json](PersonalAssistantBot/appsettings.json) with the AppId for the skill you created in the previous step.
- (Optional) Configure the CLUEndpoint, CLUAPIKey and CLUProjectName section in the [TravelAgentBot/appsettings.json](TravelAgentBot/appsettings.json) if you want to run message activities through LUIS.
- Open the `PersonalAssistantBot.sln` solution and configure it to [start debugging with multiple processes](https://docs.microsoft.com/en-us/visualstudio/debugger/debug-multiple-processes?view=vs-2019#start-debugging-with-multiple-processes).

## Testing the bot using Bot Framework Emulator

[Bot Framework Emulator](https://github.com/microsoft/botframework-emulator) is a desktop application that allows bot developers to test and debug their bots on localhost or running remotely through a tunnel.

- Install the Bot Framework Emulator version 4.8.0 or greater from [here](https://github.com/Microsoft/BotFramework-Emulator/releases)

### Connect to the bot using Bot Framework Emulator

- Launch Bot Framework Emulator
- File -> Open Bot
- Enter a Bot URL of `http://localhost:3978/api/messages`, the `MicrosoftAppId` and `MicrosoftAppPassword` for the `PersonalAssistantBot`

## Deploy the bots to Azure

To learn more about deploying a bot to Azure, see [Deploy your bot to Azure](https://aka.ms/azuredeployment) for a complete list of deployment instructions.
