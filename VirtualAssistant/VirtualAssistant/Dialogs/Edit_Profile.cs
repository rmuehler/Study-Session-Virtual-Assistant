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
    public class Edit_Profile : ComponentDialog
    {
        private readonly IStatePropertyAccessor<UserProfile> _userProfileAccessor;
        private OnboardingState _state;
        private ProfileResponses _responder = new ProfileResponses();

        public Edit_Profile(BotServices botServices,
            UserState userState,
            IBotTelemetryClient telemetryClient) : base(nameof(Edit_Profile))
        {
            {
                _userProfileAccessor = userState.CreateProperty<UserProfile>("UserProfile");
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
            var userProfile = await _userProfileAccessor.GetAsync(sc.Context, () => new UserProfile(), cancellationToken);
            User self = userProfile.self;
            
            if (self.Classification == "student")
            {
                await _responder.ReplyWith(sc.Context, ProfileResponses.ResponseIds.ShowStudentProfile, self);
            }
            else
            {
                await _responder.ReplyWith(sc.Context, ProfileResponses.ResponseIds.ShowTutorProfile, self);

            }

            return await sc.EndDialogAsync();
        }


        

        
    }


}