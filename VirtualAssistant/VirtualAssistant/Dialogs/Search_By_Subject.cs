// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using VirtualAssistant.Services;

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
                ReturnTimeResultsAsync,
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private static async Task<DialogTurnResult> CheckSearchEntitiesAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            Luis.searchskillLuis._Entities ents = (Luis.searchskillLuis._Entities)stepContext.Options;
            stepContext.Values["subject"] = ents.subject;
            stepContext.Values["time"] = ents.datetime;

            if (stepContext.Values["subject"] == null)
            {
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter the course name.") }, cancellationToken);
            }

            else if (stepContext.Values["time"] != null)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Searching for available tutors..."), cancellationToken);

                //put this in adavtive card i guess
                //this accepts the dumb AM0800 format
                Database db = new Database();
                db.findTutors_SubjectTime((string)stepContext.Values["time"], (string)stepContext.Values["subject"]);
                ///////

                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }

            else return await stepContext.ContinueDialogAsync();

        }

        private static async Task<DialogTurnResult> ReturnResultsAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["subject"] = (string)stepContext.Result;

            if (stepContext.Values["time"] == null)
            {
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter your preferred time.") }, cancellationToken);
            }

            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Searching for available tutors..."), cancellationToken);
                //put this in adavtive card i guess
                //this accepts the dumb AM0800 format
                Database db = new Database();
                db.findTutors_SubjectTime((string)stepContext.Values["time"], (string)stepContext.Values["subject"]);
                ///////
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }
        }

        private static async Task<DialogTurnResult> ReturnTimeResultsAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["time"] = (string)stepContext.Result;

            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Searching for available tutors..."), cancellationToken);
            //put this in adavtive card i guess
            //this accepts the dumb AM0800 format
            Database db = new Database();
            db.findTutors_SubjectTime((string)stepContext.Values["time"], (string)stepContext.Values["subject"]);
            ///////
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}

