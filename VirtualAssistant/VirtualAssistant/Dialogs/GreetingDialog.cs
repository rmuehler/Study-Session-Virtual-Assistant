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
    public class Greeting_Dialog : ComponentDialog
    {
        private readonly IStatePropertyAccessor<UserProfile> _userProfileAccessor;

        public Greeting_Dialog(BotServices botServices,
            UserState userState,
            IBotTelemetryClient telemetryClient) : base(nameof(Greeting_Dialog))
        {

            _userProfileAccessor = userState.CreateProperty<UserProfile>("UserProfile");

            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                AskIfReturningAsync,
                ReturnResultsAsync,
                ReturnResult2Async,
                ReturnEmailAsync,
                ReturnNameAync,
                ReturnUserTypeAsync,
                ReturnCourseAsync,
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private static async Task<DialogTurnResult> AskIfReturningAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Were you previously registered with us?") }, cancellationToken);
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
        private static async Task<DialogTurnResult> ReturnResult2Async(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((long)stepContext.Values["returning"] == 0)
            {
                if ((string)stepContext.Result == "no")
                {
                    return await stepContext.EndDialogAsync(null, cancellationToken: cancellationToken);
                }

                else
                {
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter your e-mail address.") }, cancellationToken);
                }
            }

            else return await stepContext.ContinueDialogAsync();
        }
        private async Task<DialogTurnResult> ReturnEmailAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["email"] = (string)stepContext.Result;
            string useremail = ((string)stepContext.Values["email"]);

            //end greeting if returning user
            if ((long)stepContext.Values["returning"] == 1)
            {
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
                    }
                   

                    return await stepContext.EndDialogAsync(currentuser, cancellationToken: cancellationToken);
                }

                else
                {
                  //TODO: user does not exists: either reprompt email or ask to make new account
                  await stepContext.Context.SendActivityAsync(MessageFactory.Text("No user found with that account. Creating a new account with your email ..."), cancellationToken);
                  return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter your full name.") }, cancellationToken);
                }
            }

            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter your full name.") }, cancellationToken);
        }

        private static async Task<DialogTurnResult> ReturnNameAync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["name"] = (string)stepContext.Result;
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Will you be using this service as a tutor or student?") }, cancellationToken);
        }
        private async Task<DialogTurnResult> ReturnUserTypeAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            Database db = new Database();
            stepContext.Values["usertype"] = (string)stepContext.Result;
            string usertype = ((string)stepContext.Values["usertype"]);

            if (usertype == "tutor")
            {
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions
                { Prompt = MessageFactory.Text($"Please enter which of these courses you will be tutoring for. If you are selecting multiple courses, please seperate them with commas.\n{string.Join(", ", db.getCurrentListOfCourses())}") }, cancellationToken);
            }
            else
            {
                //Creates the user object
                User newUser = new User();
                newUser.RowKey = (string)stepContext.Values["email"];
                newUser.RowKey = newUser.RowKey.ToLower();
                newUser.Classification = "student";
                newUser.Name = (string)stepContext.Values["name"];
                newUser.Name = newUser.Name.ToLower();
                newUser.PartitionKey = "University of South Florida";
                db.postNewUser(newUser);

                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Thank you for registering, {newUser.Name}! You may now use this service."), cancellationToken);
                var userProfile = await _userProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);
                userProfile.self = newUser;
                await _userProfileAccessor.SetAsync(stepContext.Context, userProfile, cancellationToken);

                return await stepContext.EndDialogAsync(newUser, cancellationToken: cancellationToken);
            }
        }
        private async Task<DialogTurnResult> ReturnCourseAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["courses"] = (string)stepContext.Result;
            string courselist = ((string)stepContext.Values["courses"]);

            //Creates the user object
            User newUser = new User();
            newUser.RowKey = (string)stepContext.Values["email"];
            newUser.RowKey = newUser.RowKey.ToLower();
            newUser.Classification = "tutor";
            newUser.Name = (string)stepContext.Values["name"];
            newUser.Name = newUser.Name.ToLower();

            Database db = new Database();
            db.postNewUser(newUser);
            db.setTutorCourses(newUser, courselist);
            db.setTutorAvailability_RESET(newUser);

            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Thank you for registering, {newUser.Name}!"), cancellationToken);
            var userProfile = await _userProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);
            userProfile.self = newUser;
            await _userProfileAccessor.SetAsync(stepContext.Context, userProfile, cancellationToken);

            return await stepContext.EndDialogAsync(newUser, cancellationToken: cancellationToken);
        }
    }
}

