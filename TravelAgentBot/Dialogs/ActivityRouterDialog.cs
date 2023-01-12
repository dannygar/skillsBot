// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Schema;
using TravelAgentBot.CognitiveModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TravelAgentBot.Dialogs
{
    /// <summary>
    /// A root dialog that can route activities sent to the skill to different sub-dialogs.
    /// </summary>
    public class ActivityRouterDialog : ComponentDialog
    {
        private readonly CLURecognizer _cluRecognizer;

        public ActivityRouterDialog(CLURecognizer cluRecognizer)
            : base(nameof(ActivityRouterDialog))
        {
            _cluRecognizer = cluRecognizer;

            AddDialog(new BookingDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[] { ProcessActivityAsync }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> ProcessActivityAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // A skill can send trace activities, if needed.
            await stepContext.Context.TraceActivityAsync($"{GetType().Name}.ProcessActivityAsync()", label: $"Got ActivityType: {stepContext.Context.Activity.Type}", cancellationToken: cancellationToken);

            switch (stepContext.Context.Activity.Type)
            {
                case ActivityTypes.Event:
                    return await OnEventActivityAsync(stepContext, cancellationToken);

                case ActivityTypes.Message:
                    return await OnMessageActivityAsync(stepContext, cancellationToken);

                default:
                    // We didn't get an activity type we can handle.
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Unrecognized ActivityType: \"{stepContext.Context.Activity.Type}\".", inputHint: InputHints.IgnoringInput), cancellationToken);
                    return new DialogTurnResult(DialogTurnStatus.Complete);
            }
        }

        // This method performs different tasks based on the event name.
        private async Task<DialogTurnResult> OnEventActivityAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var activity = stepContext.Context.Activity;
            await stepContext.Context.TraceActivityAsync($"{GetType().Name}.OnEventActivityAsync()", label: $"Name: {activity.Name}. Value: {GetObjectAsJsonString(activity.Value)}", cancellationToken: cancellationToken);

            // Resolve what to execute based on the event name.
            switch (activity.Name)
            {
                case "BookFlight":
                    return await BeginBookFlight(stepContext, cancellationToken);

                default:
                    // We didn't get an event name we can handle.
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Unrecognized EventName: \"{activity.Name}\".", inputHint: InputHints.IgnoringInput), cancellationToken);
                    return new DialogTurnResult(DialogTurnStatus.Complete);
            }
        }

        // This method just gets a message activity and runs it through CLU. 
        private async Task<DialogTurnResult> OnMessageActivityAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var activity = stepContext.Context.Activity;
            await stepContext.Context.TraceActivityAsync($"{GetType().Name}.OnMessageActivityAsync()", label: $"Text: \"{activity.Text}\". Value: {GetObjectAsJsonString(activity.Value)}", cancellationToken: cancellationToken);

            if (!_cluRecognizer.IsConfigured)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("NOTE: CLU is not configured. To enable all capabilities, add 'CLUEndpoint' and 'CLUAPIKey' to the appsettings.json file.", inputHint: InputHints.IgnoringInput), cancellationToken);
            }
            else
            {
                // Call CLU with the utterance.
                var cluResult = await _cluRecognizer.RecognizeAsync<FlightBooking>(stepContext.Context, cancellationToken);

                // Create a message showing the CLU results.
                var sb = new StringBuilder();
                sb.AppendLine($"CLU results for \"{activity.Text}\":");
                var intent = cluResult.Intents.FirstOrDefault(x => x.Value.Equals(CLURecognizer.GetMaxScore(cluResult.Intents.Values.ToArray())));
                sb.AppendLine($"Intent: \"{intent.Key}\" Score: {intent.Value.Score}");

                await stepContext.Context.SendActivityAsync(MessageFactory.Text(sb.ToString(), inputHint: InputHints.IgnoringInput), cancellationToken);

                // Start a dialog if we recognize the intent.
                switch (cluResult.TopIntent().intent)
                {
                    case FlightBooking.Intent.BookFlight:
                        stepContext.Context.Activity.Value = ParseEntities(cluResult.Entities);
                        return await BeginBookFlight(stepContext, cancellationToken);

                    default:
                        // Catch all for unhandled intents.
                        var didntUnderstandMessageText = $"Sorry, I didn't get that. Please try asking in a different way (intent was {cluResult.TopIntent().intent})";
                        var didntUnderstandMessage = MessageFactory.Text(didntUnderstandMessageText, didntUnderstandMessageText, InputHints.IgnoringInput);
                        await stepContext.Context.SendActivityAsync(didntUnderstandMessage, cancellationToken);
                        break;
                }
            }

            return new DialogTurnResult(DialogTurnStatus.Complete);
        }

        private async Task<DialogTurnResult> BeginBookFlight(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var activity = stepContext.Context.Activity;
            var bookingDetails = new BookingDetails();
            if (activity.Value != null)
            {
                bookingDetails = JsonConvert.DeserializeObject<BookingDetails>(JsonConvert.SerializeObject(activity.Value));
            }

            // Start the booking dialog.
            var bookingDialog = FindDialog(nameof(BookingDialog));
            return await stepContext.BeginDialogAsync(bookingDialog.Id, bookingDetails, cancellationToken);
        }

        private string GetObjectAsJsonString(object value) => value == null ? string.Empty : JsonConvert.SerializeObject(value);


        private JObject ParseEntities(List<FlightBooking.Entity> entities)
        {
            var categories = new Dictionary<string, FlightBooking.Entity>();
            foreach (var entity in entities)
            {
                if (!categories.ContainsKey(entity.Category))
                {
                    categories.Add(entity.Category, entity);
                } else
                {
                    var confidence = double.Parse(categories[entity.Category].Confidence);
                    if (confidence < double.Parse(entity.Confidence))
                    {
                        categories.Remove(entity.Category);
                        categories.Add(entity.Category, entity);
                    }
                }
            }

            var validDate = DateTime.TryParse(categories.First(x => x.Key == "Date").Value.Text, out DateTime travelDate);

            var activityValue = $"{{\"origin\": \"\", \"destination\": \"{categories.First(x => x.Key == "Destination").Value.Text}\", \"travelDate\": \"{categories.First(x => x.Key == "Date").Value.Text}\", \"multipleDates\": \"{!validDate}\"}}";
            return JObject.Parse(activityValue);

        }
    }
}
