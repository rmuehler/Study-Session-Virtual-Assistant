// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using VirtualAssistant.Services;
using VirtualAssistant.Responses.Profile;
using VirtualAssistant.Responses.Main;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System.Collections.Generic;

namespace VirtualAssistant.Dialogs
{
    public class SubmitDialog : ComponentDialog
    {
        private ProfileResponses _responder = new ProfileResponses();
        public SubmitDialog(BotServices botServices,
            UserState userState,
            IBotTelemetryClient telemetryClient) : base(nameof(SubmitDialog))
        {
            {
                InitialDialogId = nameof(Edit_Profile);

                var editprofile = new WaterfallStep[]
                {
                SubmitDialogAsync,
                };

                AddDialog(new WaterfallDialog(InitialDialogId, editprofile));
            }


        }
        private async Task<DialogTurnResult> SubmitDialogAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {

            await _responder.ReplyWith(sc.Context, ProfileResponses.ResponseIds.Greeting); //TODO maybe submit stuff here?
            return await sc.EndDialogAsync();
        }
    }


}