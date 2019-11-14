// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
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
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private static async Task<DialogTurnResult> AskIfReturningAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            //create set of possible choices (no isn't really needeed)
            Choice choice1 = new Choice("yes");
            choice1.Synonyms = new List<string> { "yeah", "yup", "y", "ye" , "of course", "i am"};
            choice1.Value = "yes";
            Choice choice2 = new Choice("no");
            choice2.Synonyms = new List<string> { "nah", "i don't" };
            choice2.Value = "no";

            IList<Choice> choices = new List<Choice> { choice1, choice2};

            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions { Prompt = MessageFactory.Text("Hello, are you a returning user?"), Choices = choices, Style = ListStyle.Auto}, cancellationToken);
        }

        private static async Task<DialogTurnResult> ReturnResultsAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            var choice = (FoundChoice)stepContext.Result;
            if (choice.Value == "yes")
            {
                stepContext.Values["returning"] = 1;
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter your e-mail address to confirm your identity.") }, cancellationToken);
            }
            else
            {
                Choice choice1 = new Choice("yes");
                choice1.Synonyms = new List<string> { "yeah", "yup", "y", "ye", "of course", "i am" };
                choice1.Value = "yes";
                Choice choice2 = new Choice("no");
                choice2.Synonyms = new List<string> { "nah", "i don't" };
                choice2.Value = "no";
                stepContext.Values["returning"] = 0;

                IList<Choice> choices = new List<Choice> { choice1, choice2 };

                return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions { Prompt = MessageFactory.Text("Would you like to register for this service?"), Choices = choices }, cancellationToken);
            }
        }
        private static async Task<DialogTurnResult> ReturnResult2Async(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            FoundChoice choice = null;

            //since we either get a choice or a string, need to make sure we are evaluatin a Choice
            if(stepContext.Result.GetType() == (typeof(FoundChoice)))
            {
                choice = (FoundChoice)stepContext.Result;
            }
            if ((long)stepContext.Values["returning"] == 0)
            {
                if (choice.Value == "no")
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
            string useremail = null;

            //need to make sure we are setting a string
            if(stepContext.Result.GetType() == typeof(string))
            {
                stepContext.Values["email"] = (string)stepContext.Result;
                useremail = ((string)stepContext.Values["email"]);
            }

            //end greeting if returning user
            if ((long)stepContext.Values["returning"] == 1)
            {
                //validate email using regex
                try
                {
                    MailAddress m = new MailAddress(useremail);
                }
                catch (FormatException)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("This is not a valid email address"), cancellationToken);
                    //TODO: have this reprompt user to enter email again
                }

                //Get user from database
                Database db = new Database();
                User currentuser = db.getUserFromEmail(useremail);

                if (currentuser != null)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Welcome Back {currentuser.Name}!"), cancellationToken);
                    return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
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
        private static async Task<DialogTurnResult> ReturnUserTypeAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["usertype"] = (string)stepContext.Result;
            string usertype = ((string)stepContext.Values["usertype"]);

            if (usertype == "tutor")
            {
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter which course you will be tutoring for.") }, cancellationToken);
            }

            else
            {
                //Creates the user object
                User newUser = new User();
                newUser.RowKey = (string)stepContext.Values["email"];
                newUser.Classification = "student";
                newUser.Name = (string)stepContext.Values["name"];
                newUser.PartitionKey = "University of South Florida";
                Database db = new Database();
                db.postNewUser(newUser);

                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Thank you for registering, {newUser.Name}! You may now use this service."), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }
        }
        private static async Task<DialogTurnResult> ReturnCourseAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["course"] = (string)stepContext.Result;

            //Creates the user object
            User newUser = new User();
            newUser.RowKey = (string)stepContext.Values["email"];
            newUser.Classification = "tutor";
            newUser.Name = (string)stepContext.Values["name"];
            newUser.Class = (string)stepContext.Values["course"];

            Database db = new Database();
            db.postNewUser(newUser);

            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Thank you for registering, {newUser.Name}!"), cancellationToken);
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}

