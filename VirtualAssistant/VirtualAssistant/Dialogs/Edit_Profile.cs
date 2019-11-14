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
using VirtualAssistant.Responses.Onboarding;
using VirtualAssistant.Models;
using System.IO;
using AdaptiveCards;
using Microsoft.Bot.Schema;

namespace VirtualAssistant.Dialogs
{
    public class Edit_Profile : ComponentDialog
    {
        private IStatePropertyAccessor<OnboardingState> _accessor;
        private OnboardingState _state;
        private ProfileResponses _responder = new ProfileResponses();

        public Edit_Profile(BotServices botServices,
            UserState userState,
            IBotTelemetryClient telemetryClient) : base(nameof(Edit_Profile))
        {
            {
                _accessor = userState.CreateProperty<OnboardingState>(nameof(OnboardingState));
                InitialDialogId = nameof(Edit_Profile);



                var editprofile = new WaterfallStep[]
                {
                EditProfileAsync,
                };

                AddDialog(new WaterfallDialog(InitialDialogId, editprofile));
            }


        }
        private async Task<DialogTurnResult> EditProfileAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            _state = await _accessor.GetAsync(sc.Context, () => new OnboardingState());

            if (!string.IsNullOrWhiteSpace(_state.Email))
            {
                Database db = new Database();
                User user = db.getUserFromEmail(_state.Email);

                if (user != null)
                {
                    await _responder.ReplyWith(sc.Context, ProfileResponses.ResponseIds.ShowStudentProfile, user);
                    return await sc.EndDialogAsync(cancellationToken: cancellationToken);
                }
            }
            //if no userstate email or user not found through email
            await sc.Context.SendActivityAsync(MessageFactory.Text("Please log in first."), cancellationToken);
            return await sc.EndDialogAsync(cancellationToken: cancellationToken);

        }

        }


}