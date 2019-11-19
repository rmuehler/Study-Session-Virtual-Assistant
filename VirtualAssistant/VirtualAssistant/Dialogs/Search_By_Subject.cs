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
    public class Search_by_Subject : ComponentDialog
    {
        private readonly IStatePropertyAccessor<UserProfile> _userProfileAccessor;

        public Search_by_Subject(BotServices botServices,
            UserState userState,
            IBotTelemetryClient telemetryClient) : base(nameof(Search_by_Subject))
        {

            _userProfileAccessor = userState.CreateProperty<UserProfile>("UserProfile");

            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                CheckSearchEntitiesAsync,
                ReturnSubjectAsync,
                ReturnTimeAsync,
                SearchTutorsAsync,
                GetTutorPreferenceAsync,
                ReturnConfirmAsync,
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> CheckSearchEntitiesAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //init
            stepContext.Values["subject"] = null;

            Luis.searchskillLuis._Entities ents = (Luis.searchskillLuis._Entities)stepContext.Options;
            if (ents.subject != null) stepContext.Values["subject"] = ents.subject[0];
            stepContext.Values["time"] = ents.datetime;
            
            if (stepContext.Values["subject"] == null)
            {
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter the course name.") }, cancellationToken);
            }

            else return await stepContext.ContinueDialogAsync();

        }

        private static async Task<DialogTurnResult> ReturnSubjectAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Values["subject"] == null)
            {
                stepContext.Values["subject"] = (string)stepContext.Result;
            }

            if (stepContext.Values["time"] == null)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Please ask me again with your preferred time."), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);

                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter your preferred time.") }, cancellationToken);
            }

            return await stepContext.ContinueDialogAsync();
        }

        private async Task<DialogTurnResult> ReturnTimeAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Values["time"] == null)
            {

                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Please ask me again with your preferred time."), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);

               // Microsoft.Bot.Builder.AI.Luis.DateTimeSpec[] time = new Microsoft.Bot.Builder.AI.Luis.DateTimeSpec[1] { (Microsoft.Bot.Builder.AI.Luis.DateTimeSpec)stepContext.Result };
               // time.SetValue(stepContext.Result, 0);
               // stepContext.Values["time"] = time[0];
            }
            return await stepContext.ContinueDialogAsync();
        }

        private static async Task<DialogTurnResult> SearchTutorsAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Searching for available tutors..."), cancellationToken);
            Database db = new Database();
            Microsoft.Bot.Builder.AI.Luis.DateTimeSpec[] time = (Microsoft.Bot.Builder.AI.Luis.DateTimeSpec[])stepContext.Values["time"];
            string subject = (string)stepContext.Values["subject"];
            List<User> tutors = new List<User>(db.findTutors_SubjectTime(db.convertBotTimeToString(time), db.normalizeCourseName(subject)));
            List<string> tutorNames = new List<string>();
            foreach (User t in tutors)
            {
                tutorNames.Add(t.Name);
            }
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions {
                Prompt = MessageFactory.Text($"I found some tutors that are available at that time to teach {subject}. Please choose one of the following tutors:\n{string.Join(", ", tutorNames)}") }, cancellationToken);
        }
        private static async Task<DialogTurnResult> GetTutorPreferenceAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["tutor"] = (string)stepContext.Result;
            Database db = new Database();
            Microsoft.Bot.Builder.AI.Luis.DateTimeSpec[] time = (Microsoft.Bot.Builder.AI.Luis.DateTimeSpec[])stepContext.Values["time"];
            string subject = (string)stepContext.Values["subject"];
            List<User> tutors = db.findTutors_SubjectTime(db.convertBotTimeToString(time), db.normalizeCourseName(subject));
            bool found = false;
            foreach (User t in tutors) if (t.Name == (string)stepContext.Values["tutor"]) found = true;
            if (found)
            {
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text($"Please enter 'y' to confirm your reservation with {(string)stepContext.Values["tutor"]}.") }, cancellationToken);
            }

            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Reservation cancelled."), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }
        }

        private async Task<DialogTurnResult> ReturnConfirmAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((string)stepContext.Result == "y" || (string)stepContext.Result == "Y")
            {
                var userProfile = await _userProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);
                User self = userProfile.self;
                Database db = new Database();
                db.reserveTutor(
                    db.getUserFromName((string)stepContext.Values["tutor"]),
                    self,
                    db.convertBotTimeToString((Microsoft.Bot.Builder.AI.Luis.DateTimeSpec[])stepContext.Values["time"]),
                    (string)stepContext.Values["subject"]);
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("You have successfully reserved your tutor!"), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }

            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Reservation cancelled."), cancellationToken);
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

    }
}

