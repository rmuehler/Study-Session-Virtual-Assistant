// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using VirtualAssistant.Services;

namespace VirtualAssistant.Dialogs
{
    public class Greeting_Dialog : ComponentDialog
    {

        public Greeting_Dialog(BotServices botServices,
            UserState userState,
            IBotTelemetryClient telemetryClient) : base(nameof(Greeting_Dialog))
        {
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
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Hello, are you a returning user?") }, cancellationToken);
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
                    return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                }

                else
                {
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter your e-mail address.") }, cancellationToken);
                }
            }

            else return await stepContext.ContinueDialogAsync();
        }
        private static async Task<DialogTurnResult> ReturnEmailAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["email"] = (string)stepContext.Result;

            //use this string for database.
            string useremail = ((string)stepContext.Values["email"]);

            //end greeting if returning user
            if ((long)stepContext.Values["returning"] == 1)
            {
                //Get user from database
                Database db = new Database();
                User currentuser = db.getUserFromEmail(useremail);

                if (currentuser.value.Length > 0)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Welcome Back {currentuser.value[0].Name}!"), cancellationToken);
                    return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                }

                else
                {
                    //TODO:  user does not exists: either reprompt email or ask to make new account
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("No user found with that account. Creating a new account with your email ..."), cancellationToken);
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter your full name.") }, cancellationToken);
                }

            }

            else
            {
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter your full name.") }, cancellationToken);
            }

        }

        private static async Task<DialogTurnResult> ReturnNameAync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["name"] = (string)stepContext.Result;

            //use this string for database.
            string username = ((string)stepContext.Values["name"]);
            
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Will you be using this service as a tutor or student?") }, cancellationToken);
        }
        private static async Task<DialogTurnResult> ReturnUserTypeAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["usertype"] = (string)stepContext.Result;

            //use this string for database.
            string usertype = ((string)stepContext.Values["usertype"]);

            if (usertype == "tutor")
            {
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter which course you will be tutoring for.") }, cancellationToken);
            }

            else
            {
                //Creates the user object
                User newUser = new User();
                newUser.value = new VirtualAssistant.UserData[1] { new UserData() };
                newUser.value[0].EmailAdress = (string)stepContext.Values["email"];
                newUser.value[0].Classification = "student";
                newUser.value[0].Name = (string)stepContext.Values["name"];


                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Thank you for registering, {((string)stepContext.Values["name"])}! You may now use this service."), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }
        }
        private static async Task<DialogTurnResult> ReturnCourseAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            stepContext.Values["course"] = (string)stepContext.Result;

            //Creates the user object
            User newUser = new User();
            newUser.value = new VirtualAssistant.UserData[1] {new UserData()};
            newUser.value[0].EmailAdress = (string)stepContext.Values["email"];
            newUser.value[0].Classification = "tutor";
            newUser.value[0].Name = (string)stepContext.Values["name"];
            newUser.value[0].Class = (string)stepContext.Values["course"];


            //i tried lol
            Database db = new Database();
            db.postNewUser(newUser);

            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Thank you for registering, {((string)stepContext.Values["name"])}!"), cancellationToken);
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}

