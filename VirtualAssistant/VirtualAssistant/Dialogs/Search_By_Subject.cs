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
    public class Search_by_Subject : ComponentDialog
    {
        public Search_by_Subject(BotServices botServices,
            UserState userState,
            IBotTelemetryClient telemetryClient) : base(nameof(Search_by_Subject))
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
            stepContext.Values["subject"] = stepContext.Options;
            if (stepContext.Values["subject"] == null)
            {
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter the course name.") }, cancellationToken);
            }

            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Searching for available tutors..."), cancellationToken);
                /*
                //TODO: Connect to database. Fetch tutors.
                */
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }
        }

        private static async Task<DialogTurnResult> ReturnResultsAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["subject"] = (string)stepContext.Result;
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Searching for available tutors..."), cancellationToken);
            /*
            //TODO: Connect to database. Fetch tutors.
            */
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}

