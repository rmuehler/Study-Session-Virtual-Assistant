// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using VirtualAssistant.Services;
using System.Collections.Generic;

namespace VirtualAssistant.Dialogs
{
    public class Search_by_Tutor : ComponentDialog
    {
        private IStatePropertyAccessor<UserProfile> _userProfileAccessor;

        public Search_by_Tutor(BotServices botServices,
            UserState userState,
            IBotTelemetryClient telemetryClient) : base(nameof(Search_by_Tutor))
        {

            _userProfileAccessor = userState.CreateProperty<UserProfile>("UserProfile");

            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                CheckSearchEntitiesAsync,
                ReturnTimeResultAsync,
                ReturnClassResultAsync,
                RegisterAsync,
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
            stepContext.Values["tutor"] = ents.personName[0];
            stepContext.Values["time"] = ents.datetime;
            stepContext.Values["class"] = null;
            stepContext.Values["confirm"] = 0;

            if (stepContext.Values["time"] == null)
            {
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter the time you would like to search for.") }, cancellationToken);
            }

            if (stepContext.Values["tutor"] != null)
            {
                return await stepContext.ContinueDialogAsync();
            }

            await stepContext.Context.SendActivityAsync(MessageFactory.Text("tutor name null"), cancellationToken);
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private static async Task<DialogTurnResult> ReturnTimeResultAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Values["time"] == null)
            {
                stepContext.Values["time"] = (string)stepContext.Result;
            }

            Database db = new Database();
            User tutor = db.getUserFromName((string)stepContext.Values["tutor"]);
            ICollection<string> courses = new List<string>();
            courses = db.getTutorCourses(tutor.EmailAdress);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text($"Please choose from the list of courses that {tutor.Name} is teaching:\n{string.Join(", ", courses)}") }, cancellationToken);
        }

        private static async Task<DialogTurnResult> ReturnClassResultAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["class"] = (string)stepContext.Result;
            Database db = new Database();
            User tutor = db.getUserFromName((string)stepContext.Values["tutor"]);
            ICollection<string> courses = db.getTutorCourses(tutor.EmailAdress);
            if (!courses.Contains((string)stepContext.Values["class"]))
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("course not taught by that tutor"), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }

            Microsoft.Bot.Builder.AI.Luis.DateTimeSpec[] time = (Microsoft.Bot.Builder.AI.Luis.DateTimeSpec[])stepContext.Values["time"];
            string normalizedTime = db.convertBotTimeToString(time);
            if (db.getAvailability(tutor.EmailAdress, normalizedTime))
            {
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text($"{tutor.Name} is available to tutor you with {(string)stepContext.Values["class"]} at the specified time.\nPlease enter 'y' to confirm your reservation.") }, cancellationToken);
            }

            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I'm sorry, {tutor.Name} is busy at that time."), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }
        }

        private async Task<DialogTurnResult> RegisterAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string confirm = (string)stepContext.Result;
            if (confirm == "y" || confirm == "Y")
            {
                var userProfile = await _userProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);
                User self = userProfile.self;
                Database db = new Database();
                db.reserveTutor(
                    db.getUserFromName((string)stepContext.Values["tutor"]), 
                    self, 
                    db.convertBotTimeToString((Microsoft.Bot.Builder.AI.Luis.DateTimeSpec[])stepContext.Values["time"]), 
                    (string)stepContext.Values["class"]);
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("You have successfully reserved your tutor!"), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }

            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Reservation cancelled."), cancellationToken);
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);

        }
    }
}

