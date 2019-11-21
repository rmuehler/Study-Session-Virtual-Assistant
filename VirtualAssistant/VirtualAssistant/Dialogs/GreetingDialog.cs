// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using VirtualAssistant.Services;
using System.Collections.Generic;
using VirtualAssistant.Responses.Main;
using VirtualAssistant.Responses.Profile;
using Microsoft.Bot.Schema;

namespace VirtualAssistant.Dialogs
{
    public class Greeting_Dialog : ComponentDialog
    {
        private IStatePropertyAccessor<UserProfile> _userProfileAccessor;
        private ProfileResponses _responder = new ProfileResponses();

        public Greeting_Dialog(BotServices botServices,
            UserState userState,
            IBotTelemetryClient telemetryClient) : base(nameof(Greeting_Dialog))
        {

            _userProfileAccessor = userState.CreateProperty<UserProfile>("UserProfile");

            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                BuildCardAsync,
                AskIfReturningAsync,
                ReturnResultsAsync,
                ReturnResult2Async,
                ReturnEmailAsync,
                ReturnStuffAsync,
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private static async Task<DialogTurnResult> BuildCardAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var view = new MainResponses();
            Task s = view.ReplyWith(stepContext.Context, MainResponses.ResponseIds.Greeting);

            return await stepContext.ContinueDialogAsync();
        }

        private static async Task<DialogTurnResult> AskIfReturningAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Hello! Were you previously registered with us?") }, cancellationToken);
        }

        private static async Task<DialogTurnResult> ReturnResultsAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((string)stepContext.Result == "yes")
            {
                stepContext.Values["returning"] = 1;
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter your e-mail address to confirm your identity.") }, cancellationToken);
            }
            else
            {
                stepContext.Values["returning"] = 0;
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Would you like to register for this service?") }, cancellationToken);
            }
        }
        private async Task<DialogTurnResult> ReturnResult2Async(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((long)stepContext.Values["returning"] == 0)
            {
                if ((string)stepContext.Result == "no")
                {
                    return await stepContext.EndDialogAsync(null, cancellationToken: cancellationToken);
                }

                else
                {

                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Will you be using this service as a tutor or student?") }, cancellationToken);
                }
            }

            else
            {
                stepContext.Values["email"] = (string)stepContext.Result;
                string useremail = ((string)stepContext.Values["email"]);

                    //Get user from database
                    Database db = new Database();
                    User currentuser = db.getUserFromEmail(useremail);

                    if (currentuser != null)
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Welcome Back {currentuser.Name}!"), cancellationToken);
                        var userProfile = await _userProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);
                        userProfile.self = currentuser;
                        await _userProfileAccessor.SetAsync(stepContext.Context, userProfile, cancellationToken);

                        if (currentuser.Classification == "tutor")
                        {
                            Dictionary<int, Dictionary<string, string>> registrations = db.getRegistrationUpdate(currentuser.EmailAdress);
                            if (registrations.Count == 0)
                            {
                                await stepContext.Context.SendActivityAsync(MessageFactory.Text("You have no upcoming reservations."), cancellationToken);

                            }

                            else
                            {
                                await stepContext.Context.SendActivityAsync(MessageFactory.Text("I found some upcoming reservations for you! Here are the details: "), cancellationToken);
                            }

                            foreach (var s in registrations)
                            {
                                string printout = "";
                                foreach (var ss in s.Value)
                                {
                                    printout += ss.Key + ": " + ss.Value + "\r\n";
                                }

                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"{printout}"), cancellationToken);
                            }

                            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Please contact the student(s) to set up a meeting location."), cancellationToken);

                        }


                    return await stepContext.EndDialogAsync(currentuser, cancellationToken: cancellationToken);
                    }

                    else
                    {
                        //TODO: user does not exists: either reprompt email or ask to make new account
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text("No user found with that e-mail address..."), cancellationToken);
                    return await stepContext.EndDialogAsync(currentuser, cancellationToken: cancellationToken);
                }
            }

        }

        private async Task<DialogTurnResult> ReturnEmailAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["usertype"] = (string)stepContext.Result;
            string usertype = ((string)stepContext.Values["usertype"]);

            if (usertype == "tutor")
            {
                await _responder.ReplyWith(stepContext.Context, ProfileResponses.ResponseIds.ShowTutorProfile);
            }

            else
            {
                await _responder.ReplyWith(stepContext.Context, ProfileResponses.ResponseIds.ShowStudentProfile);
            }

            var opts = new PromptOptions
            {
                Prompt = new Activity
                {
                    Type = ActivityTypes.Message,
                }
            };

            // Display a Text Prompt and wait for input
            return await stepContext.PromptAsync(nameof(TextPrompt), opts);
        }

        private async Task<DialogTurnResult> ReturnStuffAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = new UserProfile();
            userProfile = await _userProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

            User self = userProfile.self;
            if (self.Classification == "tutor")
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Thank you for registering, {self.Name}! When you are ready to start tutoring, please update your availability."), cancellationToken);
            }

            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Thank you for registering, {self.Name}!"), cancellationToken);
            }

            await _userProfileAccessor.SetAsync(stepContext.Context, userProfile, cancellationToken);

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}

