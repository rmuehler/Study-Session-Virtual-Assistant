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
                ReturnEmailAsync,
                ReturnNameAync,
                ReturnUserTypeAsync,
                ReturnCourseAsync,
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new TextPrompt(nameof(ConfirmPrompt)));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new TextPrompt(nameof(TextPrompt)));


            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private static async Task<DialogTurnResult> AskIfReturningAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text("Hello, are you a returning user?") }, cancellationToken);
        }

        private static async Task<DialogTurnResult> ReturnResultsAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                stepContext.Values["returning"] = 1;
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter your e-mail address to confirm your identity.") }, cancellationToken);
            }
            else
            {
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter your e-mail address to begin your registration.") }, cancellationToken);
            }
        }
        private static async Task<DialogTurnResult> ReturnEmailAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["email"] = (string)stepContext.Result;

            //use this string for database.
            string useremail = ((string[])stepContext.Values["email"])[0];

            //end greeting if returning user
            if ((int)stepContext.Values["returning"] == 1)
            {

                //TODO: Connect to database. verify email.

                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Welcome Back!"), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
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
            string username = ((string[])stepContext.Values["name"])[0];

            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Will you be using this service as a tutor or student?") }, cancellationToken);
        }
        private static async Task<DialogTurnResult> ReturnUserTypeAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["usertype"] = (string)stepContext.Result;

            //use this string for database.
            string usertype = ((string[])stepContext.Values["usertype"])[0];

            if (usertype == "tutor")
            {
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter which course you will be tutoring for.") }, cancellationToken);
            }

            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Thank you for registering, {((string[])stepContext.Values["name"])[0]}! You may now use this service."), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }
        }
        private static async Task<DialogTurnResult> ReturnCourseAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["course"] = (string)stepContext.Result;
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Thank you for registering, {((string[])stepContext.Values["name"])[0]}!"), cancellationToken);
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}

