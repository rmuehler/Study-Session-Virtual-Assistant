// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using VirtualAssistant.Services;
using VirtualAssistant.Responses.Profile;
using Microsoft.Bot.Schema;
using VirtualAssistant.Models;

namespace VirtualAssistant.Dialogs
{
    public class Edit_Profile : ComponentDialog
    {
        private IStatePropertyAccessor<UserProfile> _userProfileAccessor;
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
                ReturnStuffAsync,
                };

                AddDialog(new WaterfallDialog(nameof(WaterfallDialog), editprofile));
                AddDialog(new TextPrompt(nameof(TextPrompt)));
                // The initial child Dialog to run.
                InitialDialogId = nameof(WaterfallDialog);

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

            var opts = new PromptOptions
            {
                Prompt = new Activity
                {
                    Type = ActivityTypes.Message,
                }
            };

            // Display a Text Prompt and wait for input
            return await sc.PromptAsync(nameof(TextPrompt), opts);
        }
        private async Task<DialogTurnResult> ReturnStuffAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"You're all set!"), cancellationToken);
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

    }


}