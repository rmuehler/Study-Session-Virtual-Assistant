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

namespace VirtualAssistant.Dialogs
{
    public class Update_Availability : ComponentDialog
    {
        private readonly IStatePropertyAccessor<UserProfile> _userProfileAccessor;
        private OnboardingState _state;
        private ProfileResponses _responder = new ProfileResponses();

        public Update_Availability(BotServices botServices,
            UserState userState,
            IBotTelemetryClient telemetryClient) : base(nameof(Update_Availability))
        {
            {
                _userProfileAccessor = userState.CreateProperty<UserProfile>("UserProfile");
                InitialDialogId = nameof(Update_Availability);



                var updateavailability = new WaterfallStep[]
                {
                UpdateAvailabilityAsync,
                };

                AddDialog(new WaterfallDialog(InitialDialogId, updateavailability));
            }


        }
        private async Task<DialogTurnResult> UpdateAvailabilityAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userProfile = await _userProfileAccessor.GetAsync(sc.Context, () => new UserProfile(), cancellationToken);
            User self = userProfile.self;

            if (self.Classification == "student")
            {
                await sc.Context.SendActivityAsync(MessageFactory.Text("Only tutors can set their availability!"), cancellationToken);
            }
            else
            {
                await _responder.ReplyWith(sc.Context, ProfileResponses.ResponseIds.UpdateAvailability, self);

            }

            return await sc.EndDialogAsync();
        }
    }

}