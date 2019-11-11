// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using VirtualAssistant.Responses.Escalate;
using VirtualAssistant.Services;
using VirtualAssistant.Responses.Cancel;
using VirtualAssistant.Responses.Main;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System.Collections.Generic;

namespace VirtualAssistant.Dialogs
{
    public class Search_by_Tutor : ComponentDialog
    {
        public Search_by_Tutor(BotServices botServices,
            UserState userState,
            IBotTelemetryClient telemetryClient) : base(nameof(Search_by_Tutor))
        {
            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                CheckSearchEntitiesAsync,
                ReturnResultsAsync,
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private static async Task<DialogTurnResult> CheckSearchEntitiesAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["tutor"] = stepContext.Options;
            string ss = (string)stepContext.Values["tutor"];
            if (stepContext.Values["tutor"] == null)
            {
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter the name of the tutor you would like to search for.") }, cancellationToken);
            }

            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Searching for availability of {ss}..."), cancellationToken);
                Database db = new Database();
                User tutor = db.getUserFromName(ss);
                Dictionary<int, string>  avlOfRequestedTutor = db.getAvailabilityFromEmail(tutor.EmailAdress);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }
        }

        private static async Task<DialogTurnResult> ReturnResultsAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["tutor"] = (string)stepContext.Result;
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Searching for availability of {stepContext.Result}..."), cancellationToken);
            Database db = new Database();
            User tutor = db.getUserFromName(ss);
            Dictionary<int, string> avlOfRequestedTutor = db.getAvailabilityFromEmail(tutor.EmailAdress);
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}

